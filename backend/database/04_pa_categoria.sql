/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_lst_ctgra
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Lista las categorias de gasto. Con @flg_slo_actvas = 'S' devuelve solo las activas (combo).

    Ejemplo:
      exec dbo.pa_rgs_lst_ctgra @flg_slo_actvas = 'S';
      -- Retorna una lista de categorias
*/
create or alter procedure dbo.pa_rgs_lst_ctgra
    @flg_slo_actvas char(1) = 'N'
as
begin
    set nocount on;

    select cod_ctgra, nom_ctgra, dsc_ctgra, mnt_lmte, flg_cmprb_oblgt, flg_actvo
    from dbo.ta_rgs_mst_mov_ctgra
    where @flg_slo_actvas <> 'S' or flg_actvo = 'S'
    order by nom_ctgra;
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_aggar_ctgra
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Registra una categoria de gasto con su limite por gasto
                 y si exige comprobante.

    Ejemplo:
      exec dbo.pa_rgs_aggar_ctgra @nom_ctgra = 'Capacitación', @dsc_ctgra = 'Cursos', @mnt_lmte = 400,
           @flg_cmprb_oblgt = 'S', @cod_usrio_accn = 3;
      -- Retorna un SELECT con 'cod_resultado' (codigo nuevo o -1) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_aggar_ctgra
    @nom_ctgra       varchar(60),
    @dsc_ctgra       varchar(200),
    @mnt_lmte        decimal(12,2),
    @flg_cmprb_oblgt char(1),
    @cod_usrio_accn  int
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_ctgra int;
    declare @dsc_mensaje varchar(200);

    begin try
        set @nom_ctgra = ltrim(rtrim(isnull(@nom_ctgra, '')));
        set @dsc_ctgra = ltrim(rtrim(isnull(@dsc_ctgra, '')));

        if len(@nom_ctgra) < 2
        begin
            select -1 as cod_resultado, 'El nombre debe tener al menos 2 caracteres' as dsc_mensaje;
            return;
        end;

        if @dsc_ctgra = ''
        begin
            select -1 as cod_resultado, 'La descripción es obligatoria' as dsc_mensaje;
            return;
        end;

        if isnull(@mnt_lmte, 0) <= 0
        begin
            select -1 as cod_resultado, 'El límite debe ser mayor que cero' as dsc_mensaje;
            return;
        end;

        if @flg_cmprb_oblgt not in ('S', 'N')
        begin
            select -1 as cod_resultado, 'Indica si el comprobante es obligatorio' as dsc_mensaje;
            return;
        end;

        begin transaction;

        if exists (select 1 from dbo.ta_rgs_mst_mov_ctgra with (updlock, holdlock) where nom_ctgra = @nom_ctgra)
        begin
            set @cod_ctgra = -1;
            set @dsc_mensaje = 'Ya existe una categoría con ese nombre';
        end
        else
        begin
            select @cod_ctgra = isnull(max(cod_ctgra), 0) + 1
            from dbo.ta_rgs_mst_mov_ctgra with (updlock, holdlock);

            insert into dbo.ta_rgs_mst_mov_ctgra
                (cod_ctgra, nom_ctgra, dsc_ctgra, mnt_lmte, flg_cmprb_oblgt, flg_actvo,
                 cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
            values
                (@cod_ctgra, @nom_ctgra, @dsc_ctgra, @mnt_lmte, @flg_cmprb_oblgt, 'S',
                 getdate(), @cod_usrio_accn, 1);

            set @dsc_mensaje = 'Categoría registrada';
        end;

        commit transaction;

        select @cod_ctgra as cod_resultado, @dsc_mensaje as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_edtar_ctgra
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Modifica una categoria. El cambio de limite solo rige para gastos nuevos;
                 los gastos ya registrados no se tocan.

    Ejemplo:
      exec dbo.pa_rgs_edtar_ctgra @cod_ctgra = 1, @nom_ctgra = 'Transporte', @dsc_ctgra = 'Pasajes',
           @mnt_lmte = 250, @flg_cmprb_oblgt = 'N', @flg_actvo = 'S', @cod_usrio_accn = 3;
      -- Retorna un SELECT con 'cod_resultado' (1 si se guardo, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_edtar_ctgra
    @cod_ctgra       int,
    @nom_ctgra       varchar(60),
    @dsc_ctgra       varchar(200),
    @mnt_lmte        decimal(12,2),
    @flg_cmprb_oblgt char(1),
    @flg_actvo       char(1),
    @cod_usrio_accn  int
as
begin
    set nocount on;
    set dateformat dmy;

    begin try
        set @nom_ctgra = ltrim(rtrim(isnull(@nom_ctgra, '')));
        set @dsc_ctgra = ltrim(rtrim(isnull(@dsc_ctgra, '')));

        if not exists (select 1 from dbo.ta_rgs_mst_mov_ctgra where cod_ctgra = @cod_ctgra)
        begin
            select -1 as cod_resultado, 'La categoría no existe' as dsc_mensaje;
            return;
        end;

        if len(@nom_ctgra) < 2 or @dsc_ctgra = ''
        begin
            select -1 as cod_resultado, 'El nombre y la descripción son obligatorios' as dsc_mensaje;
            return;
        end;

        if isnull(@mnt_lmte, 0) <= 0
        begin
            select -1 as cod_resultado, 'El límite debe ser mayor que cero' as dsc_mensaje;
            return;
        end;

        if @flg_cmprb_oblgt not in ('S', 'N') or @flg_actvo not in ('S', 'N')
        begin
            select -1 as cod_resultado, 'Los indicadores deben ser S o N' as dsc_mensaje;
            return;
        end;

        if exists (select 1 from dbo.ta_rgs_mst_mov_ctgra where nom_ctgra = @nom_ctgra and cod_ctgra <> @cod_ctgra)
        begin
            select -1 as cod_resultado, 'Ya existe una categoría con ese nombre' as dsc_mensaje;
            return;
        end;

        update dbo.ta_rgs_mst_mov_ctgra
        set nom_ctgra = @nom_ctgra,
            dsc_ctgra = @dsc_ctgra,
            mnt_lmte = @mnt_lmte,
            flg_cmprb_oblgt = @flg_cmprb_oblgt,
            flg_actvo = @flg_actvo,
            cnr_fec_mdfcn = getdate(),
            cnr_usr_mdfcn = @cod_usrio_accn
        where cod_ctgra = @cod_ctgra;

        select 1 as cod_resultado, 'Categoría actualizada' as dsc_mensaje;
    end try
    begin catch
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go
