namespace RendiGastos.Api.Modelos;

// ---------- Solicitudes ----------

public sealed record SolicitudLogin(string? Email, string? Clave);

public sealed record SolicitudRegistro(string? Nombre, string? Email, string? Clave);

public sealed record SolicitudUsuarioAdmin(string? Nombre, string? Email, string? Clave, int CodRol, int? CodAprobador);

public sealed record SolicitudEditarUsuario(string? Nombre, int CodRol, int? CodAprobador, bool Activo);

public sealed record SolicitudCategoria(string? Nombre, string? Descripcion, decimal Limite, bool ComprobanteObligatorio, bool Activa = true);

public sealed record SolicitudRendicion(string? Titulo);

public sealed record SolicitudGasto(int CodCategoria, string? Fecha, string? Descripcion, decimal Monto);

public sealed record SolicitudRechazo(string? Motivo);

public sealed record SolicitudComentario(string? Comentario);

public sealed record SolicitudSuscripcionPush(string? Url, string? ClaveP256, string? ClaveAuth);

public sealed record SolicitudBajaPush(string? Url);

// ---------- Respuestas ----------

public sealed record RespuestaSesion(string Token, int CodUsuario, string Nombre, string Rol, int CodRol, int MinutosVigencia);

public sealed record UsuarioDto(
    int CodUsuario, string Nombre, string Email, int CodRol, string Rol,
    int? CodAprobador, string? Aprobador, bool Activo);

public sealed record CategoriaDto(
    int CodCategoria, string Nombre, string Descripcion, decimal Limite, bool ComprobanteObligatorio, bool Activa);

public sealed record RendicionResumenDto(
    int CodRendicion, string Titulo, int CodUsuario, string Solicitante, int CodEstado, string Estado,
    decimal Total, int CantidadGastos, string FechaCreacion, string FechaEnvio);

public sealed record GastoDto(
    int CodGasto, int CodCategoria, string Categoria, bool ComprobanteObligatorio, string Fecha,
    string Descripcion, decimal Monto, string? Comprobante, string? TipoComprobante, int TamanoComprobante);

public sealed record HistorialDto(
    int CodHistorial, int CodAccion, string Accion, int CodUsuario, string Usuario,
    string Comentario, string Fecha, string Hora);

public sealed record RendicionDetalleDto(
    int CodRendicion, string Titulo, int CodUsuario, string Solicitante, string? Aprobador, int CodEstado, string Estado,
    decimal Total, string MotivoRechazo, string FechaCreacion, string FechaEnvio, string FechaResolucion, string Resolvio,
    IReadOnlyList<GastoDto> Gastos, IReadOnlyList<HistorialDto> Historial);

public sealed record AvisoDto(
    int CodAviso, int CodRendicion, string Titulo, string Mensaje, bool Leido, string Fecha, string Hora);

public sealed record ClavePushDto(bool Habilitado, string ClavePublica);

public sealed record ComprobanteArchivo(string Nombre, string Tipo, byte[] Contenido);
