# RendiGastos — Manual de usuario

RendiGastos sirve para **rendir tus gastos de trabajo**: registras cada gasto con su comprobante, los juntas en una rendición y la envías a tu aprobador para que la apruebe o la rechace. Funciona en computadora y en celular, y se puede instalar como aplicación.

## 1. Ingresar

1. Abre la aplicación. Verás la pantalla **Ingresar**.
2. Escribe tu **correo** y tu **clave** y pulsa **Ingresar**. El botón **Ver** muestra u oculta la clave.
3. Si te equivocas, el mensaje dice «Correo o clave incorrectos». Por seguridad no indica cuál de los dos falló.
4. Tras demasiados intentos seguidos verás «Hiciste demasiados intentos»; espera un minuto.

**Crear cuenta:** en la pestaña *Crear cuenta* escribe tu nombre, un correo válido y una clave de **al menos 8 caracteres con letras y números**, y repítela. La cuenta nace como **Empleado**; el administrador te asignará un aprobador.

**Cuentas de demostración:** debajo del formulario hay tres botones (Empleado, Aprobador, Administrador) para probar la aplicación con un clic. Son datos ficticios.

Tu sesión dura 2 horas. Cuando vence, un cuadro te avisa y vuelves a ingresar. El botón **Salir** cierra la sesión.

## 2. Roles

| Rol | Puede |
|---|---|
| **Empleado** | Crear rendiciones, agregar gastos, adjuntar comprobantes, enviar, y corregir las que le rechacen |
| **Aprobador** | Todo lo del empleado, y aprobar o rechazar las rendiciones de las personas que lo tienen como aprobador |
| **Administrador** | Todo lo anterior, y crear usuarios, asignar roles y aprobadores, y gestionar las categorías de gasto |

El menú superior muestra solo lo que tu rol permite. Aunque alguien escriba una dirección a mano, el servidor vuelve a verificar tu rol.

## 3. Mis rendiciones

La pantalla **Mis rendiciones** lista tus rendiciones con su estado, total y número de gastos. Puedes filtrar por estado.

| Estado | Significado | Qué puedes hacer |
|---|---|---|
| **Borrador** | La estás armando | Agregar, corregir y eliminar gastos; cambiar el título; enviarla; eliminarla |
| **Enviada** | Espera la decisión de tu aprobador | Solo mirarla |
| **Aprobada** | Terminó bien | Solo mirarla |
| **Rechazada** | Tu aprobador pidió correcciones | Leer el motivo y **Corregir y volver a enviar** |

Límite: hasta **10 borradores** a la vez y **50 gastos** por rendición.

### Crear una rendición
1. Pulsa **Nueva rendición**. Se abre un panel por la derecha.
2. Escribe un **título** (entre 3 y 100 caracteres), por ejemplo «Viaje a Cusco, octubre». Es obligatorio (lo marca un asterisco rojo); si lo dejas vacío te lo indica y no se envía nada.
3. Pulsa **Crear rendición**. Entrarás a su detalle.

### Agregar un gasto
1. En el detalle, pulsa **Agregar gasto**.
2. Completa:
   - **Categoría** (obligatoria). Al elegirla, la pantalla te muestra su **límite por gasto** y si **exige comprobante**.
   - **Fecha del gasto** (obligatoria): hasta 90 días atrás y nunca futura.
   - **Descripción** (obligatoria, 3 a 200 caracteres).
   - **Monto** (obligatorio): mayor que 0, hasta 2 decimales y sin superar el límite de la categoría.
   - **Comprobante**: obligatorio solo si la categoría lo exige (la etiqueta lo marca con asterisco).
3. Pulsa **Guardar gasto**. Un cuadro centrado confirma que se guardó.

**Qué comprobantes se aceptan:** únicamente **PDF** y fotos **PNG o JPG**, de hasta **5 MB**. Si tu comprobante está en Excel, Word u otro formato, guárdalo como PDF o tómale una foto. Si eliges un archivo no permitido, vacío, demasiado grande o que solo cambió de extensión (por ejemplo un Excel renombrado a .pdf), la aplicación te lo explica antes de subirlo.

**No se permiten dos gastos idénticos** (misma fecha, categoría, descripción y monto) en una misma rendición.

### Corregir o eliminar un gasto
En un borrador, cada gasto tiene **Corregir** y **Eliminar**. En «Corregir» puedes cambiar los datos, **cambiar** el comprobante o **quitarlo**. Eliminar pide confirmación porque no se puede deshacer.

### Ver un comprobante
Pulsa **Ver comprobante: nombre.ext** en el gasto; se abre en otra pestaña.

### Enviar para aprobación
1. Pulsa **Enviar para aprobación**.
2. La aplicación revisa antes de preguntar: debe haber **al menos un gasto** y **todos los comprobantes obligatorios**. Si algo falta, un cuadro te dice qué.
3. Un cuadro te pregunta si confirmas el envío (muestra el total y a quién va).
4. Para enviar necesitas **un aprobador asignado y activo**; si no lo tienes, el mensaje te pide hablar con el administrador.

Una vez enviada, **no se puede editar**.

## 4. Si te rechazan

Recibes un aviso. En el detalle verás el **motivo del rechazo** en rojo y quién lo escribió. Pulsa **Corregir y volver a enviar**: la rendición vuelve a Borrador, la corriges y la envías otra vez. El **historial** conserva todo lo ocurrido.

## 5. Aprobar o rechazar (aprobadores y administradores)

1. Entra a **Por aprobar**. Por defecto ves las **Enviadas** de tu equipo; puedes cambiar el filtro.
2. Abre una rendición: revisa gastos y comprobantes.
3. **Aprobar:** se abre un panel con un comentario **opcional**; pulsa **Aprobar**.
4. **Rechazar:** el **motivo es obligatorio** (mínimo 10 caracteres), porque la persona lo leerá para corregir.

Reglas: solo puedes decidir sobre rendiciones de quienes te tienen como aprobador, **no puedes aprobar la tuya**, y una rendición ya resuelta no se puede volver a resolver.

## 6. Avisos

El menú **Avisos** muestra un número rojo con los avisos sin leer (se actualiza solo cada minuto). Ahí ves cada novedad con su fecha y el botón **Ver rendición**. Puedes marcar uno o **todos** como leídos.

**Avisos en el dispositivo:** en la misma pantalla, **Activar avisos** pide permiso al navegador. Después te llegará una notificación aunque tengas la aplicación cerrada. Si el navegador los bloqueó, debes permitirlos desde la configuración del sitio. Si el servidor no los tiene habilitados, la pantalla lo indica y los avisos siguen apareciendo dentro de la aplicación.

## 7. Administración (solo administradores)

### Usuarios
- **Nuevo usuario:** nombre, correo, clave inicial (8+ con letras y números), rol y aprobador. Comparte la clave por un canal seguro.
- **Editar:** cambia nombre, rol, aprobador y estado (Activo/Inactivo). Un usuario inactivo no puede ingresar y su sesión abierta deja de funcionar.
- Protecciones: no puedes desactivarte ni quitarte el rol de administrador a ti mismo; siempre debe quedar **al menos un administrador activo**; un aprobador **con personas asignadas** no puede bajar de rol ni desactivarse hasta reasignarlas.

### Categorías
- **Nueva categoría:** nombre, descripción opcional, **límite por gasto** y si el **comprobante es obligatorio**.
- **Editar:** además puedes marcarla **Inactiva**: ya no se ofrece en gastos nuevos, pero los gastos existentes no cambian.

## 8. Instalar en el celular o en la computadora

En un navegador compatible (Chrome, Edge, Safari en iOS 16.4+) usa «Instalar aplicación» o «Agregar a la pantalla de inicio». Se abrirá como una aplicación propia, con su ícono.

## 9. Mensajes frecuentes

| Mensaje | Qué hacer |
|---|---|
| «No tienes un aprobador asignado…» | Pídele al administrador que te asigne uno |
| «Faltan comprobantes en gastos de categorías que lo exigen» | Corrige esos gastos y adjunta el PDF/PNG/JPG |
| «El monto supera el límite de la categoría» | Baja el monto o usa otra categoría |
| «Ya tienes 10 borradores…» | Envía o elimina alguno |
| «Tu sesión venció» | Ingresa de nuevo |
| «No se pudo conectar con el servidor» | Revisa tu conexión y vuelve a intentar |
