/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_aggar_gasto
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Agrega un gasto a una rendicion en borrador de la persona.
                 Reglas: categoria activa; fecha valida, no futura y de los ultimos 90 dias;
                 monto mayor que cero y dentro del limite de la categoria; maximo 50 gastos
                 por rendicion; no se repite el mismo gasto (fecha, categoria, monto y descripcion).

    Ejemplo:
      exec dbo.pa_rgs_aggar_gasto @cod_rndcn = 1, @cod_usrio = 5, @cod_ctgra = 1,
           @fec_gasto = '15/11/2026', @dsc_gasto = 'Taxi al aeropuerto', @mnt_gasto = 45.50;
      -- Retorna un SELECT con 'cod_resultado' (codigo nuevo o -1) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_aggar_gasto
    @cod_rndcn int,
    @cod_usrio int,
    @cod_ctgra int,
    @fec_gasto varchar(10),
    @dsc_gasto varchar(200),
    @mnt_gasto decimal(12,2)
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_gasto int;
    declare @fec date = try_convert(date, @fec_gasto, 103);
    declare @cod_estdo int;
    declare @mnt_lmte decimal(12,2);
    declare @flg_actvo char(1);

    begin try
        set @dsc_gasto = ltrim(rtrim(isnull(@dsc_gasto, '')));

        select @cod_estdo = cod_estdo_rndcn from dbo.ta_rgs_rndcn where cod_rndcn = @cod_rndcn and cod_usrio = @cod_usrio;

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'La rendición no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 1
        begin
            select -1 as cod_resultado, 'Solo se pueden agregar gastos a una rendición en borrador' as dsc_mensaje;
            return;
        end;

        select @mnt_lmte = mnt_lmte, @flg_actvo = flg_actvo from dbo.ta_rgs_mst_mov_ctgra where cod_ctgra = @cod_ctgra;

        if @mnt_lmte is null or @flg_actvo <> 'S'
        begin
            select -1 as cod_resultado, 'La categoría no existe o está inactiva' as dsc_mensaje;
            return;
        end;

        if @fec is null
        begin
            select -1 as cod_resultado, 'La fecha no es válida (usa dd/mm/aaaa)' as dsc_mensaje;
            return;
        end;

        if @fec > cast(getdate() as date)
        begin
            select -1 as cod_resultado, 'La fecha del gasto no puede ser futura' as dsc_mensaje;
            return;
        end;

        if @fec < dateadd(day, -90, cast(getdate() as date))
        begin
            select -1 as cod_resultado, 'Solo se aceptan gastos de los últimos 90 días' as dsc_mensaje;
            return;
        end;

        if len(@dsc_gasto) < 3
        begin
            select -1 as cod_resultado, 'La descripción debe tener al menos 3 caracteres' as dsc_mensaje;
            return;
        end;

        if isnull(@mnt_gasto, 0) <= 0
        begin
            select -1 as cod_resultado, 'El monto debe ser mayor que cero' as dsc_mensaje;
            return;
        end;

        if @mnt_gasto > @mnt_lmte
        begin
            select -1 as cod_resultado,
                   'El monto supera el límite de la categoría (' + convert(varchar(20), @mnt_lmte) + ')' as dsc_mensaje;
            return;
        end;

        if (select count(*) from dbo.ta_rgs_gasto where cod_rndcn = @cod_rndcn) >= 50
        begin
            select -1 as cod_resultado, 'Una rendición admite hasta 50 gastos' as dsc_mensaje;
            return;
        end;

        if exists (
            select 1 from dbo.ta_rgs_gasto g
            inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
            where r.cod_usrio = @cod_usrio and g.fec_gasto = @fec and g.cod_ctgra = @cod_ctgra
              and g.mnt_gasto = @mnt_gasto and g.dsc_gasto = @dsc_gasto
        )
        begin
            select -1 as cod_resultado, 'Ya registraste este mismo gasto' as dsc_mensaje;
            return;
        end;

        begin transaction;

        select @cod_gasto = isnull(max(cod_gasto), 0) + 1 from dbo.ta_rgs_gasto with (updlock, holdlock);

        insert into dbo.ta_rgs_gasto
            (cod_gasto, cod_rndcn, cod_ctgra, fec_gasto, dsc_gasto, mnt_gasto, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values
            (@cod_gasto, @cod_rndcn, @cod_ctgra, @fec, @dsc_gasto, @mnt_gasto, getdate(), @cod_usrio, 1);

        update dbo.ta_rgs_rndcn
        set mnt_ttal = (select sum(mnt_gasto) from dbo.ta_rgs_gasto where cod_rndcn = @cod_rndcn),
            cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_rndcn = @cod_rndcn;

        commit transaction;

        select @cod_gasto as cod_resultado, 'Gasto agregado' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_edtar_gasto
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Modifica un gasto de una rendicion en borrador. Mismas reglas que al agregar.

    Ejemplo:
      exec dbo.pa_rgs_edtar_gasto @cod_gasto = 1, @cod_usrio = 5, @cod_ctgra = 1,
           @fec_gasto = '15/11/2026', @dsc_gasto = 'Taxi', @mnt_gasto = 40;
      -- Retorna un SELECT con 'cod_resultado' (1 si se guardo, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_edtar_gasto
    @cod_gasto int,
    @cod_usrio int,
    @cod_ctgra int,
    @fec_gasto varchar(10),
    @dsc_gasto varchar(200),
    @mnt_gasto decimal(12,2)
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_rndcn int;
    declare @cod_estdo int;
    declare @fec date = try_convert(date, @fec_gasto, 103);
    declare @mnt_lmte decimal(12,2);
    declare @flg_actvo char(1);

    begin try
        set @dsc_gasto = ltrim(rtrim(isnull(@dsc_gasto, '')));

        select @cod_rndcn = r.cod_rndcn, @cod_estdo = r.cod_estdo_rndcn
        from dbo.ta_rgs_gasto g
        inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
        where g.cod_gasto = @cod_gasto and r.cod_usrio = @cod_usrio;

        if @cod_rndcn is null
        begin
            select -1 as cod_resultado, 'El gasto no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 1
        begin
            select -1 as cod_resultado, 'Solo se pueden editar gastos de una rendición en borrador' as dsc_mensaje;
            return;
        end;

        select @mnt_lmte = mnt_lmte, @flg_actvo = flg_actvo from dbo.ta_rgs_mst_mov_ctgra where cod_ctgra = @cod_ctgra;

        if @mnt_lmte is null or @flg_actvo <> 'S'
        begin
            select -1 as cod_resultado, 'La categoría no existe o está inactiva' as dsc_mensaje;
            return;
        end;

        if @fec is null
        begin
            select -1 as cod_resultado, 'La fecha no es válida (usa dd/mm/aaaa)' as dsc_mensaje;
            return;
        end;

        if @fec > cast(getdate() as date)
        begin
            select -1 as cod_resultado, 'La fecha del gasto no puede ser futura' as dsc_mensaje;
            return;
        end;

        if @fec < dateadd(day, -90, cast(getdate() as date))
        begin
            select -1 as cod_resultado, 'Solo se aceptan gastos de los últimos 90 días' as dsc_mensaje;
            return;
        end;

        if len(@dsc_gasto) < 3
        begin
            select -1 as cod_resultado, 'La descripción debe tener al menos 3 caracteres' as dsc_mensaje;
            return;
        end;

        if isnull(@mnt_gasto, 0) <= 0
        begin
            select -1 as cod_resultado, 'El monto debe ser mayor que cero' as dsc_mensaje;
            return;
        end;

        if @mnt_gasto > @mnt_lmte
        begin
            select -1 as cod_resultado,
                   'El monto supera el límite de la categoría (' + convert(varchar(20), @mnt_lmte) + ')' as dsc_mensaje;
            return;
        end;

        if exists (
            select 1 from dbo.ta_rgs_gasto g
            inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
            where r.cod_usrio = @cod_usrio and g.cod_gasto <> @cod_gasto and g.fec_gasto = @fec
              and g.cod_ctgra = @cod_ctgra and g.mnt_gasto = @mnt_gasto and g.dsc_gasto = @dsc_gasto
        )
        begin
            select -1 as cod_resultado, 'Ya registraste este mismo gasto' as dsc_mensaje;
            return;
        end;

        begin transaction;

        update dbo.ta_rgs_gasto
        set cod_ctgra = @cod_ctgra, fec_gasto = @fec, dsc_gasto = @dsc_gasto, mnt_gasto = @mnt_gasto,
            cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_gasto = @cod_gasto;

        update dbo.ta_rgs_rndcn
        set mnt_ttal = (select sum(mnt_gasto) from dbo.ta_rgs_gasto where cod_rndcn = @cod_rndcn),
            cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_rndcn = @cod_rndcn;

        commit transaction;

        select 1 as cod_resultado, 'Gasto actualizado' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_del_gasto
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Elimina un gasto y su comprobante de una rendicion en borrador.

    Ejemplo:
      exec dbo.pa_rgs_del_gasto @cod_gasto = 1, @cod_usrio = 5;
      -- Retorna un SELECT con 'cod_resultado' (1 si se elimino, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_del_gasto
    @cod_gasto int,
    @cod_usrio int
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_rndcn int;
    declare @cod_estdo int;

    begin try
        select @cod_rndcn = r.cod_rndcn, @cod_estdo = r.cod_estdo_rndcn
        from dbo.ta_rgs_gasto g
        inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
        where g.cod_gasto = @cod_gasto and r.cod_usrio = @cod_usrio;

        if @cod_rndcn is null
        begin
            select -1 as cod_resultado, 'El gasto no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 1
        begin
            select -1 as cod_resultado, 'Solo se pueden eliminar gastos de una rendición en borrador' as dsc_mensaje;
            return;
        end;

        begin transaction;

        delete from dbo.ta_rgs_gasto_cmprb where cod_gasto = @cod_gasto;
        delete from dbo.ta_rgs_gasto where cod_gasto = @cod_gasto;

        update dbo.ta_rgs_rndcn
        set mnt_ttal = isnull((select sum(mnt_gasto) from dbo.ta_rgs_gasto where cod_rndcn = @cod_rndcn), 0),
            cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_rndcn = @cod_rndcn;

        commit transaction;

        select 1 as cod_resultado, 'Gasto eliminado' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_aggar_cmprb
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Adjunta (o reemplaza) el comprobante de un gasto de una rendicion en borrador.

    Ejemplo:
      exec dbo.pa_rgs_aggar_cmprb @cod_gasto = 1, @cod_usrio = 5, @nom_archv = 'boleta.pdf',
           @tpo_cntnd = 'application/pdf', @num_tamno = 1200, @bin_cntnd = 0x255044462D;
      -- Retorna un SELECT con 'cod_resultado' (1 si se guardo, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_aggar_cmprb
    @cod_gasto int,
    @cod_usrio int,
    @nom_archv varchar(255),
    @tpo_cntnd varchar(50),
    @num_tamno int,
    @bin_cntnd varbinary(max)
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_estdo int;

    begin try
        select @cod_estdo = r.cod_estdo_rndcn
        from dbo.ta_rgs_gasto g
        inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
        where g.cod_gasto = @cod_gasto and r.cod_usrio = @cod_usrio;

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'El gasto no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 1
        begin
            select -1 as cod_resultado, 'Solo se puede adjuntar a una rendición en borrador' as dsc_mensaje;
            return;
        end;

        if isnull(@nom_archv, '') = '' or isnull(@num_tamno, 0) <= 0 or @bin_cntnd is null
        begin
            select -1 as cod_resultado, 'El comprobante es obligatorio' as dsc_mensaje;
            return;
        end;

        begin transaction;

        delete from dbo.ta_rgs_gasto_cmprb where cod_gasto = @cod_gasto;
        insert into dbo.ta_rgs_gasto_cmprb (cod_gasto, bin_cntnd) values (@cod_gasto, @bin_cntnd);

        update dbo.ta_rgs_gasto
        set nom_archv = @nom_archv, tpo_cntnd = @tpo_cntnd, num_tamno = @num_tamno,
            cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_gasto = @cod_gasto;

        commit transaction;

        select 1 as cod_resultado, 'Comprobante guardado' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_del_cmprb
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Quita el comprobante de un gasto de una rendicion en borrador.

    Ejemplo:
      exec dbo.pa_rgs_del_cmprb @cod_gasto = 1, @cod_usrio = 5;
      -- Retorna un SELECT con 'cod_resultado' (1 si se quito, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_del_cmprb
    @cod_gasto int,
    @cod_usrio int
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_estdo int;

    begin try
        select @cod_estdo = r.cod_estdo_rndcn
        from dbo.ta_rgs_gasto g
        inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
        where g.cod_gasto = @cod_gasto and r.cod_usrio = @cod_usrio;

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'El gasto no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 1
        begin
            select -1 as cod_resultado, 'Solo se puede quitar de una rendición en borrador' as dsc_mensaje;
            return;
        end;

        begin transaction;

        delete from dbo.ta_rgs_gasto_cmprb where cod_gasto = @cod_gasto;
        update dbo.ta_rgs_gasto
        set nom_archv = null, tpo_cntnd = null, num_tamno = null, cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_gasto = @cod_gasto;

        commit transaction;

        select 1 as cod_resultado, 'Comprobante quitado' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_sel_cmprb
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Devuelve el comprobante de un gasto. Lo ve su dueño, su aprobador
                 (cuando la rendicion ya fue enviada) o un administrador.

    Ejemplo:
      exec dbo.pa_rgs_sel_cmprb @cod_gasto = 1, @cod_usrio_cnslt = 2;
      -- Retorna un registro, o ninguno si no existe o no tiene acceso
*/
create or alter procedure dbo.pa_rgs_sel_cmprb
    @cod_gasto       int,
    @cod_usrio_cnslt int
as
begin
    set nocount on;
    set dateformat dmy;

    select g.nom_archv, g.tpo_cntnd, b.bin_cntnd
    from dbo.ta_rgs_gasto g
    inner join dbo.ta_rgs_gasto_cmprb b on b.cod_gasto = g.cod_gasto
    inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
    inner join dbo.ta_rgs_mst_mov_usrio d on d.cod_usrio = r.cod_usrio
    inner join dbo.ta_rgs_mst_mov_usrio c on c.cod_usrio = @cod_usrio_cnslt and c.flg_actvo = 'S'
    where g.cod_gasto = @cod_gasto
      and (r.cod_usrio = c.cod_usrio
           or c.cod_rol = 3
           or (c.cod_rol = 2 and d.cod_aprbdr = c.cod_usrio and r.cod_estdo_rndcn <> 1));
end;
go
