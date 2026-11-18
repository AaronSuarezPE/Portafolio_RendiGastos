using System.Data;
using Dapper;

namespace RendiGastos.Api.Datos;

public interface IUsuarioRepositorio
{
    Task<int> AgregarAsync(string nombre, string email, string claveHash, int codRol, int? codAprobador, int codUsuarioAccion);
    Task<FilaUsuario?> ObtenerPorEmailAsync(string email);
    Task<FilaUsuario?> ObtenerPorCodigoAsync(int codUsuario);
    Task<IReadOnlyList<FilaUsuario>> ListarAsync(bool soloAprobadores);
    Task EditarAsync(int codUsuario, string nombre, int codRol, int? codAprobador, bool activo, int codUsuarioAccion);
    Task RegistrarIngresoAsync(int codUsuario);
}

public sealed class UsuarioRepositorio(IFabricaConexion fabrica) : IUsuarioRepositorio
{
    public async Task<int> AgregarAsync(string nombre, string email, string claveHash, int codRol, int? codAprobador, int codUsuarioAccion)
    {
        await using var conexion = fabrica.Crear();
        var r = await conexion.EjecutarAsync("dbo.pa_rgs_aggar_usrio", new
        {
            nom_usrio = nombre, nom_email = email, nom_clave_hsh = claveHash,
            cod_rol = codRol, cod_aprbdr = codAprobador, cod_usrio_accn = codUsuarioAccion,
        });
        return r.CodResultado;
    }

    public async Task<FilaUsuario?> ObtenerPorEmailAsync(string email)
    {
        await using var conexion = fabrica.Crear();
        return await conexion.QuerySingleOrDefaultAsync<FilaUsuario>(
            "dbo.pa_rgs_sel_usrio", new { nom_email = email }, commandType: CommandType.StoredProcedure);
    }

    public async Task<FilaUsuario?> ObtenerPorCodigoAsync(int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        return await conexion.QuerySingleOrDefaultAsync<FilaUsuario>(
            "dbo.pa_rgs_sel_usrio", new { cod_usrio = codUsuario }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<FilaUsuario>> ListarAsync(bool soloAprobadores)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.ListarAsync<FilaUsuario>("dbo.pa_rgs_lst_usrio",
            new { flg_slo_aprbdr = soloAprobadores ? "S" : "N" })).ToList();
    }

    public async Task EditarAsync(int codUsuario, string nombre, int codRol, int? codAprobador, bool activo, int codUsuarioAccion)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_edtar_usrio", new
        {
            cod_usrio = codUsuario, nom_usrio = nombre, cod_rol = codRol, cod_aprbdr = codAprobador,
            flg_actvo = activo ? "S" : "N", cod_usrio_accn = codUsuarioAccion,
        });
    }

    public async Task RegistrarIngresoAsync(int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_edtar_login", new { cod_usrio = codUsuario });
    }
}

public interface ICategoriaRepositorio
{
    Task<IReadOnlyList<FilaCategoria>> ListarAsync(bool soloActivas);
    Task<int> AgregarAsync(string nombre, string descripcion, decimal limite, bool comprobante, int codUsuarioAccion);
    Task EditarAsync(int codCategoria, string nombre, string descripcion, decimal limite, bool comprobante, bool activa, int codUsuarioAccion);
}

public sealed class CategoriaRepositorio(IFabricaConexion fabrica) : ICategoriaRepositorio
{
    public async Task<IReadOnlyList<FilaCategoria>> ListarAsync(bool soloActivas)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.ListarAsync<FilaCategoria>("dbo.pa_rgs_lst_ctgra",
            new { flg_slo_actvas = soloActivas ? "S" : "N" })).ToList();
    }

    public async Task<int> AgregarAsync(string nombre, string descripcion, decimal limite, bool comprobante, int codUsuarioAccion)
    {
        await using var conexion = fabrica.Crear();
        var r = await conexion.EjecutarAsync("dbo.pa_rgs_aggar_ctgra", new
        {
            nom_ctgra = nombre, dsc_ctgra = descripcion, mnt_lmte = limite,
            flg_cmprb_oblgt = comprobante ? "S" : "N", cod_usrio_accn = codUsuarioAccion,
        });
        return r.CodResultado;
    }

    public async Task EditarAsync(int codCategoria, string nombre, string descripcion, decimal limite, bool comprobante, bool activa, int codUsuarioAccion)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_edtar_ctgra", new
        {
            cod_ctgra = codCategoria, nom_ctgra = nombre, dsc_ctgra = descripcion, mnt_lmte = limite,
            flg_cmprb_oblgt = comprobante ? "S" : "N", flg_actvo = activa ? "S" : "N", cod_usrio_accn = codUsuarioAccion,
        });
    }
}

public interface IRendicionRepositorio
{
    Task<int> AgregarAsync(int codUsuario, string titulo);
    Task EditarAsync(int codRendicion, int codUsuario, string titulo);
    Task EliminarAsync(int codRendicion, int codUsuario);
    Task<IReadOnlyList<FilaRendicionResumen>> ListarAsync(int codUsuario, string modo, int? codEstado);
    Task<(FilaRendicionCabecera? Cabecera, IReadOnlyList<FilaGasto> Gastos, IReadOnlyList<FilaHistorial> Historial)>
        ObtenerAsync(int codRendicion, int codUsuarioConsulta);

    Task<int> AgregarGastoAsync(int codRendicion, int codUsuario, int codCategoria, string fecha, string descripcion, decimal monto);
    Task EditarGastoAsync(int codGasto, int codUsuario, int codCategoria, string fecha, string descripcion, decimal monto);
    Task EliminarGastoAsync(int codGasto, int codUsuario);
    Task GuardarComprobanteAsync(int codGasto, int codUsuario, string nombre, string tipo, byte[] contenido);
    Task QuitarComprobanteAsync(int codGasto, int codUsuario);
    Task<FilaComprobante?> ObtenerComprobanteAsync(int codGasto, int codUsuarioConsulta);

    Task<int> EnviarAsync(int codRendicion, int codUsuario);
    Task<int> AprobarAsync(int codRendicion, int codAprobador, string? comentario);
    Task<int> RechazarAsync(int codRendicion, int codAprobador, string motivo);
    Task ReabrirAsync(int codRendicion, int codUsuario);
}

public sealed class RendicionRepositorio(IFabricaConexion fabrica) : IRendicionRepositorio
{
    public async Task<int> AgregarAsync(int codUsuario, string titulo)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.EjecutarAsync("dbo.pa_rgs_aggar_rndcn", new { cod_usrio = codUsuario, nom_ttlo = titulo })).CodResultado;
    }

    public async Task EditarAsync(int codRendicion, int codUsuario, string titulo)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_edtar_rndcn", new { cod_rndcn = codRendicion, cod_usrio = codUsuario, nom_ttlo = titulo });
    }

    public async Task EliminarAsync(int codRendicion, int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_del_rndcn", new { cod_rndcn = codRendicion, cod_usrio = codUsuario });
    }

    public async Task<IReadOnlyList<FilaRendicionResumen>> ListarAsync(int codUsuario, string modo, int? codEstado)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.ListarAsync<FilaRendicionResumen>("dbo.pa_rgs_lst_rndcn",
            new { cod_usrio = codUsuario, flg_modo = modo, cod_estdo_rndcn = codEstado })).ToList();
    }

    public async Task<(FilaRendicionCabecera? Cabecera, IReadOnlyList<FilaGasto> Gastos, IReadOnlyList<FilaHistorial> Historial)>
        ObtenerAsync(int codRendicion, int codUsuarioConsulta)
    {
        await using var conexion = fabrica.Crear();
        using var multiple = await conexion.QueryMultipleAsync("dbo.pa_rgs_sel_rndcn",
            new { cod_rndcn = codRendicion, cod_usrio_cnslt = codUsuarioConsulta }, commandType: CommandType.StoredProcedure);

        var cabecera = await multiple.ReadSingleOrDefaultAsync<FilaRendicionCabecera>();
        var gastos = (await multiple.ReadAsync<FilaGasto>()).ToList();
        var historial = (await multiple.ReadAsync<FilaHistorial>()).ToList();
        return (cabecera, gastos, historial);
    }

    public async Task<int> AgregarGastoAsync(int codRendicion, int codUsuario, int codCategoria, string fecha, string descripcion, decimal monto)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.EjecutarAsync("dbo.pa_rgs_aggar_gasto", new
        {
            cod_rndcn = codRendicion, cod_usrio = codUsuario, cod_ctgra = codCategoria,
            fec_gasto = fecha, dsc_gasto = descripcion, mnt_gasto = monto,
        })).CodResultado;
    }

    public async Task EditarGastoAsync(int codGasto, int codUsuario, int codCategoria, string fecha, string descripcion, decimal monto)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_edtar_gasto", new
        {
            cod_gasto = codGasto, cod_usrio = codUsuario, cod_ctgra = codCategoria,
            fec_gasto = fecha, dsc_gasto = descripcion, mnt_gasto = monto,
        });
    }

    public async Task EliminarGastoAsync(int codGasto, int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_del_gasto", new { cod_gasto = codGasto, cod_usrio = codUsuario });
    }

    public async Task GuardarComprobanteAsync(int codGasto, int codUsuario, string nombre, string tipo, byte[] contenido)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_aggar_cmprb", new
        {
            cod_gasto = codGasto, cod_usrio = codUsuario, nom_archv = nombre,
            tpo_cntnd = tipo, num_tamno = contenido.Length, bin_cntnd = contenido,
        });
    }

    public async Task QuitarComprobanteAsync(int codGasto, int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_del_cmprb", new { cod_gasto = codGasto, cod_usrio = codUsuario });
    }

    public async Task<FilaComprobante?> ObtenerComprobanteAsync(int codGasto, int codUsuarioConsulta)
    {
        await using var conexion = fabrica.Crear();
        return await conexion.QuerySingleOrDefaultAsync<FilaComprobante>("dbo.pa_rgs_sel_cmprb",
            new { cod_gasto = codGasto, cod_usrio_cnslt = codUsuarioConsulta }, commandType: CommandType.StoredProcedure);
    }

    public async Task<int> EnviarAsync(int codRendicion, int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.EjecutarAsync("dbo.pa_rgs_envr_rndcn", new { cod_rndcn = codRendicion, cod_usrio = codUsuario })).CodResultado;
    }

    public async Task<int> AprobarAsync(int codRendicion, int codAprobador, string? comentario)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.EjecutarAsync("dbo.pa_rgs_aprbr_rndcn",
            new { cod_rndcn = codRendicion, cod_usrio_aprbdr = codAprobador, dsc_cmntr = comentario })).CodResultado;
    }

    public async Task<int> RechazarAsync(int codRendicion, int codAprobador, string motivo)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.EjecutarAsync("dbo.pa_rgs_rchzr_rndcn",
            new { cod_rndcn = codRendicion, cod_usrio_aprbdr = codAprobador, dsc_mtvo = motivo })).CodResultado;
    }

    public async Task ReabrirAsync(int codRendicion, int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_rabrr_rndcn", new { cod_rndcn = codRendicion, cod_usrio = codUsuario });
    }
}

public interface IAvisoRepositorio
{
    Task<IReadOnlyList<FilaAviso>> ListarAsync(int codUsuario, bool soloNoLeidos);
    Task<int> ContarNoLeidosAsync(int codUsuario);
    Task MarcarLeidosAsync(int codUsuario, int? codAviso);
    Task<int> AgregarSuscripcionAsync(int codUsuario, string url, string claveP256, string claveAuth);
    Task<IReadOnlyList<FilaSuscripcion>> ListarSuscripcionesAsync(int codUsuario);
    Task EliminarSuscripcionAsync(string url);
}

public sealed class AvisoRepositorio(IFabricaConexion fabrica) : IAvisoRepositorio
{
    public async Task<IReadOnlyList<FilaAviso>> ListarAsync(int codUsuario, bool soloNoLeidos)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.ListarAsync<FilaAviso>("dbo.pa_rgs_lst_avso",
            new { cod_usrio = codUsuario, flg_slo_nleidos = soloNoLeidos ? "S" : "N" })).ToList();
    }

    public async Task<int> ContarNoLeidosAsync(int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.QuerySingleAsync<FilaConteo>("dbo.pa_rgs_cnt_avso",
            new { cod_usrio = codUsuario }, commandType: CommandType.StoredProcedure)).NumNleidos;
    }

    public async Task MarcarLeidosAsync(int codUsuario, int? codAviso)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_edtar_avso", new { cod_usrio = codUsuario, cod_avso = codAviso });
    }

    public async Task<int> AgregarSuscripcionAsync(int codUsuario, string url, string claveP256, string claveAuth)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.EjecutarAsync("dbo.pa_rgs_aggar_sbscr", new
        {
            cod_usrio = codUsuario, nom_url_ptnto = url, nom_llave_p256 = claveP256, nom_llave_auth = claveAuth,
        })).CodResultado;
    }

    public async Task<IReadOnlyList<FilaSuscripcion>> ListarSuscripcionesAsync(int codUsuario)
    {
        await using var conexion = fabrica.Crear();
        return (await conexion.ListarAsync<FilaSuscripcion>("dbo.pa_rgs_lst_sbscr", new { cod_usrio = codUsuario })).ToList();
    }

    public async Task EliminarSuscripcionAsync(string url)
    {
        await using var conexion = fabrica.Crear();
        await conexion.EjecutarAsync("dbo.pa_rgs_del_sbscr", new { nom_url_ptnto = url });
    }
}
