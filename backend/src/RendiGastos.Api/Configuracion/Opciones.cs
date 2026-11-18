namespace RendiGastos.Api.Configuracion;

public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";

    public string Clave { get; set; } = string.Empty;
    public string Emisor { get; set; } = "rendigastos";
    public string Audiencia { get; set; } = "rendigastos";
    public int MinutosVigencia { get; set; } = 120;
}

public sealed class OpcionesArchivos
{
    public const string Seccion = "Archivos";

    public int TamanoMaximoMb { get; set; } = 5;
}

/// <summary>Claves VAPID para los avisos push. Sin ellas los avisos siguen viéndose dentro de la aplicación.</summary>
public sealed class OpcionesPush
{
    public const string Seccion = "Push";

    public string ClavePublica { get; set; } = string.Empty;
    public string ClavePrivada { get; set; } = string.Empty;
    public string Contacto { get; set; } = "mailto:contacto@rendigastos.example";

    public bool Habilitado => !string.IsNullOrWhiteSpace(ClavePublica) && !string.IsNullOrWhiteSpace(ClavePrivada);
}

/// <summary>Cuentas de demostración que se crean al iniciar si la base está vacía.</summary>
public sealed class OpcionesDemo
{
    public const string Seccion = "Demo";

    public bool Habilitado { get; set; }
    public string Clave { get; set; } = "Demo2026!";
}
