using System.Net;
using System.Net.Http.Json;
using RendiGastos.Api.Modelos;
using RendiGastos.Api.Tests.Infraestructura;

namespace RendiGastos.Api.Tests;

public sealed class FlujoApiTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private const string Ruta = "Rendiciones";

    /// <summary>
    /// Cada prueba trabaja con su propio empleado (asignado a la aprobadora demo): así no se agota
    /// el tope de 10 borradores de la cuenta demo ni se mezclan avisos entre pruebas.
    /// </summary>
    private async Task<Cliente> Empleado()
    {
        var admin = await Admin();
        var ana = (await admin.Leer<IReadOnlyList<UsuarioDto>>("Usuarios/GetList")).First(u => u.Email == Datos.Aprobador);
        var email = Datos.EmailNuevo();
        var r = await admin.Post("Usuarios/PostAgregar", new SolicitudUsuarioAdmin("Empleado de prueba", email, "Clave1234", 1, ana.CodUsuario));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await Cliente.IngresarAsync(fabrica, email, "Clave1234");
    }
    private Task<Cliente> Aprobador() => Cliente.IngresarAsync(fabrica, Datos.Aprobador);
    private Task<Cliente> Admin() => Cliente.IngresarAsync(fabrica, Datos.Admin);

    // ---------- Autenticación ----------

    [Theory]
    [InlineData(Datos.Admin, "Administrador")]
    [InlineData(Datos.Aprobador, "Aprobador")]
    [InlineData(Datos.Empleado, "Empleado")]
    public async Task Login_de_cuentas_demo_devuelve_el_rol(string email, string rol)
    {
        var c = Cliente.SinSesion(fabrica);
        var r = await c.Post("Autenticacion/PostIngresar", new SolicitudLogin(email, Datos.Clave));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(rol, (await r.Content.ReadFromJsonAsync<RespuestaSesion>())!.Rol);
    }

    [Fact]
    public async Task Login_con_clave_incorrecta_y_correo_inexistente_dan_el_mismo_mensaje()
    {
        var c = Cliente.SinSesion(fabrica);
        var a = await c.Post("Autenticacion/PostIngresar", new SolicitudLogin(Datos.Empleado, "Incorrecta1"));
        var b = await c.Post("Autenticacion/PostIngresar", new SolicitudLogin("nadie@nada.com", "Incorrecta1"));
        Assert.Equal(HttpStatusCode.Unauthorized, a.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, b.StatusCode);
        Assert.Equal(await c.Mensaje(a), await c.Mensaje(b));
    }

    [Fact]
    public async Task Login_con_campos_vacios_da_400()
    {
        var r = await Cliente.SinSesion(fabrica).Post("Autenticacion/PostIngresar", new SolicitudLogin("", ""));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Rutas_protegidas_sin_token_dan_401()
    {
        var c = Cliente.SinSesion(fabrica);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.Get($"{Ruta}/GetList")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.Get("Usuarios/GetList")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.Get("Avisos/GetList")).StatusCode);
    }

    [Fact]
    public async Task Registro_crea_un_empleado_y_no_permite_repetir_correo()
    {
        var c = Cliente.SinSesion(fabrica);
        var email = Datos.EmailNuevo();
        var r = await c.Post("Autenticacion/PostRegistrar", new SolicitudRegistro("Persona Nueva", email, "Clave1234"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("Empleado", (await r.Content.ReadFromJsonAsync<RespuestaSesion>())!.Rol);

        var repetido = await c.Post("Autenticacion/PostRegistrar", new SolicitudRegistro("Otra Persona", email.ToUpperInvariant(), "Clave1234"));
        Assert.Equal(HttpStatusCode.BadRequest, repetido.StatusCode);
    }

    [Theory]
    [InlineData("", "a@b.com", "Clave1234")]
    [InlineData("Ab", "a@b.com", "Clave1234")]
    [InlineData("Persona Valida", "sin-arroba", "Clave1234")]
    [InlineData("Persona Valida", "a@b", "Clave1234")]
    [InlineData("Persona Valida", "a@b.com", "corta1")]
    [InlineData("Persona Valida", "a@b.com", "sololetrasaqui")]
    [InlineData("Persona Valida", "a@b.com", "123456789")]
    public async Task Registro_con_datos_invalidos_da_400(string nombre, string email, string clave)
    {
        var r = await Cliente.SinSesion(fabrica).Post("Autenticacion/PostRegistrar", new SolicitudRegistro(nombre, email, clave));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Usuario_desactivado_no_puede_ingresar_ni_usar_su_token()
    {
        var admin = await Admin();
        var email = Datos.EmailNuevo();
        var r = await admin.Post("Usuarios/PostAgregar", new SolicitudUsuarioAdmin("Temporal Uno", email, "Clave1234", 1, null));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var u = (await r.Content.ReadFromJsonAsync<UsuarioDto>())!;

        var sesion = await Cliente.IngresarAsync(fabrica, email, "Clave1234");

        var edit = await admin.Put($"Usuarios/PutActualizar/{u.CodUsuario}", new SolicitudEditarUsuario("Temporal Uno", 1, null, false));
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        var login = await Cliente.SinSesion(fabrica).Post("Autenticacion/PostIngresar", new SolicitudLogin(email, "Clave1234"));
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);

        // El token anterior deja de servir porque el estado se verifica en la base.
        Assert.Equal(HttpStatusCode.Forbidden, (await sesion.Get($"{Ruta}/GetList")).StatusCode);
    }

    // ---------- Roles y administración ----------

    [Fact]
    public async Task Solo_el_administrador_gestiona_usuarios_y_categorias()
    {
        foreach (var cliente in new[] { await Empleado(), await Aprobador() })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.Get("Usuarios/GetList")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await cliente.Post("Usuarios/PostAgregar", new SolicitudUsuarioAdmin("Intruso Uno", Datos.EmailNuevo(), "Clave1234", 3, null))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await cliente.Post("Categorias/PostAgregar", new SolicitudCategoria("Categoría Nueva", "", 100, false))).StatusCode);
        }

        var admin = await Admin();
        var lista = await admin.Leer<IReadOnlyList<UsuarioDto>>("Usuarios/GetList");
        Assert.True(lista.Count >= 3);
        Assert.DoesNotContain(lista, u => u.Email.Length == 0);
    }

    [Fact]
    public async Task Administrador_no_puede_desactivarse_ni_quitarse_el_rol()
    {
        var admin = await Admin();
        var yo = (await admin.Leer<IReadOnlyList<UsuarioDto>>("Usuarios/GetList")).First(u => u.Email == Datos.Admin);

        var desactivar = await admin.Put($"Usuarios/PutActualizar/{yo.CodUsuario}", new SolicitudEditarUsuario(yo.Nombre, 3, null, false));
        Assert.Equal(HttpStatusCode.BadRequest, desactivar.StatusCode);

        var degradar = await admin.Put($"Usuarios/PutActualizar/{yo.CodUsuario}", new SolicitudEditarUsuario(yo.Nombre, 1, null, true));
        Assert.Equal(HttpStatusCode.BadRequest, degradar.StatusCode);
    }

    [Fact]
    public async Task Aprobador_con_empleados_asignados_no_puede_bajar_de_rol_ni_desactivarse()
    {
        var admin = await Admin();
        var ana = (await admin.Leer<IReadOnlyList<UsuarioDto>>("Usuarios/GetList")).First(u => u.Email == Datos.Aprobador);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.Put($"Usuarios/PutActualizar/{ana.CodUsuario}", new SolicitudEditarUsuario(ana.Nombre, 1, ana.CodAprobador, true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.Put($"Usuarios/PutActualizar/{ana.CodUsuario}", new SolicitudEditarUsuario(ana.Nombre, 2, ana.CodAprobador, false))).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public async Task Rol_inexistente_da_400(int rol)
    {
        var admin = await Admin();
        var r = await admin.Post("Usuarios/PostAgregar", new SolicitudUsuarioAdmin("Rol Malo", Datos.EmailNuevo(), "Clave1234", rol, null));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Combo_de_aprobadores_solo_trae_aprobadores_y_administradores_activos()
    {
        var admin = await Admin();
        var lista = await admin.Leer<IReadOnlyList<UsuarioDto>>("Usuarios/GetComboAprobadores");
        Assert.NotEmpty(lista);
        Assert.All(lista, u => { Assert.True(u.Activo); Assert.True(u.CodRol is 2 or 3); });
    }

    [Fact]
    public async Task Empleado_solo_ve_categorias_activas_y_el_admin_puede_crear_y_desactivar()
    {
        var admin = await Admin();
        var nombre = $"Cat {Guid.NewGuid():N}"[..14];
        var r = await admin.Post("Categorias/PostAgregar", new SolicitudCategoria(nombre, "Prueba", 80, true));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cat = (await r.Content.ReadFromJsonAsync<IReadOnlyList<CategoriaDto>>())!.First(c => c.Nombre == nombre);
        Assert.True(cat.ComprobanteObligatorio);

        var desactivar = await admin.Put($"Categorias/PutActualizar/{cat.CodCategoria}", new SolicitudCategoria(nombre, "Prueba", 80, true, false));
        Assert.Equal(HttpStatusCode.OK, desactivar.StatusCode);

        var empleado = await Empleado();
        Assert.DoesNotContain(await empleado.Leer<IReadOnlyList<CategoriaDto>>("Categorias/GetList"), c => c.Nombre == nombre);
        Assert.Contains(await admin.Leer<IReadOnlyList<CategoriaDto>>("Categorias/GetList?soloActivas=false"), c => c.Nombre == nombre);
    }

    [Theory]
    [InlineData("", 100)]
    [InlineData("Ab", 100)]
    [InlineData("Nombre válido", 0)]
    [InlineData("Nombre válido", -5)]
    [InlineData("Nombre válido", 2000000)]
    public async Task Categoria_invalida_da_400(string nombre, decimal limite)
    {
        var admin = await Admin();
        var r = await admin.Post("Categorias/PostAgregar", new SolicitudCategoria(nombre, "", limite, false));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ---------- Rendiciones: flujo completo ----------

    [Fact]
    public async Task Flujo_completo_enviar_rechazar_reabrir_y_aprobar_con_avisos()
    {
        var emp = await Empleado();
        var apr = await Aprobador();
        var transporte = await emp.CodigoCategoria("Transporte");
        var alimentacion = await emp.CodigoCategoria("Alimentación");

        var codigo = await emp.CrearRendicion("Visita a cliente");
        await emp.CrearGasto(codigo, transporte, 40);
        var comida = await emp.CrearGasto(codigo, alimentacion, 60);

        // Falta el comprobante obligatorio
        var sinComprobante = await emp.Post($"{Ruta}/PostEnviar/{codigo}");
        Assert.Equal(HttpStatusCode.BadRequest, sinComprobante.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await emp.SubirComprobante(comida, "boleta.png", Datos.Png)).StatusCode);

        var antes = (await apr.Leer<System.Text.Json.JsonElement>("Avisos/GetContador")).GetProperty("noLeidos").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await emp.Post($"{Ruta}/PostEnviar/{codigo}")).StatusCode);
        var despues = (await apr.Leer<System.Text.Json.JsonElement>("Avisos/GetContador")).GetProperty("noLeidos").GetInt32();
        Assert.Equal(antes + 1, despues);

        var detalle = await emp.Detalle(codigo);
        Assert.Equal(2, detalle.CodEstado);
        Assert.Equal(100m, detalle.Total);

        // Ya enviada: no se edita
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.Put($"{Ruta}/PutActualizar/{codigo}", new SolicitudRendicion("Nuevo título"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.Delete($"{Ruta}/DeleteEliminar/{codigo}")).StatusCode);

        // El aprobador la ve en su bandeja
        Assert.Contains(await apr.Leer<IReadOnlyList<RendicionResumenDto>>($"{Ruta}/GetListPorAprobar"), x => x.CodRendicion == codigo);

        // Rechazo: motivo corto no sirve
        Assert.Equal(HttpStatusCode.BadRequest, (await apr.Post($"{Ruta}/PostRechazar/{codigo}", new SolicitudRechazo("no"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await apr.Post($"{Ruta}/PostRechazar/{codigo}", new SolicitudRechazo("Falta el detalle de la visita"))).StatusCode);

        detalle = await emp.Detalle(codigo);
        Assert.Equal(4, detalle.CodEstado);
        Assert.Equal("Falta el detalle de la visita", detalle.MotivoRechazo);

        var avisos = await emp.Leer<IReadOnlyList<AvisoDto>>("Avisos/GetList?soloNoLeidos=true");
        Assert.Contains(avisos, a => a.CodRendicion == codigo);

        // Reabrir, reenviar y aprobar
        Assert.Equal(HttpStatusCode.NoContent, (await emp.Post($"{Ruta}/PostReabrir/{codigo}")).StatusCode);
        Assert.Equal(1, (await emp.Detalle(codigo)).CodEstado);
        Assert.Equal(HttpStatusCode.NoContent, (await emp.Post($"{Ruta}/PostEnviar/{codigo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await apr.Post($"{Ruta}/PostAprobar/{codigo}", new SolicitudComentario("Todo en orden"))).StatusCode);

        detalle = await emp.Detalle(codigo);
        Assert.Equal(3, detalle.CodEstado);
        Assert.True(detalle.Historial.Count >= 5);

        // Una vez aprobada no se puede aprobar ni rechazar otra vez
        Assert.Equal(HttpStatusCode.BadRequest, (await apr.Post($"{Ruta}/PostAprobar/{codigo}", new SolicitudComentario(null))).StatusCode);

        // Marcar avisos como leídos
        Assert.Equal(HttpStatusCode.NoContent, (await emp.Put("Avisos/PutMarcarTodosLeidos", new { })).StatusCode);
        Assert.Equal(0, (await emp.Leer<System.Text.Json.JsonElement>("Avisos/GetContador")).GetProperty("noLeidos").GetInt32());
    }

    [Fact]
    public async Task Empleado_no_puede_aprobar_y_aprobador_ajeno_tampoco()
    {
        var emp = await Empleado();
        var cat = await emp.CodigoCategoria("Transporte");
        var codigo = await emp.CrearRendicion("Para aprobar");
        await emp.CrearGasto(codigo, cat, 20);
        Assert.Equal(HttpStatusCode.NoContent, (await emp.Post($"{Ruta}/PostEnviar/{codigo}")).StatusCode);

        // Empleado intenta aprobar su propia rendición
        Assert.Equal(HttpStatusCode.Forbidden, (await emp.Post($"{Ruta}/PostAprobar/{codigo}", new SolicitudComentario(null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await emp.Get($"{Ruta}/GetListPorAprobar")).StatusCode);

        // Otro aprobador que no es el asignado
        var admin = await Admin();
        var email = Datos.EmailNuevo();
        await admin.Post("Usuarios/PostAgregar", new SolicitudUsuarioAdmin("Aprobador Ajeno", email, "Clave1234", 2, null));
        var ajeno = await Cliente.IngresarAsync(fabrica, email, "Clave1234");
        Assert.Equal(HttpStatusCode.BadRequest, (await ajeno.Post($"{Ruta}/PostAprobar/{codigo}", new SolicitudComentario(null))).StatusCode);
        Assert.DoesNotContain(await ajeno.Leer<IReadOnlyList<RendicionResumenDto>>($"{Ruta}/GetListPorAprobar"), x => x.CodRendicion == codigo);
    }

    [Fact]
    public async Task Otra_persona_no_ve_ni_modifica_rendiciones_ajenas()
    {
        var emp = await Empleado();
        var codigo = await emp.CrearRendicion("Privada");
        var cat = await emp.CodigoCategoria("Transporte");
        var gasto = await emp.CrearGasto(codigo, cat, 10);
        await emp.SubirComprobante(gasto, "r.pdf", Datos.Pdf);

        var email = Datos.EmailNuevo();
        await Cliente.SinSesion(fabrica).Post("Autenticacion/PostRegistrar", new SolicitudRegistro("Curioso Pérez", email, "Clave1234"));
        var otro = await Cliente.IngresarAsync(fabrica, email, "Clave1234");

        Assert.Equal(HttpStatusCode.NotFound, (await otro.Get($"{Ruta}/Get/{codigo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.Get($"{Ruta}/GetComprobante/{gasto}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await otro.Put($"{Ruta}/PutActualizar/{codigo}", new SolicitudRendicion("Robada"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await otro.Delete($"{Ruta}/DeleteEliminar/{codigo}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await otro.Delete($"{Ruta}/DeleteEliminarGasto/{gasto}")).StatusCode);
        Assert.DoesNotContain(await otro.Leer<IReadOnlyList<RendicionResumenDto>>($"{Ruta}/GetList"), x => x.CodRendicion == codigo);
    }

    [Fact]
    public async Task Envio_sin_aprobador_o_sin_gastos_da_400()
    {
        var email = Datos.EmailNuevo();
        await Cliente.SinSesion(fabrica).Post("Autenticacion/PostRegistrar", new SolicitudRegistro("Sin Aprobador", email, "Clave1234"));
        var sin = await Cliente.IngresarAsync(fabrica, email, "Clave1234");
        var cat = await sin.CodigoCategoria("Transporte");
        var codigo = await sin.CrearRendicion("Sin aprobador");
        await sin.CrearGasto(codigo, cat, 10);
        var r = await sin.Post($"{Ruta}/PostEnviar/{codigo}");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("aprobador", await sin.Mensaje(r), StringComparison.OrdinalIgnoreCase);

        var emp = await Empleado();
        var vacia = await emp.CrearRendicion("Vacía");
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.Post($"{Ruta}/PostEnviar/{vacia}")).StatusCode);
    }

    [Fact]
    public async Task Borrador_se_edita_y_se_elimina_con_sus_gastos()
    {
        var emp = await Empleado();
        var cat = await emp.CodigoCategoria("Transporte");
        var codigo = await emp.CrearRendicion("Original");
        var gasto = await emp.CrearGasto(codigo, cat, 15);

        Assert.Equal(HttpStatusCode.NoContent, (await emp.Put($"{Ruta}/PutActualizar/{codigo}", new SolicitudRendicion("Cambiado"))).StatusCode);
        Assert.Equal("Cambiado", (await emp.Detalle(codigo)).Titulo);

        Assert.Equal(HttpStatusCode.NoContent,
            (await emp.Put($"{Ruta}/PutActualizarGasto/{gasto}", new SolicitudGasto(cat, Datos.Fecha(2), "Taxi al aeropuerto", 25))).StatusCode);
        Assert.Equal(25m, (await emp.Detalle(codigo)).Total);

        Assert.Equal(HttpStatusCode.NoContent, (await emp.Delete($"{Ruta}/DeleteEliminarGasto/{gasto}")).StatusCode);
        Assert.Equal(0m, (await emp.Detalle(codigo)).Total);

        Assert.Equal(HttpStatusCode.NoContent, (await emp.Delete($"{Ruta}/DeleteEliminar/{codigo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await emp.Get($"{Ruta}/Get/{codigo}")).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public async Task Titulo_invalido_da_400(string titulo)
    {
        var emp = await Empleado();
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.Post($"{Ruta}/PostAgregar", new SolicitudRendicion(titulo))).StatusCode);
    }

    [Fact]
    public async Task Titulo_demasiado_largo_da_400()
    {
        var emp = await Empleado();
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.Post($"{Ruta}/PostAgregar", new SolicitudRendicion(new string('a', 101)))).StatusCode);
    }

    // ---------- Gastos: casos límite ----------

    [Fact]
    public async Task Gastos_con_datos_invalidos_dan_400()
    {
        var emp = await Empleado();
        var cat = await emp.CodigoCategoria("Transporte");
        var codigo = await emp.CrearRendicion("Validaciones");
        var url = $"{Ruta}/PostAgregarGasto/{codigo}";

        var casos = new[]
        {
            new SolicitudGasto(cat, Datos.Fecha(-1), "Fecha futura", 10),
            new SolicitudGasto(cat, Datos.Fecha(120), "Fecha muy antigua", 10),
            new SolicitudGasto(cat, "2026-10-10", "Formato incorrecto", 10),
            new SolicitudGasto(cat, "31/02/2026", "Fecha imposible", 10),
            new SolicitudGasto(cat, "", "Sin fecha", 10),
            new SolicitudGasto(cat, Datos.Fecha(1), "Monto cero", 0),
            new SolicitudGasto(cat, Datos.Fecha(1), "Monto negativo", -3),
            new SolicitudGasto(cat, Datos.Fecha(1), "Tres decimales", 10.123m),
            new SolicitudGasto(cat, Datos.Fecha(1), "Sobre el límite", 5000),
            new SolicitudGasto(cat, Datos.Fecha(1), "", 10),
            new SolicitudGasto(cat, Datos.Fecha(1), "ab", 10),
            new SolicitudGasto(cat, Datos.Fecha(1), new string('x', 201), 10),
            new SolicitudGasto(0, Datos.Fecha(1), "Sin categoría", 10),
            new SolicitudGasto(99999, Datos.Fecha(1), "Categoría inexistente", 10),
        };

        foreach (var caso in casos)
            Assert.True((await emp.Post(url, caso)).StatusCode == HttpStatusCode.BadRequest, $"Debió rechazar: {caso.Descripcion}");

        Assert.Equal(0m, (await emp.Detalle(codigo)).Total);
    }

    [Fact]
    public async Task Gasto_duplicado_se_rechaza()
    {
        var emp = await Empleado();
        var cat = await emp.CodigoCategoria("Transporte");
        var codigo = await emp.CrearRendicion("Duplicados");
        var gasto = new SolicitudGasto(cat, Datos.Fecha(1), "Taxi centro", 12);
        Assert.Equal(HttpStatusCode.OK, (await emp.Post($"{Ruta}/PostAgregarGasto/{codigo}", gasto)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.Post($"{Ruta}/PostAgregarGasto/{codigo}", gasto)).StatusCode);
    }

    [Fact]
    public async Task No_se_agregan_gastos_a_una_rendicion_enviada()
    {
        var emp = await Empleado();
        var cat = await emp.CodigoCategoria("Transporte");
        var codigo = await emp.CrearRendicion("Cerrada");
        await emp.CrearGasto(codigo, cat, 10);
        await emp.Post($"{Ruta}/PostEnviar/{codigo}");
        var r = await emp.Post($"{Ruta}/PostAgregarGasto/{codigo}", new SolicitudGasto(cat, Datos.Fecha(1), "Tarde", 5));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ---------- Comprobantes ----------

    [Theory]
    [InlineData("hoja.xlsx")]
    [InlineData("doc.docx")]
    [InlineData("programa.exe")]
    [InlineData("sinextension")]
    [InlineData("texto.txt")]
    public async Task Comprobante_con_formato_no_permitido_se_rechaza_con_mensaje_claro(string nombre)
    {
        var emp = await Empleado();
        var gasto = await NuevoGasto(emp);
        var r = await emp.SubirComprobante(gasto, nombre, Datos.Pdf);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("PDF", await emp.Mensaje(r));
    }

    [Fact]
    public async Task Comprobante_con_extension_falsa_o_vacio_se_rechaza()
    {
        var emp = await Empleado();
        var gasto = await NuevoGasto(emp);
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.SubirComprobante(gasto, "falso.pdf", "esto es texto"u8.ToArray())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.SubirComprobante(gasto, "imagen.png", Datos.Pdf)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.SubirComprobante(gasto, "vacio.pdf", [])).StatusCode);
    }

    [Fact]
    public async Task Comprobante_demasiado_grande_se_rechaza()
    {
        var emp = await Empleado();
        var gasto = await NuevoGasto(emp);
        var grande = new byte[6 * 1024 * 1024];
        Datos.Pdf.CopyTo(grande, 0);
        Assert.Equal(HttpStatusCode.BadRequest, (await emp.SubirComprobante(gasto, "grande.pdf", grande)).StatusCode);
    }

    [Fact]
    public async Task Comprobante_se_sube_descarga_reemplaza_y_quita()
    {
        var emp = await Empleado();
        var gasto = await NuevoGasto(emp);

        Assert.Equal(HttpStatusCode.NoContent, (await emp.SubirComprobante(gasto, "uno.png", Datos.Png)).StatusCode);
        var descarga = await emp.Get($"{Ruta}/GetComprobante/{gasto}");
        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("image/png", descarga.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Datos.Png, await descarga.Content.ReadAsByteArrayAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await emp.SubirComprobante(gasto, "dos.pdf", Datos.Pdf)).StatusCode);
        Assert.Equal("application/pdf", (await emp.Get($"{Ruta}/GetComprobante/{gasto}")).Content.Headers.ContentType!.MediaType);

        Assert.Equal(HttpStatusCode.NoContent, (await emp.Delete($"{Ruta}/DeleteQuitarComprobante/{gasto}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await emp.Get($"{Ruta}/GetComprobante/{gasto}")).StatusCode);
    }

    [Fact]
    public async Task Aprobador_asignado_puede_ver_el_comprobante_de_una_rendicion_enviada()
    {
        var emp = await Empleado();
        var apr = await Aprobador();
        var cat = await emp.CodigoCategoria("Alimentación");
        var codigo = await emp.CrearRendicion("Con comprobante");
        var gasto = await emp.CrearGasto(codigo, cat, 30);
        await emp.SubirComprobante(gasto, "boleta.pdf", Datos.Pdf);
        await emp.Post($"{Ruta}/PostEnviar/{codigo}");

        Assert.Equal(HttpStatusCode.OK, (await apr.Get($"{Ruta}/GetComprobante/{gasto}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await apr.Get($"{Ruta}/Get/{codigo}")).StatusCode);
    }

    // ---------- Avisos y push ----------

    [Fact]
    public async Task Push_sin_claves_se_informa_como_deshabilitado_y_la_suscripcion_se_valida()
    {
        var emp = await Empleado();
        var clave = await emp.Leer<ClavePushDto>("Avisos/GetClavePush");
        Assert.False(clave.Habilitado);
        Assert.Equal(string.Empty, clave.ClavePublica);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await emp.Post("Avisos/PostSuscribirPush", new SolicitudSuscripcionPush("http://inseguro.com/x", "p256dhp256dh", "authauthauth"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await emp.Post("Avisos/PostSuscribirPush", new SolicitudSuscripcionPush("https://push.example.com/abc", "", "authauthauth"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await emp.Post("Avisos/PostSuscribirPush", new SolicitudSuscripcionPush("https://push.example.com/abc", "p256dhp256dh", "authauthauth"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await emp.Post("Avisos/PostDesuscribirPush", new SolicitudBajaPush("https://push.example.com/abc"))).StatusCode);
    }

    [Fact]
    public async Task Salud_responde_sin_sesion()
    {
        Assert.Equal(HttpStatusCode.OK, (await Cliente.SinSesion(fabrica).Get("Salud/Get")).StatusCode);
    }

    private static async Task<int> NuevoGasto(Cliente emp)
    {
        var cat = await emp.CodigoCategoria("Transporte");
        var codigo = await emp.CrearRendicion("Con archivos");
        return await emp.CrearGasto(codigo, cat, 10);
    }
}
