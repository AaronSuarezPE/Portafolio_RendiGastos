namespace RendiGastos.Api.Servicios;

public sealed record ResultadoValidacion(bool EsValido, string? Error, string? TipoMime)
{
    public static ResultadoValidacion Valido(string tipoMime) => new(true, null, tipoMime);
    public static ResultadoValidacion Invalido(string error) => new(false, error, null);
}

/// <summary>
/// Valida el archivo por tamaño, extensión y firma real (no se confía solo en el nombre).
/// </summary>
public static class ValidadorArchivo
{
    private static readonly Dictionary<string, string> TiposPorExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
    };

    public static ResultadoValidacion Validar(string? nombre, byte[] contenido, int tamanoMaximoMb)
    {
        if (contenido.Length == 0)
            return ResultadoValidacion.Invalido("El archivo está vacío.");

        if (contenido.Length > tamanoMaximoMb * 1024L * 1024L)
            return ResultadoValidacion.Invalido($"El archivo supera el máximo de {tamanoMaximoMb} MB.");

        var extension = Path.GetExtension(nombre ?? string.Empty);
        if (!TiposPorExtension.TryGetValue(extension, out var tipoEsperado))
            return ResultadoValidacion.Invalido("Formato no permitido. Solo se aceptan documentos PDF e imágenes PNG o JPG. Si tu documento está en otro formato (Excel, Word, etc.), guárdalo como PDF.");

        var tipoReal = DetectarTipo(contenido);
        if (tipoReal is null || tipoReal != tipoEsperado)
            return ResultadoValidacion.Invalido("El contenido del archivo no corresponde a su extensión. Solo se aceptan documentos PDF e imágenes PNG o JPG reales.");

        return ResultadoValidacion.Valido(tipoReal);
    }

    private static string? DetectarTipo(byte[] datos)
    {
        if (datos.Length >= 4 && datos[0] == 0x25 && datos[1] == 0x50 && datos[2] == 0x44 && datos[3] == 0x46)
            return "application/pdf";

        if (datos.Length >= 8 && datos[0] == 0x89 && datos[1] == 0x50 && datos[2] == 0x4E && datos[3] == 0x47)
            return "image/png";

        if (datos.Length >= 3 && datos[0] == 0xFF && datos[1] == 0xD8 && datos[2] == 0xFF)
            return "image/jpeg";

        return null;
    }
}
