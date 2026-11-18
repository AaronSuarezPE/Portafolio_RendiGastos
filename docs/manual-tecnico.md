# RendiGastos — Manual técnico

## 1. Arquitectura

```
Navegador / PWA (Angular 19)  ──HTTPS+JWT──▶  API .NET 9 (Controllers)  ──procedimientos──▶  SQL Server
        │                                            │
   service worker  ◀── Web Push (VAPID) ◀────────────┘  (avisos)
```

- **Frontend** (`frontend/`): Angular 19, componentes standalone, signals, formularios reactivos, Angular Material 19. Carga perezosa por pantalla.
- **Backend** (`backend/src/RendiGastos.Api`): ASP.NET Core Controllers. Capas: `Controllers` (HTTP) → `Servicios` (reglas y permisos) → `Datos` (repositorios con Dapper que solo llaman procedimientos almacenados).
- **Base de datos** (`backend/database`): SQL Server; toda la lógica de datos vive en procedimientos `pa_rgs_*`.
- Sin dependencias de pago: LocalDB, Web Push propio (VAPID) y hosting gratuito.

## 2. Base de datos

Estándar NTS: minúsculas, esquema `dbo`, abreviaturas 3_5, ids manuales con `updlock, holdlock` (sin identity), fechas como `varchar` dd/mm/aaaa, banderas `S/N`, códigos numéricos con tablas maestras, cada procedimiento con cabecera y resultado `cod_resultado`/`dsc_mensaje` (negativo = error de negocio; sin parámetros OUTPUT).

| Script | Contenido |
|---|---|
| `00_crear_bd.sql` | Crea la base `rendigastos` |
| `01_tablas.sql` | Tablas |
| `02_datos_maestros.sql` | Roles, estados, acciones de historial y 5 categorías iniciales |
| `03_pa_usuario.sql` | Alta, consulta, listado, edición y registro de ingreso de usuarios |
| `04_pa_categoria.sql` | Listado, alta y edición de categorías |
| `05_pa_rendicion.sql` | Alta, edición, eliminación, listado (`P` propias / `A` por aprobar) y detalle (3 resultados: cabecera, gastos, historial) |
| `06_pa_gasto.sql` | Gastos y comprobantes |
| `07_pa_flujo.sql` | Enviar, aprobar, rechazar y reabrir |
| `08_pa_aviso.sql` | Avisos y suscripciones push |
| `instalar.ps1` | Instalador (`-CrearBd`, `-ConPruebas`, `-Servidor`, `-BaseDatos`, `-Usuario`, `-Clave`) |
| `pruebas/prueba_procedimientos.sql` | Prueba de los procedimientos (se ejecuta con `-ConPruebas`) |

### Tablas

| Tabla | Tipo | Notas |
|---|---|---|
| `ta_rgs_mst_rol`, `ta_rgs_mst_estdo_rndcn`, `ta_rgs_mst_accn_hstrl` | Maestras fijas | 1 Empleado/2 Aprobador/3 Administrador; 1 Borrador/2 Enviada/3 Aprobada/4 Rechazada |
| `ta_rgs_mst_mov_usrio` | Maestra con movimiento | Usuario, rol, aprobador, activo, hash de clave, auditoría `cnr_*` |
| `ta_rgs_mst_mov_ctgra` | Maestra con movimiento | Límite por gasto y comprobante obligatorio |
| `ta_rgs_rndcn` | Operativa | Rendición con total, motivo de rechazo y fechas |
| `ta_rgs_gasto` | Operativa | Gasto de una rendición |
| `ta_rgs_gasto_cmprb` | Operativa | Comprobante (`varbinary`) de un gasto |
| `ta_rgs_hstrl` | Operativa | Historial de acciones por rendición |
| `ta_rgs_avso` | Operativa | Avisos por usuario |
| `ta_rgs_sbscr_push` | Operativa | Suscripciones Web Push por dispositivo |

### Reglas de negocio en los procedimientos

- **Rendición:** solo el dueño la edita/elimina y solo en Borrador; máximo 10 borradores por persona y 50 gastos por rendición; título de 3 a 100 caracteres.
- **Gasto:** categoría activa; fecha válida, no futura y de hasta 90 días atrás; monto mayor que 0 y no superior al límite de la categoría; no se repite el mismo gasto (fecha, categoría, monto, descripción).
- **Envío:** requiere aprobador asignado, activo y con rol Aprobador/Administrador; al menos un gasto; comprobante en las categorías que lo exigen. Registra historial y crea el aviso del aprobador. Devuelve el código del aprobador.
- **Aprobación/rechazo:** solo el aprobador asignado (o un administrador), nunca sobre la propia rendición, solo si está Enviada; rechazo exige motivo de 10+ caracteres. Devuelven el código del dueño para avisarle.
- **Usuarios:** correo único; nadie es su propio aprobador; el aprobador debe ser Aprobador/Administrador activo; un administrador no puede desactivarse ni quitarse el rol; debe quedar al menos un administrador activo; un aprobador con personas activas asignadas no baja de rol ni se desactiva.
- **Detalle (`sel_rndcn`):** devuelve la cabecera solo si quien consulta es el dueño, el aprobador asignado o un administrador; en otro caso no devuelve filas (la API responde 404 para no revelar su existencia).

## 3. API

Base: `/api/v1/rendigastos/{Controller}/{Acción}` (estándar NTS: `Get`, `GetList`, `Post…`, `Put…`, `Delete…`). Errores en JSON `{ "mensaje": "…" }` con 400 (regla de negocio), 401, 403, 404, 429 o 500.

| Controlador | Acciones |
|---|---|
| `Autenticacion` (límite de intentos por IP) | `PostIngresar`, `PostRegistrar` (siempre crea Empleado) |
| `Usuarios` (admin) | `GetList`, `GetComboAprobadores`, `PostAgregar`, `PutActualizar/{id}` |
| `Categorias` | `GetList?soloActivas` (el admin ve también inactivas), `PostAgregar`, `PutActualizar/{id}` (admin) |
| `Rendiciones` | `GetList`, `GetListPorAprobar`, `Get/{id}`, `PostAgregar`, `PutActualizar/{id}`, `DeleteEliminar/{id}` |
| `Rendiciones` (gastos) | `PostAgregarGasto/{codRendicion}`, `PutActualizarGasto/{id}`, `DeleteEliminarGasto/{id}` |
| `Rendiciones` (comprobantes) | `PostSubirComprobante/{codGasto}` (multipart `archivo`), `DeleteQuitarComprobante/{id}`, `GetComprobante/{id}` |
| `Rendiciones` (flujo) | `PostEnviar/{id}`, `PostAprobar/{id}`, `PostRechazar/{id}`, `PostReabrir/{id}` |
| `Avisos` | `GetList`, `GetContador`, `PutMarcarLeido/{id}`, `PutMarcarTodosLeidos`, `GetClavePush`, `PostSuscribirPush`, `PostDesuscribirPush` |
| `Salud` | `Get` (sin sesión) |

### Seguridad

- **JWT** con código de usuario, nombre y rol. **El token solo identifica**: `Seguridad.ExigirAsync` vuelve a leer en la base el rol y el estado activo en cada acción, de modo que desactivar o cambiar de rol a alguien surte efecto de inmediato.
- Contraseñas con PBKDF2-SHA256 (210 000 iteraciones, sal aleatoria). Mismo mensaje para correo inexistente y clave errónea.
- Propiedad de los datos validada en los procedimientos (dueño / aprobador asignado / administrador), incluidos los comprobantes.
- **Archivos:** `ValidadorArchivo` revisa tamaño (5 MB), extensión (pdf, png, jpg, jpeg) y **firma real** (un Excel renombrado se rechaza); se descarga con `X-Content-Type-Options: nosniff`. El nombre se reduce a `Path.GetFileName`.
- CORS solo para los orígenes configurados; límite de cuerpo de 12 MB; límite de intentos de ingreso.
- Errores inesperados responden con un mensaje genérico y quedan en el registro con su traza.

### Avisos push

`NotificadorPush` envía con la librería `WebPush` (VAPID) a las suscripciones del usuario destino tras enviar/aprobar/rechazar. **Nunca rompe la operación principal**: sus fallos se registran como advertencia y las suscripciones caducadas (404/410) se eliminan. Sin claves VAPID, `GetClavePush` responde `habilitado: false` y los avisos siguen en la bandeja interna.

## 4. Configuración

| Clave | Uso |
|---|---|
| `ConnectionStrings:RendiGastos` | Cadena de conexión |
| `Jwt:Clave` | Secreto de firma, **mínimo 32 caracteres** (la API no arranca sin él). En producción por variable de entorno `Jwt__Clave` |
| `Jwt:MinutosVigencia` | Duración de la sesión (120) |
| `Archivos:TamanoMaximoMb` | Tamaño máximo del comprobante (5) |
| `Push:ClavePublica`, `Push:ClavePrivada`, `Push:Contacto` | Claves VAPID. Generar gratis con `npx web-push generate-vapid-keys` |
| `Demo:Habilitado`, `Demo:Clave` | Crea al iniciar las 3 cuentas de demostración |
| `Limites:LoginPorMinuto` | Intentos de ingreso por IP y minuto (10) |
| `Cors:Origenes` | Sitios autorizados a llamar a la API |

`appsettings.Development.json` trae una cadena LocalDB, una clave JWT de desarrollo y `Demo:Habilitado = true`. **Esos valores no sirven en producción.**

El registro diario queda en `Logs/rendigastos-AAAAMMDD.log` (30 días) y en consola.

## 5. Frontend

```
src/app/
  core/       api.service, auth.service/guard/interceptor, validadores, formato, notificaciones.service, errores
  shared/     avisos.service + dialogo-aviso (cuadros centrados), panel-lateral, panel-texto
  features/   acceso, rendiciones (lista, detalle, panel-gasto), avisos, admin (usuarios, categorías)
public/       manifest.webmanifest, sw.js, iconos/
```

Convenciones de interfaz:
- **Nada de `alert`, `confirm`, `prompt` ni avisos que desaparecen solos.** Avisos y confirmaciones: cuadro centrado (`AvisosService.exito/error/info/confirmar`).
- **Formularios nuevos en panel lateral derecho** (`app-panel-lateral`), con título y botones fijos y cuerpo desplazable; se cierra con la cruz, Escape o el fondo.
- Obligatorios con asterisco rojo y validación **antes** de llamar al servidor; todos los campos con *placeholder*; los opcionales también se validan si traen dato; archivos validados por extensión, tamaño, vacío y firma, y mostrados una vez elegidos; ningún fallo queda en silencio (errores de red con mensaje y «Reintentar»).
- Validaciones dependientes (monto contra el límite de la categoría, rango de fechas) son validadores del formulario, de modo que el campo se marca en error de verdad.

### PWA

- `manifest.webmanifest`: nombre, `display: standalone`, colores e iconos (192, 512 y *maskable*). Los iconos se generaron con un script propio, sin dependencias.
- `sw.js`: precarga la «cáscara», páginas con red primero y recursos estáticos con caché y actualización en segundo plano; **la API nunca se guarda**. Maneja `push` (muestra la notificación) y `notificationclick` (abre `/avisos`).
- Se registra solo en producción (`!isDevMode()`), para que la copia guardada no oculte cambios durante el desarrollo.
- `NotificacionesService`: contador de no leídos cada 60 s mientras hay sesión, y alta/baja de la suscripción push.
- **Hosting:** la aplicación necesita **HTTPS** y una regla de reescritura a `index.html` (SPA); `sw.js` debe servirse desde la raíz.

## 6. Pruebas

Un solo comando corre todo y termina con error si algo falla:

```powershell
.\pruebas-completas.ps1             # todas las pruebas
.\pruebas-completas.ps1 -Capas 1    # solo tests y linters
```

| Qué | Para qué sirve | Herramienta | Resultado |
|---|---|---|---|
| Tests y linters | Procedimientos SQL, compilación sin advertencias, pruebas del backend y del frontend, ESLint | script SQL, xUnit, Karma/Jasmine, angular-eslint | 209 pruebas de backend, 61 de frontend, lint sin errores |
| Mutation testing | Comprobar que las pruebas detectan errores: se altera el código a propósito y alguna prueba debe fallar | Stryker.NET (backend) y StrykerJS con Vitest (frontend) | backend 95,24 %, frontend 79 % (mínimo: 70 %) |
| Pruebas E2E | El flujo real: navegador, API y base de datos | Playwright con Chrome | 5 pruebas |

- **Mutation testing:** en el backend se aplica a la lógica (claves, validaciones, validador de archivos y autenticación); los controladores, la conexión, los procesos de fondo y la siembra de datos quedan fuera. En el frontend, a `core/` (formato, validadores y errores). Los mutantes que sobreviven son equivalentes. Los informes salen en `StrykerOutput/` y `frontend/reports/mutacion/`, que no se suben al repositorio.
- **E2E:** acceso, permisos por rol y el flujo empleado → aprobador (rechazo, corrección, aprobación y avisos). Necesitan la base instalada (`backend\database\instalar.ps1 -CrearBd`); el script levanta y apaga la API y el frontend.
- **Analizadores de .NET:** `Directory.Build.props` y `.editorconfig` hacen que toda advertencia rompa la compilación. Detectaron formatos de números y fechas que dependían de la cultura del servidor; ahora usan `CultureInfo.InvariantCulture`.

| Capa | Cantidad | Qué cubre |
|---|---|---|
| Backend (xUnit) | 209 | Unitarias (validaciones, hash de clave, token, validador de archivos) y de flujo contra la API real con una **base temporal en LocalDB** (`FabricaApi`): login y registro, roles y permisos, usuario desactivado con token vigente, flujo completo enviar→rechazar→reabrir→aprobar con avisos, aislamiento entre personas, comprobantes (formatos, firma falsa, tamaño, ciclo completo), gastos inválidos (14 casos), duplicados, push |
| Frontend (Karma/Jasmine) | 61 | Validadores y formato, sesión y *guards* por rol, panel de texto, panel de gasto (límite, fechas, comprobante obligatorio, archivos), pantalla de acceso, interceptor, avisos |

```powershell
dotnet test backend\tests\RendiGastos.Api.Tests
cd frontend; npx ng test --watch=false --browsers=ChromeHeadless
```

Cada prueba de flujo crea su propio empleado para no agotar el tope de 10 borradores de la cuenta demo.

**Verificado a mano en el navegador:** ingreso con cuenta demo, creación de rendición, validación del panel (vacío, límite, fechas, archivos), envío con confirmación, bandeja y contador del aprobador, rechazo, pantallas de administración y ausencia de desbordamiento horizontal a 375 px.

**No verificado en el navegador integrado:** el registro del *service worker* y los avisos push reales (el navegador del panel no los admite). Hay que probarlos en Chrome/Edge con HTTPS y claves VAPID reales antes de darlos por buenos.

## 7. Publicación

- **Base de datos y API:** MonsterASP.NET (SQL Server y hosting .NET). Se instalan los scripts `01` a `08` en la base remota (sin `00_` ni pruebas), se publica con `dotnet publish -c Release` y se sube el contenido a la raíz del sitio. La configuración (`ConnectionStrings:RendiGastos`, `Jwt:Clave`, `Cors:Origenes`, `Demo:Habilitado`) va en un `appsettings.Production.json` que no se sube al repositorio.
- **Frontend:** Cloudflare Pages. Se ajusta `apiUrl` en `src/environments/environment.ts`, se ejecuta `npx ng build` y se publica `dist/frontend/browser` con `wrangler pages deploy`. El archivo `_redirects` envía todas las rutas a `index.html`. Se necesita HTTPS para el service worker y los avisos push.
- **Swagger:** la API publica su documentación en la raíz (`/swagger`).
- **Hospedaje compartido:** algunos bloquean PUT y DELETE con un firewall. Si al editar o eliminar aparece un error de CORS sin registro en la API, hay que revisar esa regla.
- **Avisos push:** requieren claves VAPID (`Push:*`). Sin ellas los avisos siguen viéndose dentro de la aplicación.
