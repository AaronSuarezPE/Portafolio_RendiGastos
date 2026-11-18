using Microsoft.Extensions.Options;
using RendiGastos.Api.Configuracion;
using RendiGastos.Api.Datos;

namespace RendiGastos.Api.Servicios;

/// <summary>
/// Crea al iniciar las cuentas de demostración (administrador, aprobador y empleado) si todavía no existen.
/// Solo actúa cuando Demo:Habilitado es verdadero; sirve para que quien revise el portafolio pueda probar los tres roles.
/// </summary>
public sealed class SembradorDemo(
    IUsuarioRepositorio usuarios, IClaveService claves, IOptions<OpcionesDemo> opciones, ILogger<SembradorDemo> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var o = opciones.Value;
        if (!o.Habilitado)
            return;

        try
        {
            var admin = await Asegurar("Admin Demo", "admin@demo.rendigastos.app", Roles.Administrador, null, o.Clave);
            var aprobador = await Asegurar("Ana Aprobadora", "aprobador@demo.rendigastos.app", Roles.Aprobador, admin, o.Clave);
            await Asegurar("Eduardo Empleado", "empleado@demo.rendigastos.app", Roles.Empleado, aprobador, o.Clave);
            logger.LogInformation("Cuentas de demostración listas.");
        }
        catch (Exception ex)
        {
            // La demostración no debe impedir que la API arranque.
            logger.LogError(ex, "No se pudieron crear las cuentas de demostración.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<int> Asegurar(string nombre, string email, int codRol, int? codAprobador, string clave)
    {
        var existente = await usuarios.ObtenerPorEmailAsync(email);
        if (existente is not null)
            return existente.CodUsrio;

        return await usuarios.AgregarAsync(nombre, email, claves.Generar(clave), codRol, codAprobador, 0);
    }
}
