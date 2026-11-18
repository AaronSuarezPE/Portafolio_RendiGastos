using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace RendiGastos.Api.Tests.Infraestructura;

/// <summary>
/// Crea una base de datos temporal en LocalDB con los scripts del proyecto, levanta la API
/// contra ella y la elimina al terminar. No toca la base de desarrollo.
/// </summary>
public sealed class FabricaApi : WebApplicationFactory<Program>
{
    private const string Servidor = @"(localdb)\MSSQLLocalDB";
    private readonly string _baseDatos = $"rendigastos_pruebas_{Guid.NewGuid():N}"[..28];

    public FabricaApi()
    {
        try
        {
            CrearBaseDatos();
        }
        catch
        {
            EliminarBaseDatos();
            throw;
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__RendiGastos", Cadena(_baseDatos));
        Environment.SetEnvironmentVariable("Jwt__Clave", "clave-solo-para-pruebas-automaticas-0123456789");
        Environment.SetEnvironmentVariable("Limites__LoginPorMinuto", "100000");
        Environment.SetEnvironmentVariable("Demo__Habilitado", "true");
    }

    private static string Cadena(string baseDatos) =>
        $"Server={Servidor};Database={baseDatos};Integrated Security=true;TrustServerCertificate=true";

    private void CrearBaseDatos()
    {
        using (var master = new SqlConnection(Cadena("master")))
        {
            master.Open();
            using var crear = new SqlCommand($"create database [{_baseDatos}]", master);
            crear.ExecuteNonQuery();
        }

        var carpeta = Path.Combine(AppContext.BaseDirectory, "database");
        using var conexion = new SqlConnection(Cadena(_baseDatos));
        conexion.Open();

        foreach (var archivo in Directory.GetFiles(carpeta, "*.sql").OrderBy(f => f))
        {
            var texto = File.ReadAllText(archivo);
            foreach (var lote in Regex.Split(texto, @"^\s*go\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(lote)) continue;
                using var comando = new SqlCommand(lote, conexion);
                comando.ExecuteNonQuery();
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) EliminarBaseDatos();
    }

    private void EliminarBaseDatos()
    {
        SqlConnection.ClearAllPools();
        using var master = new SqlConnection(Cadena("master"));
        master.Open();
        using var borrar = new SqlCommand(
            $"if db_id('{_baseDatos}') is not null begin " +
            $"alter database [{_baseDatos}] set single_user with rollback immediate; drop database [{_baseDatos}]; end",
            master);
        borrar.ExecuteNonQuery();
    }
}
