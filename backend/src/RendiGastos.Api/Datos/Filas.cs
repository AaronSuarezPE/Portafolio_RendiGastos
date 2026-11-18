namespace RendiGastos.Api.Datos;

// Una clase por conjunto de resultados de los procedimientos almacenados.
// Dapper relaciona cada columna (cod_rndcn) con su propiedad (CodRndcn).

public sealed class FilaUsuario
{
    public int CodUsrio { get; set; }
    public string NomUsrio { get; set; } = string.Empty;
    public string NomEmail { get; set; } = string.Empty;
    public string? NomClaveHsh { get; set; }
    public int CodRol { get; set; }
    public string NomRol { get; set; } = string.Empty;
    public int? CodAprbdr { get; set; }
    public string? NomAprbdr { get; set; }
    public string FlgActvo { get; set; } = "S";
}

public sealed class FilaCategoria
{
    public int CodCtgra { get; set; }
    public string NomCtgra { get; set; } = string.Empty;
    public string DscCtgra { get; set; } = string.Empty;
    public decimal MntLmte { get; set; }
    public string FlgCmprbOblgt { get; set; } = "N";
    public string FlgActvo { get; set; } = "S";
}

public sealed class FilaRendicionResumen
{
    public int CodRndcn { get; set; }
    public string NomTtlo { get; set; } = string.Empty;
    public int CodUsrio { get; set; }
    public string NomUsrio { get; set; } = string.Empty;
    public int CodEstdoRndcn { get; set; }
    public string NomEstdoRndcn { get; set; } = string.Empty;
    public decimal MntTtal { get; set; }
    public int NumGastos { get; set; }
    public string FecCrcn { get; set; } = string.Empty;
    public string FecEnvio { get; set; } = string.Empty;
}

public sealed class FilaRendicionCabecera
{
    public int CodRndcn { get; set; }
    public string NomTtlo { get; set; } = string.Empty;
    public int CodUsrio { get; set; }
    public string NomUsrio { get; set; } = string.Empty;
    public int? CodAprbdr { get; set; }
    public string? NomAprbdr { get; set; }
    public int CodEstdoRndcn { get; set; }
    public string NomEstdoRndcn { get; set; } = string.Empty;
    public decimal MntTtal { get; set; }
    public string DscMtvoRchzo { get; set; } = string.Empty;
    public string FecCrcn { get; set; } = string.Empty;
    public string FecEnvio { get; set; } = string.Empty;
    public string FecRsln { get; set; } = string.Empty;
    public string NomRsltr { get; set; } = string.Empty;
}

public sealed class FilaGasto
{
    public int CodGasto { get; set; }
    public int CodCtgra { get; set; }
    public string NomCtgra { get; set; } = string.Empty;
    public string FlgCmprbOblgt { get; set; } = "N";
    public string FecGasto { get; set; } = string.Empty;
    public string DscGasto { get; set; } = string.Empty;
    public decimal MntGasto { get; set; }
    public string NomArchv { get; set; } = string.Empty;
    public string TpoCntnd { get; set; } = string.Empty;
    public int NumTamno { get; set; }
}

public sealed class FilaHistorial
{
    public int CodHstrl { get; set; }
    public int CodAccnHstrl { get; set; }
    public string NomAccnHstrl { get; set; } = string.Empty;
    public int CodUsrio { get; set; }
    public string NomUsrio { get; set; } = string.Empty;
    public string DscCmntr { get; set; } = string.Empty;
    public string FecHstrl { get; set; } = string.Empty;
    public string HorHstrl { get; set; } = string.Empty;
}

public sealed class FilaComprobante
{
    public string NomArchv { get; set; } = string.Empty;
    public string TpoCntnd { get; set; } = string.Empty;
    public byte[] BinCntnd { get; set; } = [];
}

public sealed class FilaAviso
{
    public int CodAvso { get; set; }
    public int CodRndcn { get; set; }
    public string NomTtlo { get; set; } = string.Empty;
    public string DscMnsje { get; set; } = string.Empty;
    public string FlgLeido { get; set; } = "N";
    public string FecAvso { get; set; } = string.Empty;
    public string HorAvso { get; set; } = string.Empty;
}

public sealed class FilaSuscripcion
{
    public int CodSbscr { get; set; }
    public string NomUrlPtnto { get; set; } = string.Empty;
    public string NomLlaveP256 { get; set; } = string.Empty;
    public string NomLlaveAuth { get; set; } = string.Empty;
}

public sealed class FilaConteo
{
    public int NumNleidos { get; set; }
}
