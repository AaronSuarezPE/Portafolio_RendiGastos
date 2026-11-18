namespace RendiGastos.Api.Servicios;

/// <summary>Regla de negocio incumplida o dato no encontrado; el mensaje es apto para mostrar a la persona.</summary>
public sealed class ErrorNegocio(string mensaje, int estado = StatusCodes.Status400BadRequest) : Exception(mensaje)
{
    public int Estado { get; } = estado;
}
