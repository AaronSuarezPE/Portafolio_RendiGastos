/*
 * Service worker de RendiGastos.
 * - Hace instalable la aplicación y guarda la "cáscara" (páginas, estilos y scripts) para abrir rápido.
 * - Recibe los avisos push y los muestra como notificaciones del dispositivo.
 * Los datos de la API nunca se guardan aquí: siempre se piden a la red.
 */
const VERSION = 'rendigastos-v1';
const CASCARA = ['/', '/manifest.webmanifest', '/iconos/icono-192.png'];

self.addEventListener('install', (evento) => {
  evento.waitUntil(caches.open(VERSION).then((cache) => cache.addAll(CASCARA)));
  self.skipWaiting();
});

self.addEventListener('activate', (evento) => {
  evento.waitUntil(
    caches
      .keys()
      .then((claves) => Promise.all(claves.filter((c) => c !== VERSION).map((c) => caches.delete(c))))
      .then(() => self.clients.claim()),
  );
});

self.addEventListener('fetch', (evento) => {
  const peticion = evento.request;
  const url = new URL(peticion.url);

  // Solo se atienden las páginas y recursos del propio sitio; la API y otros orígenes pasan directo.
  if (peticion.method !== 'GET' || url.origin !== self.location.origin) return;

  if (peticion.mode === 'navigate') {
    // Páginas: primero la red; sin conexión, la cáscara guardada.
    evento.respondWith(fetch(peticion).catch(() => caches.match('/')));
    return;
  }

  // Recursos estáticos: primero lo guardado y se refresca en segundo plano.
  evento.respondWith(
    caches.match(peticion).then((guardado) => {
      const actualizado = fetch(peticion)
        .then((respuesta) => {
          if (respuesta.ok) {
            const copia = respuesta.clone();
            caches.open(VERSION).then((cache) => cache.put(peticion, copia));
          }
          return respuesta;
        })
        .catch(() => guardado);
      return guardado || actualizado;
    }),
  );
});

self.addEventListener('push', (evento) => {
  let datos = { titulo: 'RendiGastos', mensaje: 'Tienes novedades en tus rendiciones.' };
  try {
    if (evento.data) datos = { ...datos, ...evento.data.json() };
  } catch {
    // Si el contenido no es JSON, se muestra el mensaje por defecto.
  }

  evento.waitUntil(
    self.registration.showNotification(datos.titulo, {
      body: datos.mensaje,
      icon: '/iconos/icono-192.png',
      badge: '/iconos/icono-192.png',
      data: { url: '/avisos' },
    }),
  );
});

self.addEventListener('notificationclick', (evento) => {
  evento.notification.close();
  const destino = (evento.notification.data && evento.notification.data.url) || '/';

  evento.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((ventanas) => {
      for (const ventana of ventanas) {
        if ('focus' in ventana) {
          ventana.navigate(destino);
          return ventana.focus();
        }
      }
      return self.clients.openWindow(destino);
    }),
  );
});
