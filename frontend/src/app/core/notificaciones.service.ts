import { Injectable, OnDestroy, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';

const INTERVALO_MS = 60_000;

/** Estado del permiso de avisos del dispositivo. */
export type EstadoPush = 'no-disponible' | 'sin-servidor' | 'bloqueado' | 'inactivo' | 'activo';

/**
 * Cuenta los avisos sin leer (se actualiza sola cada minuto) y gestiona la suscripción
 * a los avisos del dispositivo (Web Push). Sin claves en el servidor, los avisos solo se ven en la aplicación.
 */
@Injectable({ providedIn: 'root' })
export class NotificacionesService implements OnDestroy {
  private readonly api = inject(ApiService);
  private temporizador: ReturnType<typeof setInterval> | null = null;

  readonly noLeidos = signal(0);
  readonly estadoPush = signal<EstadoPush>('inactivo');

  iniciar(): void {
    this.detener();
    this.actualizar();
    this.temporizador = setInterval(() => this.actualizar(), INTERVALO_MS);
    void this.evaluarPush();
  }

  detener(): void {
    if (this.temporizador !== null) clearInterval(this.temporizador);
    this.temporizador = null;
    this.noLeidos.set(0);
  }

  actualizar(): void {
    this.api.contarAvisos().subscribe({
      next: (r) => this.noLeidos.set(r.noLeidos),
      error: () => {
        // Un fallo momentáneo no debe molestar a la persona: el contador se actualiza en el siguiente ciclo.
      },
    });
  }

  ngOnDestroy(): void {
    this.detener();
  }

  get pushDisponible(): boolean {
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
  }

  /** Revisa si este dispositivo ya está suscrito, bloqueado o si el servidor no ofrece avisos push. */
  async evaluarPush(): Promise<void> {
    if (!this.pushDisponible) return this.estadoPush.set('no-disponible');
    if (Notification.permission === 'denied') return this.estadoPush.set('bloqueado');

    try {
      const clave = await firstValueFrom(this.api.clavePush());
      if (!clave.habilitado) return this.estadoPush.set('sin-servidor');

      const registro = await navigator.serviceWorker.getRegistration();
      const suscripcion = await registro?.pushManager.getSubscription();
      this.estadoPush.set(suscripcion ? 'activo' : 'inactivo');
    } catch {
      this.estadoPush.set('inactivo');
    }
  }

  /** Pide permiso y suscribe el dispositivo. Lanza un Error con un mensaje claro si no se puede. */
  async activarPush(): Promise<void> {
    if (!this.pushDisponible) throw new Error('Este navegador no admite avisos en el dispositivo.');

    const clave = await firstValueFrom(this.api.clavePush());
    if (!clave.habilitado) throw new Error('El servidor todavía no tiene activados los avisos en el dispositivo.');

    const permiso = await Notification.requestPermission();
    if (permiso !== 'granted') {
      this.estadoPush.set(permiso === 'denied' ? 'bloqueado' : 'inactivo');
      throw new Error('No diste permiso para mostrar avisos. Puedes activarlo desde la configuración del navegador.');
    }

    const registro = await navigator.serviceWorker.ready;
    const suscripcion =
      (await registro.pushManager.getSubscription()) ??
      (await registro.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: base64AUint8(clave.clavePublica),
      }));

    const datos = suscripcion.toJSON();
    const p256 = datos.keys?.['p256dh'];
    const auth = datos.keys?.['auth'];
    if (!datos.endpoint || !p256 || !auth) throw new Error('El navegador no entregó los datos de la suscripción.');

    await firstValueFrom(this.api.suscribirPush(datos.endpoint, p256, auth));
    this.estadoPush.set('activo');
  }

  async desactivarPush(): Promise<void> {
    const registro = await navigator.serviceWorker.getRegistration();
    const suscripcion = await registro?.pushManager.getSubscription();
    if (suscripcion) {
      await firstValueFrom(this.api.desuscribirPush(suscripcion.endpoint));
      await suscripcion.unsubscribe();
    }
    this.estadoPush.set('inactivo');
  }
}

function base64AUint8(base64Url: string): Uint8Array {
  const relleno = '='.repeat((4 - (base64Url.length % 4)) % 4);
  const base64 = (base64Url + relleno).replace(/-/g, '+').replace(/_/g, '/');
  const crudo = atob(base64);
  return Uint8Array.from(crudo, (c) => c.charCodeAt(0));
}
