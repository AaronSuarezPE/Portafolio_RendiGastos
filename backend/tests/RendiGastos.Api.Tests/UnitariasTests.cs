using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using RendiGastos.Api.Configuracion;
using RendiGastos.Api.Datos;
using RendiGastos.Api.Servicios;

namespace RendiGastos.Api.Tests;

public sealed class UnitariasTests
{
    [Theory]
    [InlineData("ana@correo.com", true)]
    [InlineData("  ANA@Correo.COM  ", true)]
    [InlineData("sin-arroba", false)]
    [InlineData("a@b", false)]
    [InlineData("a b@correo.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Email_se_valida_y_se_normaliza(string? entrada, bool valido)
    {
        if (valido)
            Assert.Equal(entrada!.Trim().ToLowerInvariant(), Validaciones.Email(entrada));
        else
            Assert.Throws<ErrorNegocio>(() => Validaciones.Email(entrada));
    }

    [Theory]
    [InlineData("Clave1234", true)]
    [InlineData("corta1", false)]
    [InlineData("soloLetrasLargas", false)]
    [InlineData("1234567890", false)]
    [InlineData(null, false)]
    public void Clave_exige_largo_letras_y_numeros(string? clave, bool valida)
    {
        if (valida) Assert.Equal(clave, Validaciones.Clave(clave));
        else Assert.Throws<ErrorNegocio>(() => Validaciones.Clave(clave));
    }

    [Fact]
    public void Texto_recorta_espacios_y_respeta_minimo_y_maximo()
    {
        Assert.Equal("Hola", Validaciones.Texto("  Hola  ", "El campo", 3, 10));
        Assert.Throws<ErrorNegocio>(() => Validaciones.Texto("  ab ", "El campo", 3, 10));
        Assert.Throws<ErrorNegocio>(() => Validaciones.Texto("   ", "El campo", 1, 10));
        Assert.Throws<ErrorNegocio>(() => Validaciones.Texto(new string('x', 11), "El campo", 1, 10));
        Assert.Equal(string.Empty, Validaciones.TextoOpcional(null, "El campo", 10));
    }

    [Fact]
    public void Clave_se_guarda_con_hash_distinto_cada_vez_y_se_verifica()
    {
        var servicio = new ClaveService();
        var a = servicio.Generar("Clave1234");
        var b = servicio.Generar("Clave1234");

        Assert.NotEqual(a, b);
        Assert.DoesNotContain("Clave1234", a);
        Assert.True(servicio.Verificar("Clave1234", a));
        Assert.False(servicio.Verificar("Otra12345", a));
        Assert.False(servicio.Verificar("Clave1234", null));
        Assert.False(servicio.Verificar("Clave1234", "basura"));
    }

    [Theory]
    [InlineData(1, "Empleado")]
    [InlineData(2, "Aprobador")]
    [InlineData(3, "Administrador")]
    public void Token_lleva_el_codigo_el_nombre_y_el_rol(int codRol, string rol)
    {
        var opciones = Options.Create(new OpcionesJwt { Clave = "clave-solo-para-pruebas-automaticas-0123456789" });
        var token = new TokenService(opciones).Crear(new FilaUsuario { CodUsrio = 42, NomUsrio = "Ana", CodRol = codRol });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("42", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("Ana", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(rol, jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    [Theory]
    [InlineData("boleta.pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 1 }, true)]
    [InlineData("foto.JPG", new byte[] { 0xFF, 0xD8, 0xFF, 1 }, true)]
    [InlineData("foto.jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 1 }, true)]
    [InlineData("imagen.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 }, true)]
    [InlineData("hoja.xlsx", new byte[] { 0x50, 0x4B, 3, 4 }, false)]
    [InlineData("falso.pdf", new byte[] { 1, 2, 3, 4, 5 }, false)]
    [InlineData("cruzado.png", new byte[] { 0x25, 0x50, 0x44, 0x46, 1 }, false)]
    [InlineData("vacio.pdf", new byte[] { }, false)]
    public void Validador_de_archivos_revisa_extension_y_contenido_real(string nombre, byte[] contenido, bool valido)
    {
        Assert.Equal(valido, ValidadorArchivo.Validar(nombre, contenido, 5).EsValido);
    }

    [Fact]
    public void Roles_tienen_nombre_legible()
    {
        Assert.Equal("Empleado", Roles.Nombre(1));
        Assert.Equal("Aprobador", Roles.Nombre(2));
        Assert.Equal("Administrador", Roles.Nombre(3));
        Assert.Equal("Empleado", Roles.Nombre(99));
    }
}
