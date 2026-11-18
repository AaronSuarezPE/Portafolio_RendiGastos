using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RendiGastos.Api.Servicios;

namespace RendiGastos.Api.Datos;

public interface IFabricaConexion
{
    SqlConnection Crear();
}

public sealed class FabricaConexion(IConfiguration configuracion) : IFabricaConexion
{
    private readonly string _cadena = configuracion.GetConnectionString("RendiGastos")
        ?? throw new InvalidOperationException("Falta la cadena de conexión «RendiGastos».");

    public SqlConnection Crear() => new(_cadena);
}

public sealed class FilaResultado
{
    public int CodResultado { get; set; }
    public string DscMensaje { get; set; } = string.Empty;
}

public static class ExtensionesDapper
{
    /// <summary>
    /// Ejecuta un procedimiento que responde con cod_resultado y dsc_mensaje.
    /// Un código negativo se convierte en un error de negocio con el mensaje del procedimiento.
    /// </summary>
    public static async Task<FilaResultado> EjecutarAsync(this SqlConnection conexion, string procedimiento, object? parametros)
    {
        var resultado = await conexion.QuerySingleAsync<FilaResultado>(
            procedimiento, parametros, commandType: CommandType.StoredProcedure);

        if (resultado.CodResultado < 0)
            throw new ErrorNegocio(resultado.DscMensaje);

        return resultado;
    }

    public static Task<IEnumerable<T>> ListarAsync<T>(this SqlConnection conexion, string procedimiento, object? parametros = null) =>
        conexion.QueryAsync<T>(procedimiento, parametros, commandType: CommandType.StoredProcedure);
}
