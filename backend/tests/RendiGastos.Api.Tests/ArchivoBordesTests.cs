using RendiGastos.Api.Servicios;

namespace RendiGastos.Api.Tests;

// Mismos bordes que en DocuIA: el validador de archivos es idéntico.

public sealed class ValidadorArchivoBordesTests
{
    private static byte[] Con(byte[] firma, int largo)
    {
        var datos = new byte[largo];
        firma.CopyTo(datos, 0);
        return datos;
    }

    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF];

    [Theory]
    [InlineData("a.pdf", "application/pdf", 4)]
    [InlineData("a.png", "image/png", 8)]
    [InlineData("a.jpg", "image/jpeg", 3)]
    [InlineData("A.JPEG", "image/jpeg", 3)]
    public void El_archivo_mas_corto_posible_con_su_firma_es_valido(string nombre, string tipo, int largo)
    {
        var firma = tipo switch { "application/pdf" => Pdf, "image/png" => Png, _ => Jpg };
        var r = ValidadorArchivo.Validar(nombre, Con(firma, largo), 5);
        Assert.True(r.EsValido, r.Error);
        Assert.Equal(tipo, r.TipoMime);
    }

    [Theory]
    [InlineData("a.pdf", 0)] [InlineData("a.pdf", 1)] [InlineData("a.pdf", 2)] [InlineData("a.pdf", 3)]
    [InlineData("a.png", 0)] [InlineData("a.png", 1)] [InlineData("a.png", 2)] [InlineData("a.png", 3)]
    [InlineData("a.jpg", 0)] [InlineData("a.jpg", 1)] [InlineData("a.jpg", 2)]
    public void Si_falla_cualquier_byte_de_la_firma_el_archivo_se_rechaza(string nombre, int posicion)
    {
        var firma = (byte[])(nombre.EndsWith("pdf") ? Pdf : nombre.EndsWith("png") ? Png : Jpg).Clone();
        firma[posicion] ^= 0x01;
        var r = ValidadorArchivo.Validar(nombre, Con(firma, 16), 5);
        Assert.False(r.EsValido);
        Assert.Contains("no corresponde a su extensión", r.Error);
    }

    [Theory]
    [InlineData("a.pdf", new byte[] { 0x25, 0x50, 0x44 })]
    [InlineData("a.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0, 0, 0 })]
    [InlineData("a.jpg", new byte[] { 0xFF, 0xD8 })]
    public void Un_archivo_demasiado_corto_para_tener_firma_completa_se_rechaza(string nombre, byte[] datos)
    {
        Assert.False(ValidadorArchivo.Validar(nombre, datos, 5).EsValido);
    }

    [Fact]
    public void Una_firma_de_otro_tipo_con_la_extension_equivocada_se_rechaza()
    {
        Assert.False(ValidadorArchivo.Validar("a.png", Con(Pdf, 16), 5).EsValido);
        Assert.False(ValidadorArchivo.Validar("a.pdf", Con(Jpg, 16), 5).EsValido);
    }

    [Fact]
    public void El_tamano_se_mide_en_megabytes_y_el_maximo_exacto_se_acepta()
    {
        Assert.Equal("El archivo está vacío.", ValidadorArchivo.Validar("a.pdf", [], 5).Error);
        Assert.True(ValidadorArchivo.Validar("a.pdf", Con(Pdf, 2 * 1024 * 1024), 2).EsValido);
        var r = ValidadorArchivo.Validar("a.pdf", Con(Pdf, (2 * 1024 * 1024) + 1), 2);
        Assert.Equal("El archivo supera el máximo de 2 MB.", r.Error);
    }

    [Theory]
    [InlineData("a.xlsx")]
    [InlineData("sin-extension")]
    [InlineData(null)]
    public void Una_extension_no_permitida_o_ausente_se_rechaza_con_la_ayuda_de_formatos(string? nombre)
    {
        var r = ValidadorArchivo.Validar(nombre, Con(Pdf, 16), 5);
        Assert.False(r.EsValido);
        Assert.StartsWith("Formato no permitido.", r.Error);
    }
}
