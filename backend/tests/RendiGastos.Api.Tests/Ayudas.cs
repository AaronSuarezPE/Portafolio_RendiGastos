using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RendiGastos.Api.Modelos;
using RendiGastos.Api.Tests.Infraestructura;

namespace RendiGastos.Api.Tests;

public static class Datos
{
    public const string Clave = "Demo2026!";
    public const string Admin = "admin@demo.rendigastos.app";
    public const string Aprobador = "aprobador@demo.rendigastos.app";
    public const string Empleado = "empleado@demo.rendigastos.app";

    public static string Fecha(int diasAtras = 0) =>
        DateTime.Today.AddDays(-diasAtras).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string EmailNuevo() => $"u{Guid.NewGuid():N}"[..12] + "@prueba.com";

    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];
    public static readonly byte[] Pdf = "%PDF-1.4\n1 0 obj\n<<>>\nendobj\n"u8.ToArray();
}

/// <summary>Cliente de pruebas ya autenticado con una cuenta.</summary>
public sealed class Cliente
{
    public HttpClient Http { get; }
    public int CodUsuario { get; private set; }

    private Cliente(HttpClient http) => Http = http;

    public static async Task<Cliente> IngresarAsync(FabricaApi fabrica, string email, string clave = Datos.Clave)
    {
        var c = new Cliente(fabrica.CreateClient());
        var r = await c.Http.PostAsJsonAsync("api/v1/rendigastos/Autenticacion/PostIngresar", new SolicitudLogin(email, clave));
        r.EnsureSuccessStatusCode();
        var sesion = (await r.Content.ReadFromJsonAsync<RespuestaSesion>())!;
        c.Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Token);
        var yo = (await c.Http.GetFromJsonAsync<IReadOnlyList<RendicionResumenDto>>("api/v1/rendigastos/Rendiciones/GetList"))!;
        _ = yo;
        return c;
    }

    public static Cliente SinSesion(FabricaApi fabrica) => new(fabrica.CreateClient());

    public Task<HttpResponseMessage> Post(string ruta, object? cuerpo = null) =>
        Http.PostAsJsonAsync($"api/v1/rendigastos/{ruta}", cuerpo ?? new { });

    public Task<HttpResponseMessage> Put(string ruta, object cuerpo) =>
        Http.PutAsJsonAsync($"api/v1/rendigastos/{ruta}", cuerpo);

    public Task<HttpResponseMessage> Get(string ruta) => Http.GetAsync($"api/v1/rendigastos/{ruta}");

    public Task<HttpResponseMessage> Delete(string ruta) => Http.DeleteAsync($"api/v1/rendigastos/{ruta}");

    public async Task<T> Leer<T>(string ruta)
    {
        var r = await Get(ruta);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<T>())!;
    }

    public async Task<int> CrearRendicion(string titulo = "Viaje de prueba")
    {
        var r = await Post("Rendiciones/PostAgregar", new SolicitudRendicion(titulo));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("codRendicion").GetInt32();
    }

    public async Task<int> CrearGasto(int codRendicion, int codCategoria, decimal monto = 50, string? descripcion = null, int diasAtras = 1)
    {
        var r = await Post($"Rendiciones/PostAgregarGasto/{codRendicion}",
            new SolicitudGasto(codCategoria, Datos.Fecha(diasAtras), descripcion ?? $"Gasto {Guid.NewGuid():N}"[..14], monto));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("codGasto").GetInt32();
    }

    public async Task<HttpResponseMessage> SubirComprobante(int codGasto, string nombre, byte[] contenido)
    {
        using var form = new MultipartFormDataContent();
        var parte = new ByteArrayContent(contenido);
        form.Add(parte, "archivo", nombre);
        return await Http.PostAsync($"api/v1/rendigastos/Rendiciones/PostSubirComprobante/{codGasto}", form);
    }

    public async Task<int> CodigoCategoria(string nombre)
    {
        var cats = await Leer<IReadOnlyList<CategoriaDto>>("Categorias/GetList");
        return cats.First(c => c.Nombre == nombre).CodCategoria;
    }

    public Task<RendicionDetalleDto> Detalle(int codRendicion) => Leer<RendicionDetalleDto>($"Rendiciones/Get/{codRendicion}");

    public async Task<string> Mensaje(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("mensaje").GetString()!;
}
