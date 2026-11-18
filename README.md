# RendiGastos

Proyecto de portafolio de Aarón Suárez.

Aplicación web instalable (PWA) para **rendir gastos con aprobación**: cada persona registra sus gastos con su comprobante, los agrupa en una rendición y la envía a su aprobador, que la aprueba o la rechaza con un motivo. Todos reciben avisos dentro de la aplicación y, si lo activan, en el dispositivo.

- **Frontend:** Angular 19 + Angular Material (componentes standalone, signals, formularios reactivos) como PWA (manifiesto, service worker, avisos push).
- **Backend:** .NET 9, ASP.NET Core con Controllers, Dapper, JWT, Serilog, Web Push (VAPID).
- **Base de datos:** SQL Server (LocalDB en desarrollo), todo con procedimientos almacenados.
- **Publicación:** backend y base en MonsterASP.NET, frontend en Cloudflare Pages.

## Demostración en línea

- Aplicación: https://rendigastos.pages.dev (entrar con un clic con las cuentas de demostración, ver más abajo)
- API y documentación Swagger: https://asportafolioproy02.runasp.net

## Qué demuestra este proyecto

| Tema | Dónde verlo |
|---|---|
| Tres roles con permisos verificados **en la base** en cada acción (no solo en el token) | `backend/src/RendiGastos.Api/Servicios/Autenticacion.cs` (`Seguridad`) |
| Flujo de estados con reglas de negocio en procedimientos (enviar, aprobar, rechazar, reabrir) | `backend/database/05_…` a `07_…` |
| Validaciones de gasto: límite por categoría, fecha, duplicados, comprobante obligatorio | `06_pa_gasto.sql`, `Servicios/Negocio.cs` |
| Comprobantes validados por firma real del archivo (PDF, PNG, JPG) | `Servicios/ValidadorArchivo.cs`, `core/validadores.ts` |
| PWA instalable con avisos push (Web Push, VAPID) y bandeja de avisos con contador | `frontend/public/sw.js`, `core/notificaciones.service.ts`, `Servicios/Notificador.cs` |
| Interfaz sin diálogos del navegador: avisos y confirmaciones centrados, formularios en panel lateral | `shared/avisos.service.ts`, `shared/panel-lateral.component.ts` |
| Formularios con campos obligatorios marcados, ayuda y validación antes de llamar al servidor | `features/rendiciones/panel-gasto.component.*` |
| Pruebas automáticas (209 de backend, 61 de frontend, 5 de punta a punta) | `backend/tests`, `*.spec.ts` |

## Cuentas de demostración

Si `Demo:Habilitado` es verdadero, la API crea al iniciar tres cuentas (clave `Demo2026!`, cambiable con `Demo:Clave`) y la pantalla de ingreso ofrece entrar con un clic:

| Rol | Correo | Qué puede hacer |
|---|---|---|
| Empleado | `empleado@demo.rendigastos.app` | Crear gastos y rendiciones y enviarlas a su aprobador |
| Aprobador | `aprobador@demo.rendigastos.app` | Lo anterior + aprobar o rechazar las de su equipo |
| Administrador | `admin@demo.rendigastos.app` | Lo anterior + gestionar usuarios y categorías |

## Cómo correrlo en local

Requisitos: .NET 9 SDK, Node 20+, SQL Server LocalDB (o cualquier SQL Server), `sqlcmd`.

```powershell
# 1. Base de datos (crea "rendigastos" en LocalDB)
cd backend\database
.\instalar.ps1 -CrearBd -ConPruebas

# 2. API (http://localhost:5240); en Development ya trae clave de desarrollo y cuentas demo
cd ..\src\RendiGastos.Api
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run

# 3. Frontend (http://localhost:4200)
cd ..\..\..\frontend
npm install
npm start
```

## Pruebas

```powershell
.\pruebas-completas.ps1             # todas
.\pruebas-completas.ps1 -Capas 1    # solo tests y linters
```

- 209 pruebas de backend, 61 de frontend y 5 de punta a punta (Playwright).
- Mutation testing con Stryker: 95,24 % en el backend y 79 % en el frontend.
- ESLint y analizadores de .NET sin advertencias.

El detalle está en el [manual técnico](docs/manual-tecnico.md).

## Documentación

- [Manual de usuario](docs/manual-usuario.md)
- [Manual técnico](docs/manual-tecnico.md)

## Autor

Aarón Suárez, desarrollador frontend (Angular).
