/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_aggar_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Crea una rendicion en estado Borrador. Cada persona puede tener hasta
                 10 borradores a la vez.

    Ejemplo:
      exec dbo.pa_rgs_aggar_rndcn @cod_usrio = 5, @nom_ttlo = 'Viaje a Arequipa';
      -- Retorna un SELECT con 'cod_resultado' (codigo nuevo o -1) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_aggar_rndcn
    @cod_usrio int,
    @nom_ttlo  varchar(100)
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_rndcn int;
    declare @cod_hstrl int;

    begin try
        set @nom_ttlo = ltrim(rtrim(isnull(@nom_ttlo, '')));

        if not exists (select 1 from dbo.ta_rgs_mst_mov_usrio where cod_usrio = @cod_usrio and flg_actvo = 'S')
        begin
            select -1 as cod_resultado, 'El usuario no existe o está inactivo' as dsc_mensaje;
            return;
        end;

        if len(@nom_ttlo) < 3
        begin
            select -1 as cod_resultado, 'El título debe tener al menos 3 caracteres' as dsc_mensaje;
            return;
        end;

        if (select count(*) from dbo.ta_rgs_rndcn where cod_usrio = @cod_usrio and cod_estdo_rndcn = 1) >= 10
        begin
            select -1 as cod_resultado, 'Ya tienes 10 borradores. Envía o elimina alguno para crear otro' as dsc_mensaje;
            return;
        end;

        begin transaction;

        select @cod_rndcn = isnull(max(cod_rndcn), 0) + 1 from dbo.ta_rgs_rndcn with (updlock, holdlock);
        select @cod_hstrl = isnull(max(cod_hstrl), 0) + 1 from dbo.ta_rgs_hstrl with (updlock, holdlock);

        insert into dbo.ta_rgs_rndcn
            (cod_rndcn, cod_usrio, nom_ttlo, cod_estdo_rndcn, mnt_ttal, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values
            (@cod_rndcn, @cod_usrio, @nom_ttlo, 1, 0, getdate(), @cod_usrio, 1);

        insert into dbo.ta_rgs_hstrl
            (cod_hstrl, cod_rndcn, cod_accn_hstrl, cod_usrio, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values
            (@cod_hstrl, @cod_rndcn, 1, @cod_usrio, getdate(), @cod_usrio, 1);

        commit transaction;

        select @cod_rndcn as cod_resultado, 'Rendición creada' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_edtar_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Cambia el titulo de una rendicion. Solo su dueño y solo mientras es Borrador.

    Ejemplo:
      exec dbo.pa_rgs_edtar_rndcn @cod_rndcn = 1, @cod_usrio = 5, @nom_ttlo = 'Viaje a Cusco';
      -- Retorna un SELECT con 'cod_resultado' (1 si se guardo, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_edtar_rndcn
    @cod_rndcn int,
    @cod_usrio int,
    @nom_ttlo  varchar(100)
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_estdo int;

    begin try
        set @nom_ttlo = ltrim(rtrim(isnull(@nom_ttlo, '')));

        select @cod_estdo = cod_estdo_rndcn
        from dbo.ta_rgs_rndcn
        where cod_rndcn = @cod_rndcn and cod_usrio = @cod_usrio;

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'La rendición no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 1
        begin
            select -1 as cod_resultado, 'Solo se puede editar una rendición en borrador' as dsc_mensaje;
            return;
        end;

        if len(@nom_ttlo) < 3
        begin
            select -1 as cod_resultado, 'El título debe tener al menos 3 caracteres' as dsc_mensaje;
            return;
        end;

        update dbo.ta_rgs_rndcn
        set nom_ttlo = @nom_ttlo, cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_rndcn = @cod_rndcn;

        select 1 as cod_resultado, 'Rendición actualizada' as dsc_mensaje;
    end try
    begin catch
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_del_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Elimina una rendicion con sus gastos, comprobantes e historial.
                 Solo su dueño y solo mientras es Borrador.

    Ejemplo:
      exec dbo.pa_rgs_del_rndcn @cod_rndcn = 1, @cod_usrio = 5;
      -- Retorna un SELECT con 'cod_resultado' (1 si se elimino, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_del_rndcn
    @cod_rndcn int,
    @cod_usrio int
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_estdo int;

    begin try
        select @cod_estdo = cod_estdo_rndcn
        from dbo.ta_rgs_rndcn
        where cod_rndcn = @cod_rndcn and cod_usrio = @cod_usrio;

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'La rendición no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 1
        begin
            select -1 as cod_resultado, 'Solo se puede eliminar una rendición en borrador' as dsc_mensaje;
            return;
        end;

        begin transaction;

        delete c from dbo.ta_rgs_gasto_cmprb c
        inner join dbo.ta_rgs_gasto g on g.cod_gasto = c.cod_gasto
        where g.cod_rndcn = @cod_rndcn;

        delete from dbo.ta_rgs_gasto where cod_rndcn = @cod_rndcn;
        delete from dbo.ta_rgs_hstrl where cod_rndcn = @cod_rndcn;
        update dbo.ta_rgs_avso set cod_rndcn = null where cod_rndcn = @cod_rndcn;
        delete from dbo.ta_rgs_rndcn where cod_rndcn = @cod_rndcn;

        commit transaction;

        select 1 as cod_resultado, 'Rendición eliminada' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_lst_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Lista las rendiciones. Con @flg_modo = 'P' devuelve las propias de la persona;
                 con 'A' las de las personas a cargo de ese aprobador (el administrador ve
                 todas). Se puede filtrar por estado.

    Ejemplo:
      exec dbo.pa_rgs_lst_rndcn @cod_usrio = 2, @flg_modo = 'A', @cod_estdo_rndcn = 2;
      -- Retorna una lista de rendiciones
*/
create or alter procedure dbo.pa_rgs_lst_rndcn
    @cod_usrio       int,
    @flg_modo        char(1) = 'P',
    @cod_estdo_rndcn int = null
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_rol int;
    select @cod_rol = cod_rol from dbo.ta_rgs_mst_mov_usrio where cod_usrio = @cod_usrio and flg_actvo = 'S';

    select
        r.cod_rndcn, r.nom_ttlo,
        r.cod_usrio, u.nom_usrio,
        r.cod_estdo_rndcn, e.nom_estdo_rndcn,
        r.mnt_ttal,
        (select count(*) from dbo.ta_rgs_gasto g where g.cod_rndcn = r.cod_rndcn) as num_gastos,
        convert(varchar(10), r.cnr_fec_rgtro, 103) as fec_crcn,
        isnull(convert(varchar(10), r.fec_envio, 103), '') as fec_envio
    from dbo.ta_rgs_rndcn r
    inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio
    inner join dbo.ta_rgs_mst_estdo_rndcn e on e.cod_estdo_rndcn = r.cod_estdo_rndcn
    where @cod_rol is not null
      and (@cod_estdo_rndcn is null or r.cod_estdo_rndcn = @cod_estdo_rndcn)
      and (
            (@flg_modo = 'P' and r.cod_usrio = @cod_usrio)
         or (@flg_modo = 'A' and @cod_rol in (2, 3)
             and r.cod_estdo_rndcn <> 1
             and r.cod_usrio <> @cod_usrio
             and (@cod_rol = 3 or u.cod_aprbdr = @cod_usrio))
          )
    order by r.cnr_fec_rgtro desc, r.cod_rndcn desc;
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_sel_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Devuelve una rendicion en tres resultados: cabecera, gastos e historial.
                 Solo la ve su dueño, su aprobador o un administrador; si no, no devuelve nada.

    Ejemplo:
      exec dbo.pa_rgs_sel_rndcn @cod_rndcn = 1, @cod_usrio_cnslt = 5;
      -- Retorna 3 resultados (cabecera vacia si no existe o no tiene acceso)
*/
create or alter procedure dbo.pa_rgs_sel_rndcn
    @cod_rndcn       int,
    @cod_usrio_cnslt int
as
begin
    set nocount on;
    set dateformat dmy;

    declare @permitido bit = 0;

    if exists (
        select 1
        from dbo.ta_rgs_rndcn r
        inner join dbo.ta_rgs_mst_mov_usrio d on d.cod_usrio = r.cod_usrio
        inner join dbo.ta_rgs_mst_mov_usrio c on c.cod_usrio = @cod_usrio_cnslt and c.flg_actvo = 'S'
        where r.cod_rndcn = @cod_rndcn
          and (r.cod_usrio = c.cod_usrio
               or c.cod_rol = 3
               or (c.cod_rol = 2 and d.cod_aprbdr = c.cod_usrio and r.cod_estdo_rndcn <> 1))
    )
        set @permitido = 1;

    select
        r.cod_rndcn, r.nom_ttlo,
        r.cod_usrio, u.nom_usrio, u.cod_aprbdr, a.nom_usrio as nom_aprbdr,
        r.cod_estdo_rndcn, e.nom_estdo_rndcn,
        r.mnt_ttal,
        isnull(r.dsc_mtvo_rchzo, '') as dsc_mtvo_rchzo,
        convert(varchar(10), r.cnr_fec_rgtro, 103) as fec_crcn,
        isnull(convert(varchar(10), r.fec_envio, 103), '') as fec_envio,
        isnull(convert(varchar(10), r.fec_rsln, 103), '') as fec_rsln,
        isnull(rs.nom_usrio, '') as nom_rsltr
    from dbo.ta_rgs_rndcn r
    inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio
    inner join dbo.ta_rgs_mst_estdo_rndcn e on e.cod_estdo_rndcn = r.cod_estdo_rndcn
    left join dbo.ta_rgs_mst_mov_usrio a on a.cod_usrio = u.cod_aprbdr
    left join dbo.ta_rgs_mst_mov_usrio rs on rs.cod_usrio = r.cod_usrio_rsltr
    where r.cod_rndcn = @cod_rndcn and @permitido = 1;

    select
        g.cod_gasto, g.cod_ctgra, c.nom_ctgra, c.flg_cmprb_oblgt,
        convert(varchar(10), g.fec_gasto, 103) as fec_gasto,
        g.dsc_gasto, g.mnt_gasto,
        isnull(g.nom_archv, '') as nom_archv,
        isnull(g.tpo_cntnd, '') as tpo_cntnd,
        isnull(g.num_tamno, 0) as num_tamno
    from dbo.ta_rgs_gasto g
    inner join dbo.ta_rgs_mst_mov_ctgra c on c.cod_ctgra = g.cod_ctgra
    where g.cod_rndcn = @cod_rndcn and @permitido = 1
    order by g.fec_gasto, g.cod_gasto;

    select
        h.cod_hstrl, h.cod_accn_hstrl, a.nom_accn_hstrl,
        h.cod_usrio, u.nom_usrio,
        isnull(h.dsc_cmntr, '') as dsc_cmntr,
        convert(varchar(10), h.cnr_fec_rgtro, 103) as fec_hstrl,
        convert(varchar(5), h.cnr_fec_rgtro, 108) as hor_hstrl
    from dbo.ta_rgs_hstrl h
    inner join dbo.ta_rgs_mst_accn_hstrl a on a.cod_accn_hstrl = h.cod_accn_hstrl
    inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = h.cod_usrio
    where h.cod_rndcn = @cod_rndcn and @permitido = 1
    order by h.cod_hstrl;
end;
go
