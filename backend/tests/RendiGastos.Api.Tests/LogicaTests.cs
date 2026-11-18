using Microsoft.Extensions.Options;
using RendiGastos.Api.Configuracion;
using RendiGastos.Api.Datos;
using RendiGastos.Api.Modelos;
using RendiGastos.Api.Servicios;

namespace RendiGastos.Api.Tests;

// Pruebas de la lógica sin base de datos ni red (repositorios falsos).
// Cubren cada regla y cada valor límite, para que Stryker no deje mutantes vivos.

internal sealed class SeguridadFalsa : ISeguridad
{
    public List<(int Usuario, int[] Roles)> Llamadas { get; } = [];
    public FilaUsuario Actor { get; set; } = new() { CodUsrio = 1, NomUsrio = "Ana Actor", CodRol = Roles.Administrador };

    public Task<FilaUsuario> ExigirAsync(int codUsuario, params int[] rolesPermitidos)
    {
        Llamadas.Add((codUsuario, rolesPermitidos));
        return Task.FromResult(Actor);
    }

    public void Era(params int[] roles)
    {
        var l = Assert.Single(Llamadas);
        Assert.Equal(roles, l.Roles);
    }
}

internal sealed class UsuariosFalsos : IUsuarioRepositorio
{
    public FilaUsuario? Existente { get; set; } = new() { CodUsrio = 5, NomUsrio = "Ana", NomEmail = "ana@correo.com", NomClaveHsh = "H:Clave1234", CodRol = Roles.Aprobador, NomRol = "Aprobador", FlgActvo = "S" };
    public string? EmailBuscado { get; private set; }
    public bool? SoloAprobadores { get; private set; }
    public object[]? Agregado { get; private set; }
    public object[]? Editado { get; private set; }
    public List<int> Ingresos { get; } = [];

    public Task<int> AgregarAsync(string nombre, string email, string claveHash, int codRol, int? codAprobador, int codUsuarioAccion)
    {
        Agregado = [nombre, email, claveHash, codRol, codAprobador!, codUsuarioAccion];
        return Task.FromResult(5);
    }

    public Task<FilaUsuario?> ObtenerPorEmailAsync(string email) { EmailBuscado = email; return Task.FromResult(Existente); }
    public Task<FilaUsuario?> ObtenerPorCodigoAsync(int codUsuario) => Task.FromResult(Existente);

    public Task<IReadOnlyList<FilaUsuario>> ListarAsync(bool soloAprobadores)
    {
        SoloAprobadores = soloAprobadores;
        return Task.FromResult<IReadOnlyList<FilaUsuario>>(Existente is null ? [] : [Existente]);
    }

    public Task EditarAsync(int codUsuario, string nombre, int codRol, int? codAprobador, bool activo, int codUsuarioAccion)
    {
        Editado = [codUsuario, nombre, codRol, codAprobador!, activo, codUsuarioAccion];
        return Task.CompletedTask;
    }

    public Task RegistrarIngresoAsync(int codUsuario) { Ingresos.Add(codUsuario); return Task.CompletedTask; }
}

internal sealed class ClavesFalsas : IClaveService
{
    public string Generar(string clave) => "H:" + clave;
    public bool Verificar(string clave, string? hashGuardado) => hashGuardado == "H:" + clave;
}

internal sealed class TokensFalsos : ITokenService
{
    public string Crear(FilaUsuario usuario) => $"tok|{usuario.CodUsrio}";
}

public sealed class ValidacionesTests
{
    private static string Mensaje(Action a) => Assert.Throws<ErrorNegocio>(a).Message;

    [Fact]
    public void Texto_distingue_vacio_corto_y_largo_con_su_propio_mensaje()
    {
        Assert.Equal("El campo es obligatorio.", Mensaje(() => Validaciones.Texto("   ", "El campo", 3, 5)));
        Assert.Equal("El campo es obligatorio.", Mensaje(() => Validaciones.Texto(null, "El campo", 3, 5)));
        Assert.Equal("El campo debe tener al menos 3 caracteres.", Mensaje(() => Validaciones.Texto("ab", "El campo", 3, 5)));
        Assert.Equal("El campo no puede superar 5 caracteres.", Mensaje(() => Validaciones.Texto("abcdef", "El campo", 3, 5)));
    }

    [Fact]
    public void Texto_acepta_los_bordes_y_devuelve_el_valor_sin_espacios()
    {
        Assert.Equal("abc", Validaciones.Texto("  abc ", "El campo", 3, 5));
        Assert.Equal("abcde", Validaciones.Texto("abcde", "El campo", 3, 5));
    }

    [Fact]
    public void Texto_opcional_acepta_vacio_y_nulo_y_respeta_el_maximo()
    {
        Assert.Equal("", Validaciones.TextoOpcional(null, "La nota", 5));
        Assert.Equal("abcde", Validaciones.TextoOpcional(" abcde ", "La nota", 5));
        Assert.Equal("La nota no puede superar 5 caracteres.", Mensaje(() => Validaciones.TextoOpcional("abcdef", "La nota", 5)));
    }

    [Theory]
    [InlineData("")] [InlineData("   ")]
    public void Un_correo_vacio_se_pide(string email) =>
        Assert.Equal("El correo es obligatorio.", Mensaje(() => Validaciones.Email(email)));

    [Fact]
    public void El_correo_admite_150_caracteres_y_no_151()
    {
        var de150 = new string('a', 150 - "@b.com".Length) + "@b.com";
        Assert.Equal(de150, Validaciones.Email(de150));
        var de151 = new string('a', 151 - "@b.com".Length) + "@b.com";
        Assert.Equal("El correo no puede superar 150 caracteres.", Mensaje(() => Validaciones.Email(de151)));
    }

    [Theory]
    [InlineData("sin-arroba")] [InlineData("a@b")] [InlineData("a b@correo.com")]
    [InlineData("Ana <ana@correo.com>")] [InlineData("a.b@c")]
    public void Un_correo_con_formato_invalido_se_rechaza_con_ejemplo(string email) =>
        Assert.Equal("El correo no tiene un formato válido (ejemplo: nombre@empresa.com).", Mensaje(() => Validaciones.Email(email)));

    [Fact]
    public void El_correo_valido_se_normaliza()
    {
        Assert.Equal("ana@correo.com", Validaciones.Email("  ANA@Correo.com "));
    }

    [Theory]
    [InlineData(7, false)] [InlineData(8, true)] [InlineData(100, true)] [InlineData(101, false)]
    public void La_clave_va_de_8_a_100_caracteres(int largo, bool valida)
    {
        var clave = "a1" + new string('x', largo - 2);
        if (valida) Assert.Equal(clave, Validaciones.Clave(clave));
        else Assert.Equal(largo < 8 ? "La clave debe tener al menos 8 caracteres." : "La clave no puede superar 100 caracteres.", Mensaje(() => Validaciones.Clave(clave)));
    }

    [Theory]
    [InlineData("sololetrasaqui")] [InlineData("123456789")]
    public void La_clave_exige_letras_y_numeros(string clave) =>
        Assert.Equal("La clave debe incluir letras y números.", Mensaje(() => Validaciones.Clave(clave)));

    [Fact]
    public void Una_clave_nula_se_trata_como_corta() =>
        Assert.Equal("La clave debe tener al menos 8 caracteres.", Mensaje(() => Validaciones.Clave(null)));

    [Theory]
    [InlineData(1, "Empleado")] [InlineData(2, "Aprobador")] [InlineData(3, "Administrador")] [InlineData(99, "Empleado")]
    public void El_nombre_del_rol_sale_de_su_codigo(int cod, string nombre) => Assert.Equal(nombre, Roles.Nombre(cod));
}

public sealed class SeguridadTests
{
    private readonly UsuariosFalsos _repo = new();
    private Task<FilaUsuario> Exigir(params int[] roles) => new Seguridad(_repo).ExigirAsync(5, roles);

    [Fact]
    public async Task Una_sesion_de_una_cuenta_que_ya_no_existe_es_401()
    {
        _repo.Existente = null;
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Exigir());
        Assert.Equal((401, "Tu sesión ya no es válida. Ingresa de nuevo."), (e.Estado, e.Message));
    }

    [Fact]
    public async Task Una_cuenta_desactivada_es_403()
    {
        _repo.Existente!.FlgActvo = "N";
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Exigir());
        Assert.Equal((403, "Tu cuenta está desactivada. Contacta al administrador."), (e.Estado, e.Message));
    }

    [Fact]
    public async Task Un_rol_no_permitido_es_403_y_uno_permitido_pasa()
    {
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Exigir(Roles.Administrador));
        Assert.Equal((403, "No tienes permiso para realizar esta acción."), (e.Estado, e.Message));

        Assert.Equal(5, (await Exigir(Roles.Administrador, Roles.Aprobador)).CodUsrio);
        Assert.Equal(5, (await Exigir()).CodUsrio);
    }
}

public sealed class AutenticacionServiceTests
{
    private readonly UsuariosFalsos _repo = new();
    private AutenticacionService Servicio => new(_repo, new ClavesFalsas(), new TokensFalsos(), Options.Create(new OpcionesJwt { MinutosVigencia = 77 }));

    [Theory]
    [InlineData("", "Clave1234")] [InlineData("   ", "Clave1234")] [InlineData("ana@correo.com", "")] [InlineData(null, null)]
    public async Task El_ingreso_exige_correo_y_clave(string? email, string? clave)
    {
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.IngresarAsync(new(email, clave)));
        Assert.Equal("Escribe tu correo y tu clave.", e.Message);
    }

    [Fact]
    public async Task Un_ingreso_correcto_registra_el_ingreso_y_arma_la_sesion()
    {
        var r = await Servicio.IngresarAsync(new(" ANA@Correo.com ", "Clave1234"));
        Assert.Equal("ana@correo.com", _repo.EmailBuscado);
        Assert.Equal([5], _repo.Ingresos);
        Assert.Equal(new RespuestaSesion("tok|5", 5, "Ana", "Aprobador", 2, 77), r);
    }

    [Fact]
    public async Task Clave_equivocada_y_correo_inexistente_dan_el_mismo_401()
    {
        var e1 = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.IngresarAsync(new("ana@correo.com", "otra")));
        _repo.Existente = null;
        var e2 = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.IngresarAsync(new("nadie@correo.com", "Clave1234")));
        foreach (var e in new[] { e1, e2 })
            Assert.Equal((401, "Correo o clave incorrectos."), (e.Estado, e.Message));
        Assert.Empty(_repo.Ingresos);
    }

    [Fact]
    public async Task Una_cuenta_desactivada_con_la_clave_correcta_es_403_y_no_registra_ingreso()
    {
        _repo.Existente!.FlgActvo = "N";
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.IngresarAsync(new("ana@correo.com", "Clave1234")));
        Assert.Equal((403, "Tu cuenta está desactivada. Contacta al administrador."), (e.Estado, e.Message));
        Assert.Empty(_repo.Ingresos);
    }

    [Fact]
    public async Task El_registro_crea_siempre_un_empleado_sin_aprobador_y_guarda_el_hash()
    {
        _repo.Existente = new FilaUsuario { CodUsrio = 5, NomUsrio = "Ana Ruiz", CodRol = Roles.Empleado };
        var r = await Servicio.RegistrarAsync(new("  Ana Ruiz ", " ANA@correo.com", "Clave1234"));
        Assert.Equal(["Ana Ruiz", "ana@correo.com", "H:Clave1234", Roles.Empleado, null!, 0], _repo.Agregado);
        Assert.Equal(new RespuestaSesion("tok|5", 5, "Ana Ruiz", "Empleado", 1, 77), r);
    }

    [Fact]
    public async Task El_registro_valida_nombre_correo_y_clave_antes_de_guardar()
    {
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.RegistrarAsync(new("ab", "ana@correo.com", "Clave1234")));
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.RegistrarAsync(new("Ana Ruiz", "malo", "Clave1234")));
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.RegistrarAsync(new("Ana Ruiz", "ana@correo.com", "corta")));
        Assert.Null(_repo.Agregado);
    }

    [Fact]
    public async Task Si_la_cuenta_recien_creada_no_aparece_se_avisa()
    {
        _repo.Existente = null;
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.RegistrarAsync(new("Ana Ruiz", "ana@correo.com", "Clave1234")));
        Assert.Equal("No se pudo crear la cuenta.", e.Message);
    }
}

public sealed class UsuarioServiceTests
{
    private readonly SeguridadFalsa _seg = new();
    private readonly UsuariosFalsos _repo = new();
    private UsuarioService Servicio => new(_repo, _seg, new ClavesFalsas());

    [Fact]
    public async Task Listar_pide_administrador_y_traduce_la_cuenta()
    {
        _repo.Existente!.CodAprbdr = 9;
        _repo.Existente.NomAprbdr = "Beto";
        var u = Assert.Single(await Servicio.ListarAsync(1));
        _seg.Era(Roles.Administrador);
        Assert.False(_repo.SoloAprobadores);
        Assert.Equal(new UsuarioDto(5, "Ana", "ana@correo.com", 2, "Aprobador", 9, "Beto", true), u);
    }

    [Fact]
    public async Task Una_cuenta_con_bandera_N_se_muestra_inactiva()
    {
        _repo.Existente!.FlgActvo = "N";
        Assert.False((await Servicio.ListarAsync(1)).Single().Activo);
    }

    [Fact]
    public async Task Listar_aprobadores_pide_solo_aprobadores()
    {
        await Servicio.ListarAprobadoresAsync(1);
        _seg.Era(Roles.Administrador);
        Assert.True(_repo.SoloAprobadores);
    }

    [Fact]
    public async Task Crear_valida_guarda_con_hash_y_devuelve_la_cuenta()
    {
        var u = await Servicio.CrearAsync(8, new("  Ana Ruiz ", "ANA@correo.com", "Clave1234", Roles.Aprobador, 3));
        _seg.Era(Roles.Administrador);
        Assert.Equal(["Ana Ruiz", "ana@correo.com", "H:Clave1234", Roles.Aprobador, 3, 8], _repo.Agregado);
        Assert.Equal(5, u.CodUsuario);
    }

    [Theory]
    [InlineData("ab", "ana@correo.com", "Clave1234")]
    [InlineData("Ana Ruiz", "malo", "Clave1234")]
    [InlineData("Ana Ruiz", "ana@correo.com", "corta")]
    public async Task Crear_con_datos_invalidos_no_guarda_nada(string nombre, string email, string clave)
    {
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.CrearAsync(8, new(nombre, email, clave, Roles.Empleado, null)));
        Assert.Null(_repo.Agregado);
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, true)] [InlineData(2, true)] [InlineData(3, true)] [InlineData(4, false)]
    public async Task El_rol_debe_ser_Empleado_Aprobador_o_Administrador(int rol, bool valido)
    {
        if (valido)
        {
            await Servicio.CrearAsync(8, new("Ana Ruiz", "ana@correo.com", "Clave1234", rol, null));
            await Servicio.EditarAsync(8, 5, new("Ana Ruiz", rol, null, true));
            Assert.NotNull(_repo.Agregado);
            Assert.NotNull(_repo.Editado);
        }
        else
        {
            var e1 = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.CrearAsync(8, new("Ana Ruiz", "ana@correo.com", "Clave1234", rol, null)));
            var e2 = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.EditarAsync(8, 5, new("Ana Ruiz", rol, null, true)));
            Assert.Equal("El rol no es válido. Elige Empleado, Aprobador o Administrador.", e1.Message);
            Assert.Equal(e1.Message, e2.Message);
            Assert.Null(_repo.Agregado);
            Assert.Null(_repo.Editado);
        }
    }

    [Fact]
    public async Task Editar_pasa_todos_los_datos_y_pide_administrador()
    {
        await Servicio.EditarAsync(8, 5, new("  Nuevo Nombre ", Roles.Aprobador, 4, false));
        _seg.Era(Roles.Administrador);
        Assert.Equal([5, "Nuevo Nombre", Roles.Aprobador, 4, false, 8], _repo.Editado);
    }

    [Fact]
    public async Task Editar_valida_el_nombre()
    {
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.EditarAsync(8, 5, new("ab", Roles.Empleado, null, true)));
        Assert.Contains("El nombre", e.Message);
        Assert.Null(_repo.Editado);
    }
}

internal sealed class CategoriasFalsas : ICategoriaRepositorio
{
    public bool? SoloActivasPedido { get; private set; }
    public object[]? Agregada { get; private set; }
    public object[]? Editada { get; private set; }

    public Task<IReadOnlyList<FilaCategoria>> ListarAsync(bool soloActivas)
    {
        SoloActivasPedido = soloActivas;
        return Task.FromResult<IReadOnlyList<FilaCategoria>>([new() { CodCtgra = 1, NomCtgra = "Viaje", DscCtgra = "d", MntLmte = 100, FlgCmprbOblgt = "S", FlgActvo = "N" }]);
    }

    public Task<int> AgregarAsync(string nombre, string descripcion, decimal limite, bool comprobante, int codUsuarioAccion)
    {
        Agregada = [nombre, descripcion, limite, comprobante, codUsuarioAccion];
        return Task.FromResult(1);
    }

    public Task EditarAsync(int codCategoria, string nombre, string descripcion, decimal limite, bool comprobante, bool activa, int codUsuarioAccion)
    {
        Editada = [codCategoria, nombre, descripcion, limite, comprobante, activa, codUsuarioAccion];
        return Task.CompletedTask;
    }
}

public sealed class CategoriaServiceTests
{
    private readonly SeguridadFalsa _seg = new();
    private readonly CategoriasFalsas _repo = new();
    private CategoriaService Servicio => new(_repo, _seg);

    [Theory]
    [InlineData(Roles.Administrador, false, false)]
    [InlineData(Roles.Administrador, true, true)]
    [InlineData(Roles.Aprobador, false, true)]
    [InlineData(Roles.Empleado, false, true)]
    public async Task Solo_el_administrador_ve_las_categorias_inactivas_y_solo_si_las_pide(int rol, bool soloActivas, bool esperado)
    {
        _seg.Actor.CodRol = rol;
        var c = Assert.Single(await Servicio.ListarAsync(1, soloActivas));
        Assert.Equal(esperado, _repo.SoloActivasPedido);
        Assert.Empty(_seg.Llamadas.Single().Roles);
        Assert.Equal(new CategoriaDto(1, "Viaje", "d", 100, true, false), c);
    }

    [Fact]
    public async Task Crear_pide_administrador_valida_guarda_y_devuelve_todas_incluidas_las_inactivas()
    {
        var r = await Servicio.CrearAsync(7, new("  Viajes ", null, 250.5m, true));
        _seg.Era(Roles.Administrador);
        Assert.Equal(["Viajes", "", 250.5m, true, 7], _repo.Agregada);
        Assert.False(_repo.SoloActivasPedido);
        Assert.Single(r);
    }

    [Fact]
    public async Task Editar_guarda_con_el_estado_activa_y_devuelve_todas()
    {
        await Servicio.EditarAsync(7, 3, new("Viajes", "Pasajes", 100, false, Activa: false));
        _seg.Era(Roles.Administrador);
        Assert.Equal([3, "Viajes", "Pasajes", 100m, false, false, 7], _repo.Editada);
        Assert.False(_repo.SoloActivasPedido);
    }

    [Theory]
    [InlineData("ab", "", 10, "nombre")]
    [InlineData("Viajes", "", 0, "límite")]
    [InlineData("Viajes", "", -1, "límite")]
    [InlineData("Viajes", "", 1_000_000.01, "límite")]
    public async Task Los_datos_invalidos_de_una_categoria_se_rechazan(string nombre, string descripcion, double limite, string campo)
    {
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.CrearAsync(7, new(nombre, descripcion, (decimal)limite, false)));
        Assert.Contains(campo, e.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(_repo.Agregada);
    }

    [Theory]
    [InlineData(0.01)] [InlineData(1_000_000)]
    public async Task El_limite_acepta_desde_un_centimo_hasta_un_millon(double limite)
    {
        await Servicio.CrearAsync(7, new("Viajes", "", (decimal)limite, false));
        Assert.NotNull(_repo.Agregada);
    }

    [Fact]
    public async Task El_nombre_va_de_3_a_60_y_la_descripcion_hasta_200()
    {
        await Servicio.CrearAsync(7, new(new string('n', 60), new string('d', 200), 10, false));
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.CrearAsync(7, new(new string('n', 61), "", 10, false)));
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.CrearAsync(7, new("Viajes", new string('d', 201), 10, false)));
    }
}

internal sealed class NotificadorFalso : INotificador
{
    public List<(int Usuario, string Titulo, string Mensaje)> Avisos { get; } = [];
    public Task AvisarAsync(int codUsuario, string titulo, string mensaje) { Avisos.Add((codUsuario, titulo, mensaje)); return Task.CompletedTask; }
}

internal sealed class RendicionesFalsas : IRendicionRepositorio
{
    public List<string> Llamadas { get; } = [];
    public FilaRendicionCabecera? Cabecera { get; set; } = new() { CodRndcn = 4, NomTtlo = "Viaje", CodUsrio = 1, NomUsrio = "Ana", NomAprbdr = "Beto", CodEstdoRndcn = 2, NomEstdoRndcn = "Enviada", MntTtal = 90, DscMtvoRchzo = "m", FecCrcn = "f1", FecEnvio = "f2", FecRsln = "f3", NomRsltr = "Rita" };
    public FilaComprobante? Comprobante { get; set; } = new() { NomArchv = "a.pdf", TpoCntnd = "application/pdf", BinCntnd = [1] };
    public (string Nombre, string Tipo, int Bytes)? Subido { get; private set; }
    public string? ModoPedido { get; private set; }

    private Task<T> Reg<T>(string llamada, T valor) { Llamadas.Add(llamada); return Task.FromResult(valor); }
    private Task Reg(string llamada) { Llamadas.Add(llamada); return Task.CompletedTask; }

    public Task<int> AgregarAsync(int codUsuario, string titulo) => Reg($"Agregar({codUsuario},{titulo})", 4);
    public Task EditarAsync(int codRendicion, int codUsuario, string titulo) => Reg($"Editar({codRendicion},{codUsuario},{titulo})");
    public Task EliminarAsync(int codRendicion, int codUsuario) => Reg($"Eliminar({codRendicion},{codUsuario})");

    public Task<IReadOnlyList<FilaRendicionResumen>> ListarAsync(int codUsuario, string modo, int? codEstado)
    {
        ModoPedido = modo;
        Llamadas.Add($"Listar({codUsuario},{modo},{codEstado})");
        return Task.FromResult<IReadOnlyList<FilaRendicionResumen>>([new() { CodRndcn = 4, NomTtlo = "Viaje", CodUsrio = 1, NomUsrio = "Ana", CodEstdoRndcn = 2, NomEstdoRndcn = "Enviada", MntTtal = 90, NumGastos = 3, FecCrcn = "f1", FecEnvio = "f2" }]);
    }

    public Task<(FilaRendicionCabecera? Cabecera, IReadOnlyList<FilaGasto> Gastos, IReadOnlyList<FilaHistorial> Historial)> ObtenerAsync(int codRendicion, int codUsuarioConsulta)
    {
        IReadOnlyList<FilaGasto> gastos = [new() { CodGasto = 1, CodCtgra = 2, NomCtgra = "Taxi", FlgCmprbOblgt = "S", FecGasto = "01/01/2026", DscGasto = "carrera", MntGasto = 15, NomArchv = "", TpoCntnd = "", NumTamno = 0 },
                                           new() { CodGasto = 2, CodCtgra = 2, NomCtgra = "Taxi", FlgCmprbOblgt = "N", NomArchv = "b.png", TpoCntnd = "image/png", NumTamno = 9 }];
        IReadOnlyList<FilaHistorial> historial = [new() { CodHstrl = 1, CodAccnHstrl = 2, NomAccnHstrl = "Envió", CodUsrio = 1, NomUsrio = "Ana", DscCmntr = "c", FecHstrl = "F", HorHstrl = "H" }];
        return Task.FromResult((Cabecera, gastos, historial));
    }

    public Task<int> AgregarGastoAsync(int codRendicion, int codUsuario, int codCategoria, string fecha, string descripcion, decimal monto) =>
        Reg($"AgregarGasto({codRendicion},{codUsuario},{codCategoria},{fecha},{descripcion},{monto})", 8);
    public Task EditarGastoAsync(int codGasto, int codUsuario, int codCategoria, string fecha, string descripcion, decimal monto) =>
        Reg($"EditarGasto({codGasto},{codUsuario},{codCategoria},{fecha},{descripcion},{monto})");
    public Task EliminarGastoAsync(int codGasto, int codUsuario) => Reg($"EliminarGasto({codGasto},{codUsuario})");

    public Task GuardarComprobanteAsync(int codGasto, int codUsuario, string nombre, string tipo, byte[] contenido)
    {
        Subido = (nombre, tipo, contenido.Length);
        return Reg($"GuardarComprobante({codGasto},{codUsuario})");
    }

    public Task QuitarComprobanteAsync(int codGasto, int codUsuario) => Reg($"QuitarComprobante({codGasto},{codUsuario})");
    public Task<FilaComprobante?> ObtenerComprobanteAsync(int codGasto, int codUsuarioConsulta) => Reg($"ObtenerComprobante({codGasto},{codUsuarioConsulta})", Comprobante);
    public Task<int> EnviarAsync(int codRendicion, int codUsuario) => Reg($"Enviar({codRendicion},{codUsuario})", 20);
    public Task<int> AprobarAsync(int codRendicion, int codAprobador, string? comentario) => Reg($"Aprobar({codRendicion},{codAprobador},{comentario ?? "<null>"})", 30);
    public Task<int> RechazarAsync(int codRendicion, int codAprobador, string motivo) => Reg($"Rechazar({codRendicion},{codAprobador},{motivo})", 30);
    public Task ReabrirAsync(int codRendicion, int codUsuario) => Reg($"Reabrir({codRendicion},{codUsuario})");
}

public sealed class RendicionServiceTests
{
    private readonly SeguridadFalsa _seg = new();
    private readonly RendicionesFalsas _repo = new();
    private readonly NotificadorFalso _not = new();
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 1];

    private RendicionService Servicio => new(_repo, _seg, _not, Options.Create(new OpcionesArchivos { TamanoMaximoMb = 5 }));

    private static SolicitudGasto Gasto(int cat = 2, string? fecha = "25/10/2026", string? desc = "Taxi al aeropuerto", decimal monto = 45.5m) => new(cat, fecha, desc, monto);

    [Fact]
    public async Task Cada_operacion_pide_a_la_base_el_rol_que_le_corresponde()
    {
        await Servicio.CrearAsync(1, new("Viaje a Lima"));
        await Servicio.EditarAsync(1, 4, new("Viaje a Lima"));
        await Servicio.EliminarAsync(1, 4);
        await Servicio.ListarPropiasAsync(1, null);
        await Servicio.ListarPorAprobarAsync(1, null);
        await Servicio.ObtenerAsync(1, 4);
        await Servicio.AgregarGastoAsync(1, 4, Gasto());
        await Servicio.EditarGastoAsync(1, 8, Gasto());
        await Servicio.EliminarGastoAsync(1, 8);
        await Servicio.SubirComprobanteAsync(1, 8, "a.pdf", Pdf);
        await Servicio.QuitarComprobanteAsync(1, 8);
        await Servicio.DescargarComprobanteAsync(1, 8);
        await Servicio.EnviarAsync(1, 4);
        await Servicio.AprobarAsync(1, 4, null);
        await Servicio.RechazarAsync(1, 4, "Falta el comprobante");
        await Servicio.ReabrirAsync(1, 4);

        Assert.Equal(16, _seg.Llamadas.Count);
        var aprobadores = new[] { Roles.Aprobador, Roles.Administrador };
        for (var i = 0; i < 16; i++)
            Assert.Equal(i is 4 or 13 or 14 ? aprobadores : [], _seg.Llamadas[i].Roles);
    }

    [Fact]
    public async Task Crear_y_editar_validan_el_titulo_y_lo_guardan_limpio()
    {
        Assert.Equal(4, await Servicio.CrearAsync(1, new("  Viaje a Lima ")));
        await Servicio.EditarAsync(1, 4, new(" Otro título "));
        Assert.Equal(["Agregar(1,Viaje a Lima)", "Editar(4,1,Otro título)"], _repo.Llamadas);

        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.CrearAsync(1, new("ab")));
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.EditarAsync(1, 4, new(new string('t', 101))));
        Assert.Equal(2, _repo.Llamadas.Count);
    }

    [Fact]
    public async Task Eliminar_y_reabrir_llegan_a_la_base_con_actor_y_rendicion()
    {
        await Servicio.EliminarAsync(1, 4);
        await Servicio.ReabrirAsync(1, 4);
        Assert.Equal(["Eliminar(4,1)", "Reabrir(4,1)"], _repo.Llamadas);
    }

    [Fact]
    public async Task Las_listas_distinguen_propias_P_y_por_aprobar_A_y_traducen_el_resumen()
    {
        var p = Assert.Single(await Servicio.ListarPropiasAsync(1, 2));
        Assert.Equal("P", _repo.ModoPedido);
        Assert.Equal(new RendicionResumenDto(4, "Viaje", 1, "Ana", 2, "Enviada", 90, 3, "f1", "f2"), p);
        await Servicio.ListarPorAprobarAsync(1, null);
        Assert.Equal("A", _repo.ModoPedido);
        Assert.Equal(["Listar(1,P,2)", "Listar(1,A,)"], _repo.Llamadas);
    }

    [Fact]
    public async Task Obtener_una_rendicion_ajena_o_inexistente_es_404()
    {
        _repo.Cabecera = null;
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.ObtenerAsync(1, 4));
        Assert.Equal((404, "La rendición no existe o no tienes acceso a ella."), (e.Estado, e.Message));
    }

    [Fact]
    public async Task El_detalle_traduce_cabecera_gastos_e_historial()
    {
        var d = await Servicio.ObtenerAsync(1, 4);
        Assert.Equal((4, "Viaje", 1, "Ana", "Beto", 2, "Enviada", 90m, "m", "f1", "f2", "f3", "Rita"),
            (d.CodRendicion, d.Titulo, d.CodUsuario, d.Solicitante, d.Aprobador, d.CodEstado, d.Estado, d.Total, d.MotivoRechazo, d.FechaCreacion, d.FechaEnvio, d.FechaResolucion, d.Resolvio));
        Assert.Equal(new GastoDto(1, 2, "Taxi", true, "01/01/2026", "carrera", 15, null, null, 0), d.Gastos[0]);
        Assert.Equal(new GastoDto(2, 2, "Taxi", false, "", "", 0, "b.png", "image/png", 9), d.Gastos[1]);
        Assert.Equal(new HistorialDto(1, 2, "Envió", 1, "Ana", "c", "F", "H"), Assert.Single(d.Historial));
    }

    // ---------- Gastos ----------

    [Fact]
    public async Task Un_gasto_valido_llega_a_la_base_con_fecha_y_descripcion_limpias()
    {
        Assert.Equal(8, await Servicio.AgregarGastoAsync(1, 4, Gasto(fecha: " 25/10/2026 ", desc: "  Taxi al aeropuerto ")));
        await Servicio.EditarGastoAsync(1, 8, Gasto(monto: 1_000_000));
        Assert.Equal(["AgregarGasto(4,1,2,25/10/2026,Taxi al aeropuerto,45.5)", "EditarGasto(8,1,2,25/10/2026,Taxi al aeropuerto,1000000)"], _repo.Llamadas);
    }

    [Theory]
    [InlineData("ab", 2, "25/10/2026", 10, "descripción")]
    [InlineData("Taxi", 0, "25/10/2026", 10, "Elige una categoría.")]
    [InlineData("Taxi", -1, "25/10/2026", 10, "Elige una categoría.")]
    [InlineData("Taxi", 2, "25/10/2026", 0, "El monto debe ser mayor que 0.")]
    [InlineData("Taxi", 2, "25/10/2026", -5, "El monto debe ser mayor que 0.")]
    [InlineData("Taxi", 2, "25/10/2026", 1_000_000.01, "El monto no puede superar 1,000,000.")]
    [InlineData("Taxi", 2, "25/10/2026", 10.123, "El monto admite como máximo 2 decimales.")]
    [InlineData("Taxi", 2, "2026-10-25", 10, "La fecha debe tener el formato dd/mm/aaaa (ejemplo: 25/10/2026).")]
    [InlineData("Taxi", 2, "31/02/2026", 10, "La fecha debe tener el formato dd/mm/aaaa (ejemplo: 25/10/2026).")]
    [InlineData("Taxi", 2, "", 10, "La fecha debe tener el formato dd/mm/aaaa (ejemplo: 25/10/2026).")]
    [InlineData("Taxi", 2, null, 10, "La fecha debe tener el formato dd/mm/aaaa (ejemplo: 25/10/2026).")]
    public async Task Un_gasto_invalido_se_rechaza_con_su_mensaje_y_no_llega_a_la_base(string desc, int cat, string? fecha, double monto, string mensaje)
    {
        var e1 = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.AgregarGastoAsync(1, 4, Gasto(cat, fecha, desc, (decimal)monto)));
        var e2 = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.EditarGastoAsync(1, 8, Gasto(cat, fecha, desc, (decimal)monto)));
        Assert.Contains(mensaje, e1.Message);
        Assert.Equal(e1.Message, e2.Message);
        Assert.Empty(_repo.Llamadas);
    }

    [Theory]
    [InlineData(0.01)] [InlineData(1)] [InlineData(999999.99)]
    public async Task Los_montos_validos_incluyen_un_centimo_y_los_dos_decimales(double monto)
    {
        await Servicio.AgregarGastoAsync(1, 4, Gasto(monto: (decimal)monto));
        Assert.Single(_repo.Llamadas);
    }

    [Fact]
    public async Task Eliminar_un_gasto_llega_a_la_base()
    {
        await Servicio.EliminarGastoAsync(1, 8);
        Assert.Equal(["EliminarGasto(8,1)"], _repo.Llamadas);
    }

    // ---------- Comprobantes ----------

    [Fact]
    public async Task Un_comprobante_invalido_se_rechaza_con_el_mensaje_del_validador()
    {
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.SubirComprobanteAsync(1, 8, "a.xlsx", Pdf));
        Assert.StartsWith("Formato no permitido.", e.Message);
        Assert.Empty(_repo.Llamadas);
    }

    [Fact]
    public async Task Un_comprobante_se_guarda_con_nombre_limpio_y_tipo_real()
    {
        await Servicio.SubirComprobanteAsync(1, 8, "  ../fotos/recibo.PDF", Pdf);
        Assert.Equal(("recibo.PDF", "application/pdf", Pdf.Length), _repo.Subido);
    }

    [Fact]
    public async Task Un_nombre_de_mas_de_255_caracteres_se_recorta_conservando_el_final()
    {
        var largo = new string('a', 300) + "FIN.pdf";
        await Servicio.SubirComprobanteAsync(1, 8, largo, Pdf);
        Assert.Equal(255, _repo.Subido!.Value.Nombre.Length);
        Assert.EndsWith("FIN.pdf", _repo.Subido.Value.Nombre);

        var justo = new string('b', 255 - ".pdf".Length) + ".pdf";
        await Servicio.SubirComprobanteAsync(1, 8, justo, Pdf);
        Assert.Equal(justo, _repo.Subido.Value.Nombre);
    }

    [Fact]
    public async Task Quitar_y_descargar_comprobantes()
    {
        await Servicio.QuitarComprobanteAsync(1, 8);
        var c = await Servicio.DescargarComprobanteAsync(1, 8);
        Assert.Equal(new ComprobanteArchivo("a.pdf", "application/pdf", [1]).Nombre, c.Nombre);
        Assert.Equal(["QuitarComprobante(8,1)", "ObtenerComprobante(8,1)"], _repo.Llamadas);
    }

    [Fact]
    public async Task Un_comprobante_inexistente_o_vacio_es_404()
    {
        foreach (var f in new FilaComprobante?[] { null, new() { NomArchv = "a.pdf", TpoCntnd = "application/pdf", BinCntnd = [] } })
        {
            _repo.Comprobante = f;
            var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.DescargarComprobanteAsync(1, 8));
            Assert.Equal((404, "El comprobante no existe o no tienes acceso a él."), (e.Estado, e.Message));
        }
    }

    // ---------- Flujo de aprobación ----------

    [Fact]
    public async Task Enviar_avisa_al_aprobador_con_el_nombre_de_quien_envia()
    {
        await Servicio.EnviarAsync(1, 4);
        Assert.Equal([(20, "Rendición por aprobar", "Ana Actor envió una rendición para tu revisión.")], _not.Avisos);
    }

    [Theory]
    [InlineData(null, "<null>")] [InlineData("", "<null>")] [InlineData("   ", "<null>")] [InlineData("  Todo bien ", "Todo bien")]
    public async Task Aprobar_guarda_el_comentario_solo_si_lo_hay_y_avisa_al_dueno(string? comentario, string guardado)
    {
        await Servicio.AprobarAsync(9, 4, comentario);
        Assert.Equal([$"Aprobar(4,9,{guardado})"], _repo.Llamadas);
        Assert.Equal([(30, "Rendición aprobada", "Tu rendición fue aprobada.")], _not.Avisos);
    }

    [Fact]
    public async Task El_comentario_de_aprobacion_admite_500_caracteres_y_no_501()
    {
        await Servicio.AprobarAsync(9, 4, new string('c', 500));
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.AprobarAsync(9, 4, new string('c', 501)));
        Assert.Contains("El comentario", e.Message);
        Assert.Single(_repo.Llamadas);
    }

    [Fact]
    public async Task Rechazar_exige_un_motivo_de_10_a_500_caracteres_y_avisa_al_dueno()
    {
        await Servicio.RechazarAsync(9, 4, "  Falta el comprobante ");
        Assert.Equal(["Rechazar(4,9,Falta el comprobante)"], _repo.Llamadas);
        Assert.Equal([(30, "Rendición rechazada", "Tu rendición fue rechazada. Revisa el motivo.")], _not.Avisos);

        await Servicio.RechazarAsync(9, 4, new string('m', 10));
        await Servicio.RechazarAsync(9, 4, new string('m', 500));
        foreach (var malo in new[] { null, "", "corto", new string('m', 9), new string('m', 501) })
            await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio.RechazarAsync(9, 4, malo));
        Assert.Equal(3, _repo.Llamadas.Count);
        Assert.Equal(3, _not.Avisos.Count);
    }
}

internal sealed class AvisosFalsos : IAvisoRepositorio
{
    public List<string> Llamadas { get; } = [];

    public Task<IReadOnlyList<FilaAviso>> ListarAsync(int codUsuario, bool soloNoLeidos)
    {
        Llamadas.Add($"Listar({codUsuario},{soloNoLeidos})");
        return Task.FromResult<IReadOnlyList<FilaAviso>>([new() { CodAvso = 1, CodRndcn = 4, NomTtlo = "T", DscMnsje = "M", FlgLeido = "S", FecAvso = "F", HorAvso = "H" }, new() { CodAvso = 2, FlgLeido = "N" }]);
    }

    public Task<int> ContarNoLeidosAsync(int codUsuario) => Task.FromResult(6);
    public Task MarcarLeidosAsync(int codUsuario, int? codAviso) { Llamadas.Add($"Marcar({codUsuario},{codAviso?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "todos"})"); return Task.CompletedTask; }
    public Task<int> AgregarSuscripcionAsync(int codUsuario, string url, string claveP256, string claveAuth) { Llamadas.Add($"Suscribir({codUsuario},{url},{claveP256},{claveAuth})"); return Task.FromResult(1); }
    public Task<IReadOnlyList<FilaSuscripcion>> ListarSuscripcionesAsync(int codUsuario) => Task.FromResult<IReadOnlyList<FilaSuscripcion>>([]);
    public Task EliminarSuscripcionAsync(string url) { Llamadas.Add($"Baja({url})"); return Task.CompletedTask; }
}

public sealed class AvisoServiceTests
{
    private readonly SeguridadFalsa _seg = new();
    private readonly AvisosFalsos _repo = new();
    private AvisoService Servicio(OpcionesPush? push = null) => new(_repo, _seg, Options.Create(push ?? new OpcionesPush()));

    private static SolicitudSuscripcionPush Suscripcion(string? url = "https://push.example.com/abc", string? p256 = "0123456789", string? auth = "0123456789") => new(url, p256, auth);

    [Fact]
    public async Task Listar_contar_y_marcar_leidos_llegan_a_la_base_y_traducen_la_bandera()
    {
        var lista = await Servicio().ListarAsync(1, true);
        Assert.Equal([true, false], lista.Select(a => a.Leido));
        Assert.Equal(new AvisoDto(1, 4, "T", "M", true, "F", "H"), lista[0]);
        Assert.Equal(6, await Servicio().ContarAsync(1));
        await Servicio().MarcarLeidoAsync(1, null);
        await Servicio().MarcarLeidoAsync(1, 7);
        Assert.Equal(["Listar(1,True)", "Marcar(1,todos)", "Marcar(1,7)"], _repo.Llamadas);
        Assert.All(_seg.Llamadas, l => Assert.Empty(l.Roles));
        Assert.Equal(4, _seg.Llamadas.Count);
    }

    [Fact]
    public async Task Una_suscripcion_valida_se_guarda_con_los_textos_limpios()
    {
        await Servicio().SuscribirAsync(1, Suscripcion(" https://push.example.com/abc ", " 0123456789 ", " 0123456789 "));
        Assert.Equal(["Suscribir(1,https://push.example.com/abc,0123456789,0123456789)"], _repo.Llamadas);
        Assert.Single(_seg.Llamadas);
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("no-es-url")] [InlineData("http://push.example.com/abc")]
    public async Task La_direccion_debe_ser_una_url_https(string? url)
    {
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio().SuscribirAsync(1, Suscripcion(url)));
        Assert.Equal("La suscripción de avisos no es válida.", e.Message);
        Assert.Empty(_repo.Llamadas);
    }

    [Fact]
    public async Task La_url_admite_450_caracteres_y_no_451()
    {
        var de450 = "https://a.co/" + new string('x', 450 - "https://a.co/".Length);
        await Servicio().SuscribirAsync(1, Suscripcion(de450));
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio().SuscribirAsync(1, Suscripcion(de450 + "x")));
        Assert.Single(_repo.Llamadas);
    }

    [Theory]
    [InlineData("corta", "0123456789", "clave de la suscripción")]
    [InlineData("0123456789", "corta", "clave de autenticación")]
    public async Task Las_claves_de_la_suscripcion_se_validan(string p256, string auth, string campo)
    {
        var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio().SuscribirAsync(1, Suscripcion(p256: p256, auth: auth)));
        Assert.Contains(campo, e.Message);
        Assert.Empty(_repo.Llamadas);
    }

    [Fact]
    public async Task Las_claves_admiten_sus_maximos_y_no_uno_mas()
    {
        await Servicio().SuscribirAsync(1, Suscripcion(p256: new string('p', 200), auth: new string('a', 100)));
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio().SuscribirAsync(1, Suscripcion(p256: new string('p', 201))));
        await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio().SuscribirAsync(1, Suscripcion(auth: new string('a', 101))));
        Assert.Single(_repo.Llamadas);
    }

    [Fact]
    public async Task Darse_de_baja_exige_la_direccion_y_la_limpia()
    {
        await Servicio().DesuscribirAsync(1, new("  https://push.example.com/abc "));
        Assert.Equal(["Baja(https://push.example.com/abc)"], _repo.Llamadas);
        Assert.Single(_seg.Llamadas);

        foreach (var vacia in new string?[] { null, "", "   " })
        {
            var e = await Assert.ThrowsAsync<ErrorNegocio>(() => Servicio().DesuscribirAsync(1, new(vacia)));
            Assert.Equal("Falta la dirección de la suscripción.", e.Message);
        }
        Assert.Single(_repo.Llamadas);
    }

    [Fact]
    public void La_clave_publica_solo_se_entrega_si_los_avisos_estan_habilitados()
    {
        Assert.Equal(new ClavePushDto(false, ""), Servicio(new OpcionesPush { ClavePublica = "pub" }).ClavePublica());
        Assert.Equal(new ClavePushDto(true, "pub"), Servicio(new OpcionesPush { ClavePublica = "pub", ClavePrivada = "priv" }).ClavePublica());
    }
}
