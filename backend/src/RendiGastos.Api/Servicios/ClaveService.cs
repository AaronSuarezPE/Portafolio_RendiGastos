using System.Security.Cryptography;

namespace RendiGastos.Api.Servicios;

public interface IClaveService
{
    string Generar(string clave);
    bool Verificar(string clave, string? hashGuardado);
}

/// <summary>
/// Guarda la clave como PBKDF2-SHA256 con sal aleatoria. Formato: iteraciones.sal.hash (base64).
/// </summary>
public sealed class ClaveService : IClaveService
{
    private const int Iteraciones = 210_000;
    private const int BytesSal = 16;
    private const int BytesHash = 32;

    public string Generar(string clave)
    {
        var sal = RandomNumberGenerator.GetBytes(BytesSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, BytesHash);
        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string clave, string? hashGuardado)
    {
        if (string.IsNullOrEmpty(hashGuardado))
            return false;

        var partes = hashGuardado.Split('.');
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
            return false;

        try
        {
            var sal = Convert.FromBase64String(partes[1]);
            var esperado = Convert.FromBase64String(partes[2]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(clave, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
