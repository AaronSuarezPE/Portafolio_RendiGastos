using System.Globalization;
using RendiGastos.Api.Datos;
using RendiGastos.Api.Modelos;
using Microsoft.Extensions.Options;
using RendiGastos.Api.Configuracion;

namespace RendiGastos.Api.Servicios;

public static class Mapeo
{
    public static UsuarioDto Usuario(FilaUsuario f) =>
        new(f.CodUsrio, f.NomUsrio, f.NomEmail, f.CodRol, f.NomRol, f.CodAprbdr, f.NomAprbdr, f.FlgActvo == "S");

    public static CategoriaDto Categoria(FilaCategoria f) =>
        new(f.CodCtgra, f.NomCtgra, f.DscCtgra, f.MntLmte, f.FlgCmprbOblgt == "S", f.FlgActvo == "S");

    public static RendicionResumenDto Resumen(FilaRendicionResumen f) =>
        new(f.CodRndcn, f.NomTtlo, f.CodUsrio, f.NomUsrio, f.CodEstdoRndcn, f.NomEstdoRndcn,
            f.MntTtal, f.NumGastos, f.FecCrcn, f.FecEnvio);

    public static GastoDto Gasto(FilaGasto f) =>
        new(f.CodGasto, f.CodCtgra, f.NomCtgra, f.FlgCmprbOblgt == "S", f.FecGasto, f.DscGasto, f.MntGasto,
            string.IsNullOrEmpty(f.NomArchv) ? null : f.NomArchv,
            string.IsNullOrEmpty(f.TpoCntnd) ? null : f.TpoCntnd, f.NumTamno);

    public static HistorialDto Historial(FilaHistorial f) =>
        new(f.CodHstrl, f.CodAccnHstrl, f.NomAccnHstrl, f.CodUsrio, f.NomUsrio, f.DscCmntr, f.FecHstrl, f.HorHstrl);

    public static AvisoDto Aviso(FilaAviso f) =>
        new(f.CodAvso, f.CodRndcn, f.NomTtlo, f.DscMnsje, f.FlgLeido == "S", f.FecAvso, f.HorAvso);
}

// ---------------- Usuarios (administración) ----------------

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(int codActor);
    Task<IReadOnlyList<UsuarioDto>> ListarAprobadoresAsync(int codActor);
    Task<UsuarioDto> CrearAsync(int codActor, SolicitudUsuarioAdmin solicitud);
    Task<UsuarioDto> EditarAsync(int codActor, int codUsuario, SolicitudEditarUsuario solicitud);
}

public sealed class UsuarioService(IUsuarioRepositorio usuarios, ISeguridad seguridad, IClaveService claves) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(int codActor)
    {
        await seguridad.ExigirAsync(codActor, Roles.Administrador);
        return (await usuarios.ListarAsync(false)).Select(Mapeo.Usuario).ToList();
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAprobadoresAsync(int codActor)
    {
        await seguridad.ExigirAsync(codActor, Roles.Administrador);
        return (await usuarios.ListarAsync(true)).Select(Mapeo.Usuario).ToList();
    }

    public async Task<UsuarioDto> CrearAsync(int codActor, SolicitudUsuarioAdmin s)
    {
        await seguridad.ExigirAsync(codActor, Roles.Administrador);
        var nombre = Validaciones.Texto(s.Nombre, "El nombre", 3, 100);
        var email = Validaciones.Email(s.Email);
        var clave = Validaciones.Clave(s.Clave);
        ValidarRol(s.CodRol);

        var codigo = await usuarios.AgregarAsync(nombre, email, claves.Generar(clave), s.CodRol, s.CodAprobador, codActor);
        return Mapeo.Usuario((await usuarios.ObtenerPorCodigoAsync(codigo))!);
    }

    public async Task<UsuarioDto> EditarAsync(int codActor, int codUsuario, SolicitudEditarUsuario s)
    {
        await seguridad.ExigirAsync(codActor, Roles.Administrador);
        var nombre = Validaciones.Texto(s.Nombre, "El nombre", 3, 100);
        ValidarRol(s.CodRol);

        await usuarios.EditarAsync(codUsuario, nombre, s.CodRol, s.CodAprobador, s.Activo, codActor);
        return Mapeo.Usuario((await usuarios.ObtenerPorCodigoAsync(codUsuario))!);
    }

    private static void ValidarRol(int codRol)
    {
        if (codRol is < Roles.Empleado or > Roles.Administrador)
            throw new ErrorNegocio("El rol no es válido. Elige Empleado, Aprobador o Administrador.");
    }
}

// ---------------- Categorías ----------------

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaDto>> ListarAsync(int codActor, bool soloActivas);
    Task<IReadOnlyList<CategoriaDto>> CrearAsync(int codActor, SolicitudCategoria solicitud);
    Task<IReadOnlyList<CategoriaDto>> EditarAsync(int codActor, int codCategoria, SolicitudCategoria solicitud);
}

public sealed class CategoriaService(ICategoriaRepositorio categorias, ISeguridad seguridad) : ICategoriaService
{
    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(int codActor, bool soloActivas)
    {
        var actor = await seguridad.ExigirAsync(codActor);
        // Solo el administrador ve también las categorías inactivas.
        var activas = soloActivas || actor.CodRol != Roles.Administrador;
        return (await categorias.ListarAsync(activas)).Select(Mapeo.Categoria).ToList();
    }

    public async Task<IReadOnlyList<CategoriaDto>> CrearAsync(int codActor, SolicitudCategoria s)
    {
        await seguridad.ExigirAsync(codActor, Roles.Administrador);
        var (nombre, descripcion) = Validar(s);
        await categorias.AgregarAsync(nombre, descripcion, s.Limite, s.ComprobanteObligatorio, codActor);
        return (await categorias.ListarAsync(false)).Select(Mapeo.Categoria).ToList();
    }

    public async Task<IReadOnlyList<CategoriaDto>> EditarAsync(int codActor, int codCategoria, SolicitudCategoria s)
    {
        await seguridad.ExigirAsync(codActor, Roles.Administrador);
        var (nombre, descripcion) = Validar(s);
        await categorias.EditarAsync(codCategoria, nombre, descripcion, s.Limite, s.ComprobanteObligatorio, s.Activa, codActor);
        return (await categorias.ListarAsync(false)).Select(Mapeo.Categoria).ToList();
    }

    private static (string Nombre, string Descripcion) Validar(SolicitudCategoria s)
    {
        var nombre = Validaciones.Texto(s.Nombre, "El nombre de la categoría", 3, 60);
        var descripcion = Validaciones.TextoOpcional(s.Descripcion, "La descripción", 200);
        if (s.Limite <= 0 || s.Limite > 1_000_000)
            throw new ErrorNegocio("El límite por gasto debe ser mayor que 0 y no superar 1,000,000.");
        return (nombre, descripcion);
    }
}

// ---------------- Rendiciones ----------------

public interface IRendicionService
{
    Task<int> CrearAsync(int codActor, SolicitudRendicion solicitud);
    Task EditarAsync(int codActor, int codRendicion, SolicitudRendicion solicitud);
    Task EliminarAsync(int codActor, int codRendicion);
    Task<IReadOnlyList<RendicionResumenDto>> ListarPropiasAsync(int codActor, int? codEstado);
    Task<IReadOnlyList<RendicionResumenDto>> ListarPorAprobarAsync(int codActor, int? codEstado);
    Task<RendicionDetalleDto> ObtenerAsync(int codActor, int codRendicion);

    Task<int> AgregarGastoAsync(int codActor, int codRendicion, SolicitudGasto solicitud);
    Task EditarGastoAsync(int codActor, int codGasto, SolicitudGasto solicitud);
    Task EliminarGastoAsync(int codActor, int codGasto);
    Task SubirComprobanteAsync(int codActor, int codGasto, string? nombre, byte[] contenido);
    Task QuitarComprobanteAsync(int codActor, int codGasto);
    Task<ComprobanteArchivo> DescargarComprobanteAsync(int codActor, int codGasto);

    Task EnviarAsync(int codActor, int codRendicion);
    Task AprobarAsync(int codActor, int codRendicion, string? comentario);
    Task RechazarAsync(int codActor, int codRendicion, string? motivo);
    Task ReabrirAsync(int codActor, int codRendicion);
}

public sealed class RendicionService(
    IRendicionRepositorio rendiciones, ISeguridad seguridad, INotificador notificador,
    IOptions<OpcionesArchivos> archivos) : IRendicionService
{
    public async Task<int> CrearAsync(int codActor, SolicitudRendicion s)
    {
        await seguridad.ExigirAsync(codActor);
        var titulo = Validaciones.Texto(s.Titulo, "El título", 3, 100);
        return await rendiciones.AgregarAsync(codActor, titulo);
    }

    public async Task EditarAsync(int codActor, int codRendicion, SolicitudRendicion s)
    {
        await seguridad.ExigirAsync(codActor);
        var titulo = Validaciones.Texto(s.Titulo, "El título", 3, 100);
        await rendiciones.EditarAsync(codRendicion, codActor, titulo);
    }

    public async Task EliminarAsync(int codActor, int codRendicion)
    {
        await seguridad.ExigirAsync(codActor);
        await rendiciones.EliminarAsync(codRendicion, codActor);
    }

    public async Task<IReadOnlyList<RendicionResumenDto>> ListarPropiasAsync(int codActor, int? codEstado)
    {
        await seguridad.ExigirAsync(codActor);
        return (await rendiciones.ListarAsync(codActor, "P", codEstado)).Select(Mapeo.Resumen).ToList();
    }

    public async Task<IReadOnlyList<RendicionResumenDto>> ListarPorAprobarAsync(int codActor, int? codEstado)
    {
        await seguridad.ExigirAsync(codActor, Roles.Aprobador, Roles.Administrador);
        return (await rendiciones.ListarAsync(codActor, "A", codEstado)).Select(Mapeo.Resumen).ToList();
    }

    public async Task<RendicionDetalleDto> ObtenerAsync(int codActor, int codRendicion)
    {
        await seguridad.ExigirAsync(codActor);
        var (c, gastos, historial) = await rendiciones.ObtenerAsync(codRendicion, codActor);
        if (c is null)
            throw new ErrorNegocio("La rendición no existe o no tienes acceso a ella.", StatusCodes.Status404NotFound);

        return new RendicionDetalleDto(c.CodRndcn, c.NomTtlo, c.CodUsrio, c.NomUsrio, c.NomAprbdr, c.CodEstdoRndcn,
            c.NomEstdoRndcn, c.MntTtal, c.DscMtvoRchzo, c.FecCrcn, c.FecEnvio, c.FecRsln, c.NomRsltr,
            gastos.Select(Mapeo.Gasto).ToList(), historial.Select(Mapeo.Historial).ToList());
    }

    public async Task<int> AgregarGastoAsync(int codActor, int codRendicion, SolicitudGasto s)
    {
        await seguridad.ExigirAsync(codActor);
        var (fecha, descripcion) = ValidarGasto(s);
        return await rendiciones.AgregarGastoAsync(codRendicion, codActor, s.CodCategoria, fecha, descripcion, s.Monto);
    }

    public async Task EditarGastoAsync(int codActor, int codGasto, SolicitudGasto s)
    {
        await seguridad.ExigirAsync(codActor);
        var (fecha, descripcion) = ValidarGasto(s);
        await rendiciones.EditarGastoAsync(codGasto, codActor, s.CodCategoria, fecha, descripcion, s.Monto);
    }

    public async Task EliminarGastoAsync(int codActor, int codGasto)
    {
        await seguridad.ExigirAsync(codActor);
        await rendiciones.EliminarGastoAsync(codGasto, codActor);
    }

    public async Task SubirComprobanteAsync(int codActor, int codGasto, string? nombre, byte[] contenido)
    {
        await seguridad.ExigirAsync(codActor);
        var r = ValidadorArchivo.Validar(nombre, contenido, archivos.Value.TamanoMaximoMb);
        if (!r.EsValido) throw new ErrorNegocio(r.Error!);

        var limpio = Path.GetFileName(nombre!.Trim());
        if (limpio.Length > 255) limpio = limpio[^255..];
        await rendiciones.GuardarComprobanteAsync(codGasto, codActor, limpio, r.TipoMime!, contenido);
    }

    public async Task QuitarComprobanteAsync(int codActor, int codGasto)
    {
        await seguridad.ExigirAsync(codActor);
        await rendiciones.QuitarComprobanteAsync(codGasto, codActor);
    }

    public async Task<ComprobanteArchivo> DescargarComprobanteAsync(int codActor, int codGasto)
    {
        await seguridad.ExigirAsync(codActor);
        var f = await rendiciones.ObtenerComprobanteAsync(codGasto, codActor);
        if (f is null || f.BinCntnd.Length == 0)
            throw new ErrorNegocio("El comprobante no existe o no tienes acceso a él.", StatusCodes.Status404NotFound);
        return new ComprobanteArchivo(f.NomArchv, f.TpoCntnd, f.BinCntnd);
    }

    public async Task EnviarAsync(int codActor, int codRendicion)
    {
        var actor = await seguridad.ExigirAsync(codActor);
        var codAprobador = await rendiciones.EnviarAsync(codRendicion, codActor);
        await notificador.AvisarAsync(codAprobador, "Rendición por aprobar",
            $"{actor.NomUsrio} envió una rendición para tu revisión.");
    }

    public async Task AprobarAsync(int codActor, int codRendicion, string? comentario)
    {
        await seguridad.ExigirAsync(codActor, Roles.Aprobador, Roles.Administrador);
        var nota = Validaciones.TextoOpcional(comentario, "El comentario", 500);
        var codDueno = await rendiciones.AprobarAsync(codRendicion, codActor, nota.Length == 0 ? null : nota);
        await notificador.AvisarAsync(codDueno, "Rendición aprobada", "Tu rendición fue aprobada.");
    }

    public async Task RechazarAsync(int codActor, int codRendicion, string? motivo)
    {
        await seguridad.ExigirAsync(codActor, Roles.Aprobador, Roles.Administrador);
        var texto = Validaciones.Texto(motivo, "El motivo del rechazo", 10, 500);
        var codDueno = await rendiciones.RechazarAsync(codRendicion, codActor, texto);
        await notificador.AvisarAsync(codDueno, "Rendición rechazada", "Tu rendición fue rechazada. Revisa el motivo.");
    }

    public async Task ReabrirAsync(int codActor, int codRendicion)
    {
        await seguridad.ExigirAsync(codActor);
        await rendiciones.ReabrirAsync(codRendicion, codActor);
    }

    private static (string Fecha, string Descripcion) ValidarGasto(SolicitudGasto s)
    {
        var descripcion = Validaciones.Texto(s.Descripcion, "La descripción del gasto", 3, 200);

        if (s.CodCategoria <= 0)
            throw new ErrorNegocio("Elige una categoría.");
        if (s.Monto <= 0)
            throw new ErrorNegocio("El monto debe ser mayor que 0.");
        if (s.Monto > 1_000_000)
            throw new ErrorNegocio("El monto no puede superar 1,000,000.");
        if (decimal.Round(s.Monto, 2) != s.Monto)
            throw new ErrorNegocio("El monto admite como máximo 2 decimales.");

        var texto = (s.Fecha ?? string.Empty).Trim();
        if (!DateTime.TryParseExact(texto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new ErrorNegocio("La fecha debe tener el formato dd/mm/aaaa (ejemplo: 25/10/2026).");

        return (texto, descripcion);
    }
}

// ---------------- Avisos ----------------

public interface IAvisoService
{
    Task<IReadOnlyList<AvisoDto>> ListarAsync(int codActor, bool soloNoLeidos);
    Task<int> ContarAsync(int codActor);
    Task MarcarLeidoAsync(int codActor, int? codAviso);
    Task SuscribirAsync(int codActor, SolicitudSuscripcionPush solicitud);
    Task DesuscribirAsync(int codActor, SolicitudBajaPush solicitud);
    ClavePushDto ClavePublica();
}

public sealed class AvisoService(IAvisoRepositorio avisos, ISeguridad seguridad, IOptions<OpcionesPush> push) : IAvisoService
{
    public async Task<IReadOnlyList<AvisoDto>> ListarAsync(int codActor, bool soloNoLeidos)
    {
        await seguridad.ExigirAsync(codActor);
        return (await avisos.ListarAsync(codActor, soloNoLeidos)).Select(Mapeo.Aviso).ToList();
    }

    public async Task<int> ContarAsync(int codActor)
    {
        await seguridad.ExigirAsync(codActor);
        return await avisos.ContarNoLeidosAsync(codActor);
    }

    public async Task MarcarLeidoAsync(int codActor, int? codAviso)
    {
        await seguridad.ExigirAsync(codActor);
        await avisos.MarcarLeidosAsync(codActor, codAviso);
    }

    public async Task SuscribirAsync(int codActor, SolicitudSuscripcionPush s)
    {
        await seguridad.ExigirAsync(codActor);
        var url = (s.Url ?? string.Empty).Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || url.Length > 450)
            throw new ErrorNegocio("La suscripción de avisos no es válida.");
        var p256 = Validaciones.Texto(s.ClaveP256, "La clave de la suscripción", 10, 200);
        var auth = Validaciones.Texto(s.ClaveAuth, "La clave de autenticación", 10, 100);
        await avisos.AgregarSuscripcionAsync(codActor, url, p256, auth);
    }

    public async Task DesuscribirAsync(int codActor, SolicitudBajaPush s)
    {
        await seguridad.ExigirAsync(codActor);
        var url = (s.Url ?? string.Empty).Trim();
        if (url.Length == 0) throw new ErrorNegocio("Falta la dirección de la suscripción.");
        await avisos.EliminarSuscripcionAsync(url);
    }

    public ClavePushDto ClavePublica() => new(push.Value.Habilitado, push.Value.Habilitado ? push.Value.ClavePublica : string.Empty);
}
