using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RendiGastos.Api.Configuracion;
using RendiGastos.Api.Datos;
using RendiGastos.Api.Modelos;

namespace RendiGastos.Api.Servicios;

public static class Roles
{
    public const int Empleado = 1;
    public const int Aprobador = 2;
    public const int Administrador = 3;

    public static string Nombre(int codRol) => codRol switch
    {
        Aprobador => "Aprobador",
        Administrador => "Administrador",
        _ => "Empleado",
    };
}

public static class Validaciones
{
    public static string Texto(string? valor, string campo, int minimo, int maximo)
    {
        var limpio = (valor ?? string.Empty).Trim();
        if (limpio.Length == 0) throw new ErrorNegocio($"{campo} es obligatorio.");
        if (limpio.Length < minimo) throw new ErrorNegocio($"{campo} debe tener al menos {minimo} caracteres.");
        if (limpio.Length > maximo) throw new ErrorNegocio($"{campo} no puede superar {maximo} caracteres.");
        return limpio;
    }

    public static string TextoOpcional(string? valor, string campo, int maximo)
    {
        var limpio = (valor ?? string.Empty).Trim();
        if (limpio.Length > maximo) throw new ErrorNegocio($"{campo} no puede superar {maximo} caracteres.");
        return limpio;
    }

    public static string Email(string? valor)
    {
        var limpio = (valor ?? string.Empty).Trim().ToLowerInvariant();
        if (limpio.Length == 0) throw new ErrorNegocio("El correo es obligatorio.");
        if (limpio.Length > 150) throw new ErrorNegocio("El correo no puede superar 150 caracteres.");
        try
        {
            var direccion = new MailAddress(limpio);
            if (direccion.Address != limpio || !limpio.Contains('.', StringComparison.Ordinal) ||
                limpio.LastIndexOf('.') < limpio.IndexOf('@'))
                throw new FormatException();
        }
        catch (FormatException)
        {
            throw new ErrorNegocio("El correo no tiene un formato válido (ejemplo: nombre@empresa.com).");
        }
        return limpio;
    }

    public static string Clave(string? valor)
    {
        var clave = valor ?? string.Empty;
        if (clave.Length < 8) throw new ErrorNegocio("La clave debe tener al menos 8 caracteres.");
        if (clave.Length > 100) throw new ErrorNegocio("La clave no puede superar 100 caracteres.");
        if (!clave.Any(char.IsLetter) || !clave.Any(char.IsDigit))
            throw new ErrorNegocio("La clave debe incluir letras y números.");
        return clave;
    }
}

public interface ITokenService
{
    string Crear(FilaUsuario usuario);
}

public sealed class TokenService(IOptions<OpcionesJwt> opciones) : ITokenService
{
    private readonly OpcionesJwt _o = opciones.Value;

    public string Crear(FilaUsuario usuario)
    {
        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Clave));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.CodUsrio.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, usuario.NomUsrio),
            new Claim(ClaimTypes.Role, Roles.Nombre(usuario.CodRol)),
        };
        var token = new JwtSecurityToken(_o.Emisor, _o.Audiencia, claims,
            expires: DateTime.UtcNow.AddMinutes(_o.MinutosVigencia),
            signingCredentials: new SigningCredentials(llave, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>Verifica contra la base el rol y el estado actual de quien actúa (el token solo identifica).</summary>
public interface ISeguridad
{
    Task<FilaUsuario> ExigirAsync(int codUsuario, params int[] rolesPermitidos);
}

public sealed class Seguridad(IUsuarioRepositorio usuarios) : ISeguridad
{
    public async Task<FilaUsuario> ExigirAsync(int codUsuario, params int[] rolesPermitidos)
    {
        var u = await usuarios.ObtenerPorCodigoAsync(codUsuario)
            ?? throw new ErrorNegocio("Tu sesión ya no es válida. Ingresa de nuevo.", StatusCodes.Status401Unauthorized);
        if (u.FlgActvo != "S")
            throw new ErrorNegocio("Tu cuenta está desactivada. Contacta al administrador.", StatusCodes.Status403Forbidden);
        if (rolesPermitidos.Length > 0 && !rolesPermitidos.Contains(u.CodRol))
            throw new ErrorNegocio("No tienes permiso para realizar esta acción.", StatusCodes.Status403Forbidden);
        return u;
    }
}

public interface IAutenticacionService
{
    Task<RespuestaSesion> IngresarAsync(SolicitudLogin solicitud);
    Task<RespuestaSesion> RegistrarAsync(SolicitudRegistro solicitud);
}

public sealed class AutenticacionService(
    IUsuarioRepositorio usuarios, IClaveService claves, ITokenService tokens, IOptions<OpcionesJwt> jwt) : IAutenticacionService
{
    public async Task<RespuestaSesion> IngresarAsync(SolicitudLogin solicitud)
    {
        var email = (solicitud.Email ?? string.Empty).Trim().ToLowerInvariant();
        var clave = solicitud.Clave ?? string.Empty;
        if (email.Length == 0 || clave.Length == 0)
            throw new ErrorNegocio("Escribe tu correo y tu clave.");

        var u = await usuarios.ObtenerPorEmailAsync(email);
        // Mismo mensaje para correo inexistente y clave errónea: no revela qué cuentas existen.
        if (u is null || !claves.Verificar(clave, u.NomClaveHsh))
            throw new ErrorNegocio("Correo o clave incorrectos.", StatusCodes.Status401Unauthorized);
        if (u.FlgActvo != "S")
            throw new ErrorNegocio("Tu cuenta está desactivada. Contacta al administrador.", StatusCodes.Status403Forbidden);

        await usuarios.RegistrarIngresoAsync(u.CodUsrio);
        return Sesion(u);
    }

    public async Task<RespuestaSesion> RegistrarAsync(SolicitudRegistro solicitud)
    {
        var nombre = Validaciones.Texto(solicitud.Nombre, "El nombre", 3, 100);
        var email = Validaciones.Email(solicitud.Email);
        var clave = Validaciones.Clave(solicitud.Clave);

        var codigo = await usuarios.AgregarAsync(nombre, email, claves.Generar(clave), Roles.Empleado, null, 0);
        var u = await usuarios.ObtenerPorCodigoAsync(codigo)
            ?? throw new ErrorNegocio("No se pudo crear la cuenta.");
        return Sesion(u);
    }

    private RespuestaSesion Sesion(FilaUsuario u) =>
        new(tokens.Crear(u), u.CodUsrio, u.NomUsrio, Roles.Nombre(u.CodRol), u.CodRol, jwt.Value.MinutosVigencia);
}
