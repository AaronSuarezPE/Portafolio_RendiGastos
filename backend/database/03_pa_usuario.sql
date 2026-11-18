/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_aggar_usrio
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Registra un usuario con su rol y, si corresponde, su aprobador.
                 El aprobador debe estar activo y ser Aprobador o Administrador.

    Ejemplo:
      exec dbo.pa_rgs_aggar_usrio @nom_usrio = 'Ana', @nom_email = 'ana@correo.com',
           @nom_clave_hsh = 'hash', @cod_rol = 1, @cod_aprbdr = 2, @cod_usrio_accn = 3;
      -- Retorna un SELECT con 'cod_resultado' (codigo nuevo o -1) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_aggar_usrio
    @nom_usrio      varchar(100),
    @nom_email      varchar(150),
    @nom_clave_hsh  varchar(200),
    @cod_rol        int,
    @cod_aprbdr     int = null,
    @cod_usrio_accn int = 0
as
begin
    set nocount on;
    set dateformat dmy;

    declare @cod_usrio int;
    declare @dsc_mensaje varchar(200);

    begin try
        set @nom_usrio = ltrim(rtrim(isnull(@nom_usrio, '')));
        set @nom_email = lower(ltrim(rtrim(isnull(@nom_email, ''))));

        if len(@nom_usrio) < 2
        begin
            select -1 as cod_resultado, 'El nombre debe tener al menos 2 caracteres' as dsc_mensaje;
            return;
        end;

        if @nom_email = '' or @nom_email not like '%_@_%._%'
        begin
            select -1 as cod_resultado, 'Escribe un correo válido' as dsc_mensaje;
            return;
        end;

        if isnull(@nom_clave_hsh, '') = ''
        begin
            select -1 as cod_resultado, 'La clave es obligatoria' as dsc_mensaje;
            return;
        end;

        if not exists (select 1 from dbo.ta_rgs_mst_rol where cod_rol = @cod_rol)
        begin
            select -1 as cod_resultado, 'El rol no existe' as dsc_mensaje;
            return;
        end;

        if @cod_aprbdr is not null
           and not exists (select 1 from dbo.ta_rgs_mst_mov_usrio
                           where cod_usrio = @cod_aprbdr and flg_actvo = 'S' and cod_rol in (2, 3))
        begin
            select -1 as cod_resultado, 'El aprobador debe ser un Aprobador o Administrador activo' as dsc_mensaje;
            return;
        end;

        begin transaction;

        if exists (select 1 from dbo.ta_rgs_mst_mov_usrio with (updlock, holdlock) where nom_email = @nom_email)
        begin
            set @cod_usrio = -1;
            set @dsc_mensaje = 'El correo ya está registrado';
        end
        else
        begin
            select @cod_usrio = isnull(max(cod_usrio), 0) + 1
            from dbo.ta_rgs_mst_mov_usrio with (updlock, holdlock);

            insert into dbo.ta_rgs_mst_mov_usrio
                (cod_usrio, nom_usrio, nom_email, nom_clave_hsh, cod_rol, cod_aprbdr, flg_actvo,
                 cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
            values
                (@cod_usrio, @nom_usrio, @nom_email, @nom_clave_hsh, @cod_rol, @cod_aprbdr, 'S',
                 getdate(), isnull(@cod_usrio_accn, 0), 1);

            set @dsc_mensaje = 'Usuario registrado';
        end;

        commit transaction;

        select @cod_usrio as cod_resultado, @dsc_mensaje as dsc_mensaje;
    end try
    begin catch
        if @@trancount > 0 rollback transaction;
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_sel_usrio
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Devuelve un usuario con su rol y aprobador, buscado por codigo o por correo.

    Ejemplo:
      exec dbo.pa_rgs_sel_usrio @nom_email = 'ana@correo.com';
      -- Retorna un registro, o ninguno si no existe
*/
create or alter procedure dbo.pa_rgs_sel_usrio
    @cod_usrio int = null,
    @nom_email varchar(150) = null
as
begin
    set nocount on;
    set dateformat dmy;

    set @nom_email = lower(ltrim(rtrim(@nom_email)));

    select
        u.cod_usrio, u.nom_usrio, u.nom_email, u.nom_clave_hsh,
        u.cod_rol, r.nom_rol,
        u.cod_aprbdr, a.nom_usrio as nom_aprbdr,
        u.flg_actvo
    from dbo.ta_rgs_mst_mov_usrio u
    inner join dbo.ta_rgs_mst_rol r on r.cod_rol = u.cod_rol
    left join dbo.ta_rgs_mst_mov_usrio a on a.cod_usrio = u.cod_aprbdr
    where (@cod_usrio is not null and u.cod_usrio = @cod_usrio)
       or (@cod_usrio is null and @nom_email is not null and u.nom_email = @nom_email);
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_lst_usrio
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Lista los usuarios para la pantalla de administracion.
                 Si @flg_slo_aprbdr es 'S' devuelve solo quienes pueden aprobar (combo de aprobador).

    Ejemplo:
      exec dbo.pa_rgs_lst_usrio @flg_slo_aprbdr = 'S';
      -- Retorna una lista de usuarios (sin la clave)
*/
create or alter procedure dbo.pa_rgs_lst_usrio
    @flg_slo_aprbdr char(1) = 'N'
as
begin
    set nocount on;
    set dateformat dmy;

    select
        u.cod_usrio, u.nom_usrio, u.nom_email,
        u.cod_rol, r.nom_rol,
        u.cod_aprbdr, a.nom_usrio as nom_aprbdr,
        u.flg_actvo
    from dbo.ta_rgs_mst_mov_usrio u
    inner join dbo.ta_rgs_mst_rol r on r.cod_rol = u.cod_rol
    left join dbo.ta_rgs_mst_mov_usrio a on a.cod_usrio = u.cod_aprbdr
    where @flg_slo_aprbdr <> 'S' or (u.cod_rol in (2, 3) and u.flg_actvo = 'S')
    order by u.nom_usrio;
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_edtar_usrio
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Modifica nombre, rol, aprobador y estado de un usuario.
                 Reglas: nadie se desactiva ni se quita el rol de administrador a si mismo;
                 siempre debe quedar un administrador activo; quien tiene personas a su cargo
                 no puede dejar de ser Aprobador ni desactivarse.

    Ejemplo:
      exec dbo.pa_rgs_edtar_usrio @cod_usrio = 5, @nom_usrio = 'Ana', @cod_rol = 1,
           @cod_aprbdr = 2, @flg_actvo = 'S', @cod_usrio_accn = 3;
      -- Retorna un SELECT con 'cod_resultado' (1 si se guardo, -1 si no) y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_edtar_usrio
    @cod_usrio      int,
    @nom_usrio      varchar(100),
    @cod_rol        int,
    @cod_aprbdr     int = null,
    @flg_actvo      char(1),
    @cod_usrio_accn int
as
begin
    set nocount on;
    set dateformat dmy;

    begin try
        set @nom_usrio = ltrim(rtrim(isnull(@nom_usrio, '')));

        if not exists (select 1 from dbo.ta_rgs_mst_mov_usrio where cod_usrio = @cod_usrio)
        begin
            select -1 as cod_resultado, 'El usuario no existe' as dsc_mensaje;
            return;
        end;

        if len(@nom_usrio) < 2
        begin
            select -1 as cod_resultado, 'El nombre debe tener al menos 2 caracteres' as dsc_mensaje;
            return;
        end;

        if not exists (select 1 from dbo.ta_rgs_mst_rol where cod_rol = @cod_rol)
        begin
            select -1 as cod_resultado, 'El rol no existe' as dsc_mensaje;
            return;
        end;

        if @flg_actvo not in ('S', 'N')
        begin
            select -1 as cod_resultado, 'El estado no es válido' as dsc_mensaje;
            return;
        end;

        if @cod_aprbdr is not null and @cod_aprbdr = @cod_usrio
        begin
            select -1 as cod_resultado, 'Nadie puede ser su propio aprobador' as dsc_mensaje;
            return;
        end;

        if @cod_aprbdr is not null
           and not exists (select 1 from dbo.ta_rgs_mst_mov_usrio
                           where cod_usrio = @cod_aprbdr and flg_actvo = 'S' and cod_rol in (2, 3))
        begin
            select -1 as cod_resultado, 'El aprobador debe ser un Aprobador o Administrador activo' as dsc_mensaje;
            return;
        end;

        if @cod_usrio = @cod_usrio_accn and (@flg_actvo = 'N' or @cod_rol <> 3)
           and exists (select 1 from dbo.ta_rgs_mst_mov_usrio where cod_usrio = @cod_usrio and cod_rol = 3)
        begin
            select -1 as cod_resultado, 'No puedes desactivarte ni quitarte el rol de administrador a ti mismo' as dsc_mensaje;
            return;
        end;

        if (@flg_actvo = 'N' or @cod_rol = 1)
           and exists (select 1 from dbo.ta_rgs_mst_mov_usrio where cod_aprbdr = @cod_usrio and flg_actvo = 'S')
        begin
            select -1 as cod_resultado, 'Tiene personas a su cargo: asígnales otro aprobador antes de este cambio' as dsc_mensaje;
            return;
        end;

        if (@flg_actvo = 'N' or @cod_rol <> 3)
           and exists (select 1 from dbo.ta_rgs_mst_mov_usrio where cod_usrio = @cod_usrio and cod_rol = 3 and flg_actvo = 'S')
           and not exists (select 1 from dbo.ta_rgs_mst_mov_usrio
                           where cod_usrio <> @cod_usrio and cod_rol = 3 and flg_actvo = 'S')
        begin
            select -1 as cod_resultado, 'Debe quedar al menos un administrador activo' as dsc_mensaje;
            return;
        end;

        update dbo.ta_rgs_mst_mov_usrio
        set nom_usrio = @nom_usrio,
            cod_rol = @cod_rol,
            cod_aprbdr = @cod_aprbdr,
            flg_actvo = @flg_actvo,
            cnr_fec_mdfcn = getdate(),
            cnr_usr_mdfcn = @cod_usrio_accn
        where cod_usrio = @cod_usrio;

        select 1 as cod_resultado, 'Usuario actualizado' as dsc_mensaje;
    end try
    begin catch
        select -1 as cod_resultado, error_message() as dsc_mensaje;
    end catch
end;
go

/*
    Procedimiento Almacenado desarrollado por Aaron Suarez
    Nombre: pa_rgs_edtar_login
    Autor: Aaron Suarez
    Fecha Creacion: 17/11/2026
    Fecha ultima actualizacion:
    Nombre ultima persona que actualizo:
    Base de datos: rendigastos

    Descripcion: Registra la fecha del ultimo ingreso de un usuario.

    Ejemplo:
      exec dbo.pa_rgs_edtar_login @cod_usrio = 5;
      -- Retorna un SELECT con 'cod_resultado' y 'dsc_mensaje'
*/
create or alter procedure dbo.pa_rgs_edtar_login
    @cod_usrio int
as
begin
    set nocount on;

    update dbo.ta_rgs_mst_mov_usrio set fec_ultmo_login = getdate() where cod_usrio = @cod_usrio;

    select 1 as cod_resultado, 'Ingreso registrado' as dsc_mensaje;
end;
go
