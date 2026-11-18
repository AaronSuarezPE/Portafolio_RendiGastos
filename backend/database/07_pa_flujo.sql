/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_envr_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Envia una rendicion a aprobacion (Borrador -> Enviada) y avisa al aprobador.
                 Reglas: la persona debe tener un aprobador activo; la rendicion debe tener al menos
                 un gasto y todos los gastos de categorias que exigen comprobante deben tenerlo.

    Ejemplo:
      exec dbo.pa_rgs_envr_rndcn @cod_rndcn = 1, @cod_usrio = 5;
      -- Retorna un SELECT con 'cod_resultado' (codigo del aprobador avisado o -1) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_envr_rndcn
    @cod_rndcn int,
    @cod_usrio int
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_estdo int;
    declare @cod_aprbdr int;
    declare @nom_ttlo varchar(100);
    declare @nom_usrio varchar(100);
    declare @mnt_ttal decimal(12,2);
    declare @cod_hstrl int;
    declare @cod_avso int;

    begin try
        select @cod_estdo = r.cod_estdo_rndcn, @nom_ttlo = r.nom_ttlo, @mnt_ttal = r.mnt_ttal,
               @cod_aprbdr = u.cod_aprbdr, @nom_usrio = u.nom_usrio
        from dbo.ta_rgs_rndcn r
        inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio
        where r.cod_rndcn = @cod_rndcn and r.cod_usrio = @cod_usrio;

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'La rendición no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 1
        begin
            select -1 as cod_resultado, 'Solo se puede enviar una rendición en borrador' as dsc_mensaje;
            return;
        end;

        if @cod_aprbdr is null
           or not exists (select 1 from dbo.ta_rgs_mst_mov_usrio where cod_usrio = @cod_aprbdr and flg_actvo = 'S' and cod_rol in (2, 3))
        begin
            select -1 as cod_resultado, 'No tienes un aprobador asignado. Pídele al administrador que lo asigne' as dsc_mensaje;
            return;
        end;

        if not exists (select 1 from dbo.ta_rgs_gasto where cod_rndcn = @cod_rndcn)
        begin
            select -1 as cod_resultado, 'Agrega al menos un gasto antes de enviar' as dsc_mensaje;
            return;
        end;

        if exists (
            select 1 from dbo.ta_rgs_gasto g
            inner join dbo.ta_rgs_mst_mov_ctgra c on c.cod_ctgra = g.cod_ctgra
            where g.cod_rndcn = @cod_rndcn and c.flg_cmprb_oblgt = 'S' and g.nom_archv is null
        )
        begin
            select -1 as cod_resultado, 'Faltan comprobantes en gastos de categorías que lo exigen' as dsc_mensaje;
            return;
        end;

        begin transaction;

        select @cod_hstrl = isnull(max(cod_hstrl), 0) + 1 from dbo.ta_rgs_hstrl with (updlock, holdlock);
        select @cod_avso = isnull(max(cod_avso), 0) + 1 from dbo.ta_rgs_avso with (updlock, holdlock);

        update dbo.ta_rgs_rndcn
        set cod_estdo_rndcn = 2, fec_envio = getdate(), dsc_mtvo_rchzo = null,
            cod_usrio_rsltr = null, fec_rsln = null,
            cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_rndcn = @cod_rndcn;

        insert into dbo.ta_rgs_hstrl
            (cod_hstrl, cod_rndcn, cod_accn_hstrl, cod_usrio, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values (@cod_hstrl, @cod_rndcn, 2, @cod_usrio, getdate(), @cod_usrio, 1);

        insert into dbo.ta_rgs_avso
            (cod_avso, cod_usrio, cod_rndcn, nom_ttlo, dsc_mnsje, flg_leido, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values
            (@cod_avso, @cod_aprbdr, @cod_rndcn, 'Rendición por aprobar',
             left(@nom_usrio + ' envió «' + @nom_ttlo + '» por ' + convert(varchar(20), @mnt_ttal), 300),
             'N', getdate(), @cod_usrio, 1);

        commit transaction;

        select @cod_aprbdr as cod_resultado, 'Rendición enviada' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_aprbr_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Aprueba una rendicion enviada (Enviada -> Aprobada) y avisa a su dueño.
                 Solo puede hacerlo su aprobador asignado o un administrador, y nunca sobre
                 una rendicion propia.

    Ejemplo:
      exec dbo.pa_rgs_aprbr_rndcn @cod_rndcn = 1, @cod_usrio_aprbdr = 2, @dsc_cmntr = 'Conforme';
      -- Retorna un SELECT con 'cod_resultado' (codigo del dueño avisado o -1) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_aprbr_rndcn
    @cod_rndcn        int,
    @cod_usrio_aprbdr int,
    @dsc_cmntr        varchar(500) = null
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_estdo int;
    declare @cod_dueno int;
    declare @cod_aprbdr_asgnd int;
    declare @cod_rol int;
    declare @nom_ttlo varchar(100);
    declare @nom_aprbdr varchar(100);
    declare @cod_hstrl int;
    declare @cod_avso int;

    begin try
        set @dsc_cmntr = nullif(ltrim(rtrim(isnull(@dsc_cmntr, ''))), '');

        select @cod_estdo = r.cod_estdo_rndcn, @cod_dueno = r.cod_usrio, @nom_ttlo = r.nom_ttlo,
               @cod_aprbdr_asgnd = u.cod_aprbdr
        from dbo.ta_rgs_rndcn r
        inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio
        where r.cod_rndcn = @cod_rndcn;

        select @cod_rol = cod_rol, @nom_aprbdr = nom_usrio
        from dbo.ta_rgs_mst_mov_usrio where cod_usrio = @cod_usrio_aprbdr and flg_actvo = 'S';

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'La rendición no existe' as dsc_mensaje;
            return;
        end;

        if @cod_rol is null or @cod_rol not in (2, 3)
        begin
            select -1 as cod_resultado, 'No tienes permiso para aprobar rendiciones' as dsc_mensaje;
            return;
        end;

        if @cod_dueno = @cod_usrio_aprbdr
        begin
            select -1 as cod_resultado, 'No puedes aprobar tu propia rendición' as dsc_mensaje;
            return;
        end;

        if @cod_rol = 2 and isnull(@cod_aprbdr_asgnd, 0) <> @cod_usrio_aprbdr
        begin
            select -1 as cod_resultado, 'Esta rendición no está a tu cargo' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 2
        begin
            select -1 as cod_resultado, 'Solo se puede aprobar una rendición enviada' as dsc_mensaje;
            return;
        end;

        begin transaction;

        select @cod_hstrl = isnull(max(cod_hstrl), 0) + 1 from dbo.ta_rgs_hstrl with (updlock, holdlock);
        select @cod_avso = isnull(max(cod_avso), 0) + 1 from dbo.ta_rgs_avso with (updlock, holdlock);

        update dbo.ta_rgs_rndcn
        set cod_estdo_rndcn = 3, cod_usrio_rsltr = @cod_usrio_aprbdr, fec_rsln = getdate(),
            cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio_aprbdr
        where cod_rndcn = @cod_rndcn;

        insert into dbo.ta_rgs_hstrl
            (cod_hstrl, cod_rndcn, cod_accn_hstrl, cod_usrio, dsc_cmntr, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values (@cod_hstrl, @cod_rndcn, 3, @cod_usrio_aprbdr, @dsc_cmntr, getdate(), @cod_usrio_aprbdr, 1);

        insert into dbo.ta_rgs_avso
            (cod_avso, cod_usrio, cod_rndcn, nom_ttlo, dsc_mnsje, flg_leido, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values
            (@cod_avso, @cod_dueno, @cod_rndcn, 'Rendición aprobada',
             left(@nom_aprbdr + ' aprobó «' + @nom_ttlo + '»', 300),
             'N', getdate(), @cod_usrio_aprbdr, 1);

        commit transaction;

        select @cod_dueno as cod_resultado, 'Rendición aprobada' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_rchzr_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Rechaza una rendicion enviada (Enviada -> Rechazada). El motivo es obligatorio
                 (minimo 10 caracteres). Mismas reglas de permiso que al aprobar.

    Ejemplo:
      exec dbo.pa_rgs_rchzr_rndcn @cod_rndcn = 1, @cod_usrio_aprbdr = 2, @dsc_mtvo = 'Falta la boleta del taxi';
      -- Retorna un SELECT con 'cod_resultado' (codigo del dueño avisado o -1) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_rchzr_rndcn
    @cod_rndcn        int,
    @cod_usrio_aprbdr int,
    @dsc_mtvo         varchar(500)
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_estdo int;
    declare @cod_dueno int;
    declare @cod_aprbdr_asgnd int;
    declare @cod_rol int;
    declare @nom_ttlo varchar(100);
    declare @nom_aprbdr varchar(100);
    declare @cod_hstrl int;
    declare @cod_avso int;

    begin try
        set @dsc_mtvo = ltrim(rtrim(isnull(@dsc_mtvo, '')));

        select @cod_estdo = r.cod_estdo_rndcn, @cod_dueno = r.cod_usrio, @nom_ttlo = r.nom_ttlo,
               @cod_aprbdr_asgnd = u.cod_aprbdr
        from dbo.ta_rgs_rndcn r
        inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio
        where r.cod_rndcn = @cod_rndcn;

        select @cod_rol = cod_rol, @nom_aprbdr = nom_usrio
        from dbo.ta_rgs_mst_mov_usrio where cod_usrio = @cod_usrio_aprbdr and flg_actvo = 'S';

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'La rendición no existe' as dsc_mensaje;
            return;
        end;

        if @cod_rol is null or @cod_rol not in (2, 3)
        begin
            select -1 as cod_resultado, 'No tienes permiso para rechazar rendiciones' as dsc_mensaje;
            return;
        end;

        if @cod_dueno = @cod_usrio_aprbdr
        begin
            select -1 as cod_resultado, 'No puedes rechazar tu propia rendición' as dsc_mensaje;
            return;
        end;

        if @cod_rol = 2 and isnull(@cod_aprbdr_asgnd, 0) <> @cod_usrio_aprbdr
        begin
            select -1 as cod_resultado, 'Esta rendición no está a tu cargo' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 2
        begin
            select -1 as cod_resultado, 'Solo se puede rechazar una rendición enviada' as dsc_mensaje;
            return;
        end;

        if len(@dsc_mtvo) < 10
        begin
            select -1 as cod_resultado, 'Explica el motivo del rechazo (mínimo 10 caracteres)' as dsc_mensaje;
            return;
        end;

        begin transaction;

        select @cod_hstrl = isnull(max(cod_hstrl), 0) + 1 from dbo.ta_rgs_hstrl with (updlock, holdlock);
        select @cod_avso = isnull(max(cod_avso), 0) + 1 from dbo.ta_rgs_avso with (updlock, holdlock);

        update dbo.ta_rgs_rndcn
        set cod_estdo_rndcn = 4, dsc_mtvo_rchzo = @dsc_mtvo, cod_usrio_rsltr = @cod_usrio_aprbdr,
            fec_rsln = getdate(), cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio_aprbdr
        where cod_rndcn = @cod_rndcn;

        insert into dbo.ta_rgs_hstrl
            (cod_hstrl, cod_rndcn, cod_accn_hstrl, cod_usrio, dsc_cmntr, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values (@cod_hstrl, @cod_rndcn, 4, @cod_usrio_aprbdr, @dsc_mtvo, getdate(), @cod_usrio_aprbdr, 1);

        insert into dbo.ta_rgs_avso
            (cod_avso, cod_usrio, cod_rndcn, nom_ttlo, dsc_mnsje, flg_leido, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values
            (@cod_avso, @cod_dueno, @cod_rndcn, 'Rendición rechazada',
             left(@nom_aprbdr + ' rechazó «' + @nom_ttlo + '»: ' + @dsc_mtvo, 300),
             'N', getdate(), @cod_usrio_aprbdr, 1);

        commit transaction;

        select @cod_dueno as cod_resultado, 'Rendición rechazada' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_rabrr_rndcn
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Devuelve a borrador una rendicion rechazada para corregirla (Rechazada -> Borrador).
                 Solo su dueño. El motivo del rechazo queda en el historial.

    Ejemplo:
      exec dbo.pa_rgs_rabrr_rndcn @cod_rndcn = 1, @cod_usrio = 5;
      -- Retorna un SELECT con 'cod_resultado' (1 si se reabrio, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_rabrr_rndcn
    @cod_rndcn int,
    @cod_usrio int
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_estdo int;
    declare @cod_hstrl int;

    begin try
        select @cod_estdo = cod_estdo_rndcn from dbo.ta_rgs_rndcn where cod_rndcn = @cod_rndcn and cod_usrio = @cod_usrio;

        if @cod_estdo is null
        begin
            select -1 as cod_resultado, 'La rendición no existe' as dsc_mensaje;
            return;
        end;

        if @cod_estdo <> 4
        begin
            select -1 as cod_resultado, 'Solo se puede reabrir una rendición rechazada' as dsc_mensaje;
            return;
        end;

        if (select count(*) from dbo.ta_rgs_rndcn where cod_usrio = @cod_usrio and cod_estdo_rndcn = 1) >= 10
        begin
            select -1 as cod_resultado, 'Ya tienes 10 borradores. Envía o elimina alguno antes de reabrir' as dsc_mensaje;
            return;
        end;

        begin transaction;

        select @cod_hstrl = isnull(max(cod_hstrl), 0) + 1 from dbo.ta_rgs_hstrl with (updlock, holdlock);

        update dbo.ta_rgs_rndcn
        set cod_estdo_rndcn = 1, dsc_mtvo_rchzo = null, cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
        where cod_rndcn = @cod_rndcn;

        insert into dbo.ta_rgs_hstrl
            (cod_hstrl, cod_rndcn, cod_accn_hstrl, cod_usrio, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
        values (@cod_hstrl, @cod_rndcn, 5, @cod_usrio, getdate(), @cod_usrio, 1);

        commit transaction;

        select 1 as cod_resultado, 'Rendición reabierta' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go
