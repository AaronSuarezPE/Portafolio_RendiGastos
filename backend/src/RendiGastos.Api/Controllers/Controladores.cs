using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RendiGastos.Api.Modelos;
using RendiGastos.Api.Servicios;

namespace RendiGastos.Api.Controllers;

public abstract class ControladorBase : ControllerBase
{
    protected int CodUsuario =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var c)
            ? c
            : throw new ErrorNegocio("Tu sesión ya no es válida. Ingresa de nuevo.", StatusCodes.Status401Unauthorized);
}

[ApiController]
[Route("api/v1/rendigastos/Salud")]
public sealed class SaludController : ControllerBase
{
    [HttpGet("Get")]
    public IActionResult Get() => Ok(new { estado = "ok", producto = "RendiGastos" });
}

[ApiController]
[Route("api/v1/rendigastos/Autenticacion")]
[EnableRateLimiting("autenticacion")]
public sealed class AutenticacionController(IAutenticacionService servicio) : ControllerBase
{
    [HttpPost("PostIngresar")]
    public async Task<ActionResult<RespuestaSesion>> PostIngresar(SolicitudLogin solicitud) =>
        Ok(await servicio.IngresarAsync(solicitud));

    [HttpPost("PostRegistrar")]
    public async Task<ActionResult<RespuestaSesion>> PostRegistrar(SolicitudRegistro solicitud) =>
        Ok(await servicio.RegistrarAsync(solicitud));
}

[ApiController]
[Authorize]
[Route("api/v1/rendigastos/Usuarios")]
public sealed class UsuariosController(IUsuarioService servicio) : ControladorBase
{
    [HttpGet("GetList")]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> GetList() => Ok(await servicio.ListarAsync(CodUsuario));

    [HttpGet("GetComboAprobadores")]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> GetComboAprobadores() =>
        Ok(await servicio.ListarAprobadoresAsync(CodUsuario));

    [HttpPost("PostAgregar")]
    public async Task<ActionResult<UsuarioDto>> PostAgregar(SolicitudUsuarioAdmin solicitud) =>
        Ok(await servicio.CrearAsync(CodUsuario, solicitud));

    [HttpPut("PutActualizar/{codUsuario:int}")]
    public async Task<ActionResult<UsuarioDto>> PutActualizar(int codUsuario, SolicitudEditarUsuario solicitud) =>
        Ok(await servicio.EditarAsync(CodUsuario, codUsuario, solicitud));
}

[ApiController]
[Authorize]
[Route("api/v1/rendigastos/Categorias")]
public sealed class CategoriasController(ICategoriaService servicio) : ControladorBase
{
    [HttpGet("GetList")]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> GetList([FromQuery] bool soloActivas = true) =>
        Ok(await servicio.ListarAsync(CodUsuario, soloActivas));

    [HttpPost("PostAgregar")]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> PostAgregar(SolicitudCategoria solicitud) =>
        Ok(await servicio.CrearAsync(CodUsuario, solicitud));

    [HttpPut("PutActualizar/{codCategoria:int}")]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> PutActualizar(int codCategoria, SolicitudCategoria solicitud) =>
        Ok(await servicio.EditarAsync(CodUsuario, codCategoria, solicitud));
}

[ApiController]
[Authorize]
[Route("api/v1/rendigastos/Rendiciones")]
public sealed class RendicionesController(IRendicionService servicio) : ControladorBase
{
    [HttpGet("GetList")]
    public async Task<ActionResult<IReadOnlyList<RendicionResumenDto>>> GetList([FromQuery] int? codEstado) =>
        Ok(await servicio.ListarPropiasAsync(CodUsuario, codEstado));

    [HttpGet("GetListPorAprobar")]
    public async Task<ActionResult<IReadOnlyList<RendicionResumenDto>>> GetListPorAprobar([FromQuery] int? codEstado) =>
        Ok(await servicio.ListarPorAprobarAsync(CodUsuario, codEstado));

    [HttpGet("Get/{codRendicion:int}")]
    public async Task<ActionResult<RendicionDetalleDto>> Get(int codRendicion) =>
        Ok(await servicio.ObtenerAsync(CodUsuario, codRendicion));

    [HttpPost("PostAgregar")]
    public async Task<IActionResult> PostAgregar(SolicitudRendicion solicitud) =>
        Ok(new { codRendicion = await servicio.CrearAsync(CodUsuario, solicitud) });

    [HttpPut("PutActualizar/{codRendicion:int}")]
    public async Task<IActionResult> PutActualizar(int codRendicion, SolicitudRendicion solicitud)
    {
        await servicio.EditarAsync(CodUsuario, codRendicion, solicitud);
        return NoContent();
    }

    [HttpDelete("DeleteEliminar/{codRendicion:int}")]
    public async Task<IActionResult> DeleteEliminar(int codRendicion)
    {
        await servicio.EliminarAsync(CodUsuario, codRendicion);
        return NoContent();
    }

    // ---- Gastos ----

    [HttpPost("PostAgregarGasto/{codRendicion:int}")]
    public async Task<IActionResult> PostAgregarGasto(int codRendicion, SolicitudGasto solicitud) =>
        Ok(new { codGasto = await servicio.AgregarGastoAsync(CodUsuario, codRendicion, solicitud) });

    [HttpPut("PutActualizarGasto/{codGasto:int}")]
    public async Task<IActionResult> PutActualizarGasto(int codGasto, SolicitudGasto solicitud)
    {
        await servicio.EditarGastoAsync(CodUsuario, codGasto, solicitud);
        return NoContent();
    }

    [HttpDelete("DeleteEliminarGasto/{codGasto:int}")]
    public async Task<IActionResult> DeleteEliminarGasto(int codGasto)
    {
        await servicio.EliminarGastoAsync(CodUsuario, codGasto);
        return NoContent();
    }

    // ---- Comprobantes ----

    [HttpPost("PostSubirComprobante/{codGasto:int}")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PostSubirComprobante(int codGasto, IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
            throw new ErrorNegocio("Selecciona un archivo PDF, PNG o JPG.");

        await using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);
        await servicio.SubirComprobanteAsync(CodUsuario, codGasto, archivo.FileName, memoria.ToArray());
        return NoContent();
    }

    [HttpDelete("DeleteQuitarComprobante/{codGasto:int}")]
    public async Task<IActionResult> DeleteQuitarComprobante(int codGasto)
    {
        await servicio.QuitarComprobanteAsync(CodUsuario, codGasto);
        return NoContent();
    }

    [HttpGet("GetComprobante/{codGasto:int}")]
    public async Task<IActionResult> GetComprobante(int codGasto)
    {
        var c = await servicio.DescargarComprobanteAsync(CodUsuario, codGasto);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(c.Contenido, c.Tipo, c.Nombre);
    }

    // ---- Flujo ----

    [HttpPost("PostEnviar/{codRendicion:int}")]
    public async Task<IActionResult> PostEnviar(int codRendicion)
    {
        await servicio.EnviarAsync(CodUsuario, codRendicion);
        return NoContent();
    }

    [HttpPost("PostAprobar/{codRendicion:int}")]
    public async Task<IActionResult> PostAprobar(int codRendicion, SolicitudComentario? solicitud)
    {
        await servicio.AprobarAsync(CodUsuario, codRendicion, solicitud?.Comentario);
        return NoContent();
    }

    [HttpPost("PostRechazar/{codRendicion:int}")]
    public async Task<IActionResult> PostRechazar(int codRendicion, SolicitudRechazo solicitud)
    {
        await servicio.RechazarAsync(CodUsuario, codRendicion, solicitud.Motivo);
        return NoContent();
    }

    [HttpPost("PostReabrir/{codRendicion:int}")]
    public async Task<IActionResult> PostReabrir(int codRendicion)
    {
        await servicio.ReabrirAsync(CodUsuario, codRendicion);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/v1/rendigastos/Avisos")]
public sealed class AvisosController(IAvisoService servicio) : ControladorBase
{
    [HttpGet("GetList")]
    public async Task<ActionResult<IReadOnlyList<AvisoDto>>> GetList([FromQuery] bool soloNoLeidos = false) =>
        Ok(await servicio.ListarAsync(CodUsuario, soloNoLeidos));

    [HttpGet("GetContador")]
    public async Task<IActionResult> GetContador() => Ok(new { noLeidos = await servicio.ContarAsync(CodUsuario) });

    [HttpPut("PutMarcarLeido/{codAviso:int}")]
    public async Task<IActionResult> PutMarcarLeido(int codAviso)
    {
        await servicio.MarcarLeidoAsync(CodUsuario, codAviso);
        return NoContent();
    }

    [HttpPut("PutMarcarTodosLeidos")]
    public async Task<IActionResult> PutMarcarTodosLeidos()
    {
        await servicio.MarcarLeidoAsync(CodUsuario, null);
        return NoContent();
    }

    [HttpGet("GetClavePush")]
    public ActionResult<ClavePushDto> GetClavePush() => Ok(servicio.ClavePublica());

    [HttpPost("PostSuscribirPush")]
    public async Task<IActionResult> PostSuscribirPush(SolicitudSuscripcionPush solicitud)
    {
        await servicio.SuscribirAsync(CodUsuario, solicitud);
        return NoContent();
    }

    [HttpPost("PostDesuscribirPush")]
    public async Task<IActionResult> PostDesuscribirPush(SolicitudBajaPush solicitud)
    {
        await servicio.DesuscribirAsync(CodUsuario, solicitud);
        return NoContent();
    }
}
