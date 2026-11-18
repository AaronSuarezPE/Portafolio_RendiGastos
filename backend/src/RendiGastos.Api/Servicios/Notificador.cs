using System.Text.Json;
using Microsoft.Extensions.Options;
using RendiGastos.Api.Configuracion;
using RendiGastos.Api.Datos;
using WebPush;

namespace RendiGastos.Api.Servicios;

/// <summary>
/// Envía el aviso push a los dispositivos de una persona. El aviso dentro de la aplicación ya lo
/// guardan los procedimientos; esto es un complemento y nunca debe romper la operación principal.
/// </summary>
public interface INotificador
{
    Task AvisarAsync(int codUsuario, string titulo, string mensaje);
}

public sealed class NotificadorPush(
    IAvisoRepositorio avisos, IOptions<OpcionesPush> opciones, ILogger<NotificadorPush> logger) : INotificador
{
    private readonly OpcionesPush _o = opciones.Value;

    public async Task AvisarAsync(int codUsuario, string titulo, string mensaje)
    {
        if (!_o.Habilitado || codUsuario <= 0)
            return;

        try
        {
            var suscripciones = await avisos.ListarSuscripcionesAsync(codUsuario);
            if (suscripciones.Count == 0)
                return;

            var vapid = new VapidDetails(_o.Contacto, _o.ClavePublica, _o.ClavePrivada);
            var cliente = new WebPushClient();
            var carga = JsonSerializer.Serialize(new { titulo, mensaje });

            foreach (var s in suscripciones)
            {
                try
                {
                    await cliente.SendNotificationAsync(new PushSubscription(s.NomUrlPtnto, s.NomLlaveP256, s.NomLlaveAuth), carga, vapid);
                }
                catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Gone or System.Net.HttpStatusCode.NotFound)
                {
                    // El dispositivo ya no existe: se retira para no insistir.
                    await avisos.EliminarSuscripcionAsync(s.NomUrlPtnto);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "No se pudo enviar el aviso push a la suscripción {Codigo}.", s.CodSbscr);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falló el envío de avisos push al usuario {Usuario}.", codUsuario);
        }
    }
}

public sealed class NotificadorNulo : INotificador
{
    public Task AvisarAsync(int codUsuario, string titulo, string mensaje) => Task.CompletedTask;
}
