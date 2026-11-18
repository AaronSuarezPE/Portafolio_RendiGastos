/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_lst_avso
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Lista los ultimos 50 avisos de una persona. Con @flg_slo_nleidos = 'S'
                 devuelve solo los que no leyo.

    Ejemplo:
      exec dbo.pa_rgs_lst_avso @cod_usrio = 5, @flg_slo_nleidos = 'S';
      -- Retorna una lista de avisos
*/
create or alter procedure dbo.pa_rgs_lst_avso
    @cod_usrio       int,
    @flg_slo_nleidos char(1) = 'N'
as
begin
    set nocount on;
    set dateformat dmy;

    select top 50
        cod_avso, isnull(cod_rndcn, 0) as cod_rndcn, nom_ttlo, dsc_mnsje, flg_leido,
        convert(varchar(10), cnr_fec_rgtro, 103) as fec_avso,
        convert(varchar(5), cnr_fec_rgtro, 108) as hor_avso
    from dbo.ta_rgs_avso
    where cod_usrio = @cod_usrio and (@flg_slo_nleidos <> 'S' or flg_leido = 'N')
    order by cod_avso desc;
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_cnt_avso
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Cuenta los avisos que una persona todavia no leyo.

    Ejemplo:
      exec dbo.pa_rgs_cnt_avso @cod_usrio = 5;
      -- Retorna un registro con 'num_nleidos'
*/
create or alter procedure dbo.pa_rgs_cnt_avso
    @cod_usrio int
as
begin
    set nocount on;

    select count(*) as num_nleidos from dbo.ta_rgs_avso where cod_usrio = @cod_usrio and flg_leido = 'N';
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_edtar_avso
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Marca como leido un aviso de la persona, o todos si @cod_avso es nulo.

    Ejemplo:
      exec dbo.pa_rgs_edtar_avso @cod_usrio = 5, @cod_avso = null;
      -- Retorna un SELECT con 'cod_resultado' (cantidad marcada) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_edtar_avso
    @cod_usrio int,
    @cod_avso  int = null
as
begin
    set nocount on;

    update dbo.ta_rgs_avso
    set flg_leido = 'S', cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
    where cod_usrio = @cod_usrio and flg_leido = 'N' and (@cod_avso is null or cod_avso = @cod_avso);

    select @@rowcount as cod_resultado, 'Avisos marcados como leídos' as dsc_mensaje;
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_aggar_sbscr
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Registra el dispositivo de una persona para recibir avisos push.
                 Si el dispositivo ya estaba registrado, pasa a la persona actual.

    Ejemplo:
      exec dbo.pa_rgs_aggar_sbscr @cod_usrio = 5, @nom_url_ptnto = 'https://...', @nom_llave_p256 = 'k', @nom_llave_auth = 'a';
      -- Retorna un SELECT con 'cod_resultado' (codigo de la suscripcion o -1) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_aggar_sbscr
    @cod_usrio      int,
    @nom_url_ptnto  varchar(450),
    @nom_llave_p256 varchar(200),
    @nom_llave_auth varchar(100)
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_sbscr int;

    begin try
        if isnull(@nom_url_ptnto, '') = '' or isnull(@nom_llave_p256, '') = '' or isnull(@nom_llave_auth, '') = ''
        begin
            select -1 as cod_resultado, 'La suscripción está incompleta' as dsc_mensaje;
            return;
        end;

        begin transaction;

        select @cod_sbscr = cod_sbscr from dbo.ta_rgs_sbscr_push with (updlock, holdlock) where nom_url_ptnto = @nom_url_ptnto;

        if @cod_sbscr is null
        begin
            select @cod_sbscr = isnull(max(cod_sbscr), 0) + 1 from dbo.ta_rgs_sbscr_push with (updlock, holdlock);

            insert into dbo.ta_rgs_sbscr_push
                (cod_sbscr, cod_usrio, nom_url_ptnto, nom_llave_p256, nom_llave_auth, cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
            values
                (@cod_sbscr, @cod_usrio, @nom_url_ptnto, @nom_llave_p256, @nom_llave_auth, getdate(), @cod_usrio, 1);
        end
        else
            update dbo.ta_rgs_sbscr_push
            set cod_usrio = @cod_usrio, nom_llave_p256 = @nom_llave_p256, nom_llave_auth = @nom_llave_auth,
                cnr_fec_mdfcn = getdate(), cnr_usr_mdfcn = @cod_usrio
            where cod_sbscr = @cod_sbscr;

        commit transaction;

        select @cod_sbscr as cod_resultado, 'Avisos activados en este dispositivo' as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_lst_sbscr
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Lista los dispositivos registrados de una persona para enviarles un aviso push.

    Ejemplo:
      exec dbo.pa_rgs_lst_sbscr @cod_usrio = 5;
      -- Retorna una lista de suscripciones
*/
create or alter procedure dbo.pa_rgs_lst_sbscr
    @cod_usrio int
as
begin
    set nocount on;

    select cod_sbscr, nom_url_ptnto, nom_llave_p256, nom_llave_auth
    from dbo.ta_rgs_sbscr_push
    where cod_usrio = @cod_usrio
    order by cod_sbscr;
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_del_sbscr
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Elimina un dispositivo registrado (la persona desactiva los avisos
                 o el servicio push informa que ya no existe).

    Ejemplo:
      exec dbo.pa_rgs_del_sbscr @nom_url_ptnto = 'https://...';
      -- Retorna un SELECT con 'cod_resultado' (cantidad eliminada) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_del_sbscr
    @nom_url_ptnto varchar(450)
as
begin
    set nocount on;

    delete from dbo.ta_rgs_sbscr_push where nom_url_ptnto = @nom_url_ptnto;

    select @@rowcount as cod_resultado, 'Dispositivo eliminado' as dsc_mensaje;
end;
go
