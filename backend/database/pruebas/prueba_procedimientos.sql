/*
    RendiGastos - pruebas de los procedimientos almacenados
    Base de datos: rendigastos

    Ejecuta cada procedimiento con casos correctos y con errores.
    Si una verificacion falla se lanza un error con el nombre de la prueba.
    Los datos de prueba usan el prefijo 'zz_prueba' y se eliminan al terminar.
*/
set nocount on;
set dateformat dmy;

declare @sql_limpia nvarchar(max) = N'
    delete a from dbo.ta_rgs_avso a inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = a.cod_usrio where u.nom_usrio like ''zz_prueba%'';
    delete s from dbo.ta_rgs_sbscr_push s inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = s.cod_usrio where u.nom_usrio like ''zz_prueba%'';
    delete h from dbo.ta_rgs_hstrl h inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = h.cod_rndcn
        inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio where u.nom_usrio like ''zz_prueba%'';
    delete c from dbo.ta_rgs_gasto_cmprb c inner join dbo.ta_rgs_gasto g on g.cod_gasto = c.cod_gasto
        inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
        inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio where u.nom_usrio like ''zz_prueba%'';
    delete g from dbo.ta_rgs_gasto g inner join dbo.ta_rgs_rndcn r on r.cod_rndcn = g.cod_rndcn
        inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio where u.nom_usrio like ''zz_prueba%'';
    delete r from dbo.ta_rgs_rndcn r inner join dbo.ta_rgs_mst_mov_usrio u on u.cod_usrio = r.cod_usrio where u.nom_usrio like ''zz_prueba%'';
    update dbo.ta_rgs_mst_mov_usrio set cod_aprbdr = null where nom_usrio like ''zz_prueba%'';
    delete from dbo.ta_rgs_mst_mov_usrio where nom_usrio like ''zz_prueba%'';
    delete from dbo.ta_rgs_mst_mov_ctgra where nom_ctgra like ''zz_prueba%'';';

exec sp_executesql @sql_limpia;

declare @r table (cod_resultado int, dsc_mensaje varchar(500));
declare @cod int, @n int, @msj varchar(500);
declare @adm int, @apr int, @apr2 int, @ana int, @beto int, @sin_apr int;
declare @ctg_cmprb int, @ctg_libre int;
declare @rnd int, @rnd2 int, @g1 int, @g2 int, @g3 int;
declare @hoy varchar(10) = convert(varchar(10), getdate(), 103);
declare @ayer varchar(10) = convert(varchar(10), dateadd(day, -1, getdate()), 103);
declare @manana varchar(10) = convert(varchar(10), dateadd(day, 1, getdate()), 103);
declare @viejo varchar(10) = convert(varchar(10), dateadd(day, -91, getdate()), 103);
declare @bin varbinary(max) = 0x255044462D312E34;

begin try

    -- usuarios -----------------------------------------------------------------
    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba admin', 'zz_admin@prueba.com', 'h', 3, null, 0;
    select @adm = cod_resultado from @r; delete from @r;
    if @adm < 1 throw 50001, 'aggar_usrio: no registró al administrador', 1;

    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba aprobador', 'zz_apr@prueba.com', 'h', 2, null, @adm;
    select @apr = cod_resultado from @r; delete from @r;
    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba aprobador dos', 'zz_apr2@prueba.com', 'h', 2, null, @adm;
    select @apr2 = cod_resultado from @r; delete from @r;
    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba ana', 'zz_ana@prueba.com', 'h', 1, @apr, @adm;
    select @ana = cod_resultado from @r; delete from @r;
    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba beto', 'zz_beto@prueba.com', 'h', 1, @apr, @adm;
    select @beto = cod_resultado from @r; delete from @r;
    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba sin aprobador', 'zz_sin@prueba.com', 'h', 1, null, @adm;
    select @sin_apr = cod_resultado from @r; delete from @r;
    if @apr < 1 or @apr2 < 1 or @ana < 1 or @beto < 1 or @sin_apr < 1 throw 50001, 'aggar_usrio: no registró a los usuarios', 1;

    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba otra', 'ZZ_ANA@prueba.com', 'h', 1, null, @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_usrio: permitió un correo repetido (sin distinguir mayúsculas)', 1;

    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba x', 'correo-malo', 'h', 1, null, @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_usrio: permitió un correo inválido', 1;

    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba x', 'zz_x@prueba.com', '', 1, null, @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_usrio: permitió una clave vacía', 1;

    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba x', 'zz_x@prueba.com', 'h', 9, null, @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_usrio: permitió un rol inexistente', 1;

    insert into @r exec dbo.pa_rgs_aggar_usrio 'zz_prueba x', 'zz_x@prueba.com', 'h', 1, @ana, @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_usrio: permitió como aprobador a un empleado', 1;

    insert into @r exec dbo.pa_rgs_aggar_usrio 'z', 'zz_x@prueba.com', 'h', 1, null, @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_usrio: permitió un nombre de un carácter', 1;

    -- sel_usrio / lst_usrio ------------------------------------------------------
    declare @u table (cod_usrio int, nom_usrio varchar(100), nom_email varchar(150), nom_clave_hsh varchar(200),
                      cod_rol int, nom_rol varchar(50), cod_aprbdr int, nom_aprbdr varchar(100), flg_actvo char(1));
    insert into @u exec dbo.pa_rgs_sel_usrio @nom_email = 'ZZ_Ana@prueba.com';
    select @n = count(*) from @u where cod_usrio = @ana and cod_rol = 1 and nom_rol = 'Empleado' and cod_aprbdr = @apr
        and nom_aprbdr = 'zz_prueba aprobador' and flg_actvo = 'S';
    if @n <> 1 throw 50001, 'sel_usrio: no devolvió al usuario con rol y aprobador', 1;
    delete from @u;
    insert into @u exec dbo.pa_rgs_sel_usrio @cod_usrio = 0;
    if exists (select 1 from @u) throw 50001, 'sel_usrio: devolvió un usuario que no existe', 1;

    declare @l table (cod_usrio int, nom_usrio varchar(100), nom_email varchar(150), cod_rol int, nom_rol varchar(50),
                      cod_aprbdr int, nom_aprbdr varchar(100), flg_actvo char(1));
    insert into @l exec dbo.pa_rgs_lst_usrio 'S';
    if exists (select 1 from @l where cod_usrio in (@ana, @beto)) throw 50001, 'lst_usrio: el combo de aprobadores incluye empleados', 1;
    if not exists (select 1 from @l where cod_usrio = @apr) throw 50001, 'lst_usrio: el combo de aprobadores no incluye al aprobador', 1;
    delete from @l;
    insert into @l exec dbo.pa_rgs_lst_usrio 'N';
    if not exists (select 1 from @l where cod_usrio = @ana) throw 50001, 'lst_usrio: la lista completa no incluye al empleado', 1;

    -- edtar_usrio: reglas --------------------------------------------------------
    insert into @r exec dbo.pa_rgs_edtar_usrio @adm, 'zz_prueba admin', 3, null, 'N', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_usrio: permitió que el administrador se desactive a sí mismo', 1;

    insert into @r exec dbo.pa_rgs_edtar_usrio @adm, 'zz_prueba admin', 1, null, 'S', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_usrio: permitió que el administrador se quite el rol a sí mismo', 1;

    insert into @r exec dbo.pa_rgs_edtar_usrio @apr, 'zz_prueba aprobador', 1, null, 'S', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_usrio: dejó sin rol de aprobador a quien tiene personas a cargo', 1;

    insert into @r exec dbo.pa_rgs_edtar_usrio @apr, 'zz_prueba aprobador', 2, null, 'N', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_usrio: desactivó a un aprobador con personas a cargo', 1;

    insert into @r exec dbo.pa_rgs_edtar_usrio @ana, 'zz_prueba ana', 1, @ana, 'S', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_usrio: permitió ser su propio aprobador', 1;

    insert into @r exec dbo.pa_rgs_edtar_usrio @ana, 'zz_prueba ana', 1, @beto, 'S', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_usrio: permitió como aprobador a un empleado', 1;

    insert into @r exec dbo.pa_rgs_edtar_usrio 0, 'zz_prueba nadie', 1, null, 'S', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_usrio: aceptó un usuario que no existe', 1;

    insert into @r exec dbo.pa_rgs_edtar_usrio @beto, 'zz_prueba beto', 1, @apr2, 'S', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 1 throw 50001, 'edtar_usrio: no cambió el aprobador', 1;
    insert into @r exec dbo.pa_rgs_edtar_usrio @beto, 'zz_prueba beto', 1, @apr, 'S', @adm;
    delete from @r;

    -- categorias -----------------------------------------------------------------
    insert into @r exec dbo.pa_rgs_aggar_ctgra 'zz_prueba con comprobante', 'Exige comprobante', 100, 'S', @adm;
    select @ctg_cmprb = cod_resultado from @r; delete from @r;
    insert into @r exec dbo.pa_rgs_aggar_ctgra 'zz_prueba libre', 'No exige comprobante', 50, 'N', @adm;
    select @ctg_libre = cod_resultado from @r; delete from @r;
    if @ctg_cmprb < 1 or @ctg_libre < 1 throw 50001, 'aggar_ctgra: no registró las categorías', 1;

    insert into @r exec dbo.pa_rgs_aggar_ctgra 'zz_prueba libre', 'repetida', 50, 'N', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_ctgra: permitió un nombre repetido', 1;
    insert into @r exec dbo.pa_rgs_aggar_ctgra 'zz_prueba cero', 'limite cero', 0, 'N', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_ctgra: permitió un límite de cero', 1;
    insert into @r exec dbo.pa_rgs_aggar_ctgra 'zz_prueba flag', 'indicador malo', 10, 'X', @adm;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_ctgra: permitió un indicador inválido', 1;

    declare @c table (cod_ctgra int, nom_ctgra varchar(60), dsc_ctgra varchar(200), mnt_lmte decimal(12,2), flg_cmprb_oblgt char(1), flg_actvo char(1));
    insert into @c exec dbo.pa_rgs_lst_ctgra 'S';
    select @n = count(*) from @c where cod_ctgra in (@ctg_cmprb, @ctg_libre);
    if @n <> 2 throw 50001, 'lst_ctgra: no lista las categorías activas', 1;

    -- rendiciones y gastos: reglas del borrador ---------------------------------
    insert into @r exec dbo.pa_rgs_aggar_rndcn @ana, 'zz viaje a Lima';
    select @rnd = cod_resultado from @r; delete from @r;
    if @rnd < 1 throw 50001, 'aggar_rndcn: no creó la rendición', 1;

    insert into @r exec dbo.pa_rgs_aggar_rndcn @ana, 'ab';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_rndcn: permitió un título corto', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @hoy, 'Taxi al aeropuerto', 40;
    select @g1 = cod_resultado from @r; delete from @r;
    if @g1 < 1 throw 50001, 'aggar_gasto: no agregó un gasto válido', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @hoy, 'Taxi al aeropuerto', 40;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió un gasto repetido', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @manana, 'Gasto futuro', 10;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió una fecha futura', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @viejo, 'Gasto viejo', 10;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió una fecha de hace más de 90 días', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, '31/02/2026', 'Fecha rara', 10;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió una fecha inexistente', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @hoy, 'Supera el límite', 50.01;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió superar el límite de la categoría', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @hoy, 'Justo en el límite', 50;
    select @g3 = cod_resultado from @r; delete from @r;
    if @g3 < 1 throw 50001, 'aggar_gasto: rechazó un monto igual al límite', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @hoy, 'Monto cero', 0;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió un monto de cero', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @hoy, 'Monto negativo', -5;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió un monto negativo', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @hoy, 'ab', 10;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió una descripción corta', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, 9999, @hoy, 'Categoria inexistente', 10;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió una categoría inexistente', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @beto, @ctg_libre, @hoy, 'Ajeno', 10;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: permitió agregar a la rendición de otra persona', 1;

    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_cmprb, @ayer, 'Almuerzo con cliente', 90;
    select @g2 = cod_resultado from @r; delete from @r;
    if @g2 < 1 throw 50001, 'aggar_gasto: no agregó el gasto con comprobante obligatorio', 1;

    -- el total se recalcula
    select @n = count(*) from dbo.ta_rgs_rndcn where cod_rndcn = @rnd and mnt_ttal = 180;
    if @n <> 1 throw 50001, 'aggar_gasto: el total de la rendición no es 180', 1;

    -- edtar_gasto
    insert into @r exec dbo.pa_rgs_edtar_gasto @g1, @ana, @ctg_libre, @hoy, 'Taxi al aeropuerto ida', 30;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 1 throw 50001, 'edtar_gasto: no modificó el gasto', 1;
    select @n = count(*) from dbo.ta_rgs_rndcn where cod_rndcn = @rnd and mnt_ttal = 170;
    if @n <> 1 throw 50001, 'edtar_gasto: el total no se recalculó (170)', 1;

    insert into @r exec dbo.pa_rgs_edtar_gasto @g1, @beto, @ctg_libre, @hoy, 'Intruso', 30;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_gasto: permitió editar el gasto de otra persona', 1;

    insert into @r exec dbo.pa_rgs_edtar_gasto @g1, @ana, @ctg_libre, @hoy, 'Taxi', 80;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_gasto: permitió superar el límite al editar', 1;

    -- enviar: reglas --------------------------------------------------------------
    insert into @r exec dbo.pa_rgs_envr_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'envr_rndcn: envió sin el comprobante obligatorio', 1;

    insert into @r exec dbo.pa_rgs_aggar_cmprb @g2, @beto, 'boleta.pdf', 'application/pdf', 8, @bin;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_cmprb: permitió adjuntar al gasto de otra persona', 1;

    insert into @r exec dbo.pa_rgs_aggar_cmprb @g2, @ana, '', 'application/pdf', 8, @bin;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_cmprb: aceptó un comprobante sin nombre', 1;

    insert into @r exec dbo.pa_rgs_aggar_cmprb @g2, @ana, 'boleta.pdf', 'application/pdf', 8, @bin;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 1 throw 50001, 'aggar_cmprb: no guardó el comprobante', 1;

    declare @cb table (nom_archv varchar(255), tpo_cntnd varchar(50), bin_cntnd varbinary(max));
    insert into @cb exec dbo.pa_rgs_sel_cmprb @g2, @ana;
    select @n = count(*) from @cb where nom_archv = 'boleta.pdf' and bin_cntnd = @bin;
    if @n <> 1 throw 50001, 'sel_cmprb: el dueño no obtuvo su comprobante', 1;
    delete from @cb;
    insert into @cb exec dbo.pa_rgs_sel_cmprb @g2, @beto;
    if exists (select 1 from @cb) throw 50001, 'sel_cmprb: otro empleado vio un comprobante ajeno', 1;
    insert into @cb exec dbo.pa_rgs_sel_cmprb @g2, @apr;
    if exists (select 1 from @cb) throw 50001, 'sel_cmprb: el aprobador vio un comprobante de un borrador', 1;

    -- sin aprobador no se puede enviar
    declare @rnd_sin int;
    insert into @r exec dbo.pa_rgs_aggar_rndcn @sin_apr, 'zz sin aprobador';
    select @rnd_sin = cod_resultado from @r; delete from @r;
    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd_sin, @sin_apr, @ctg_libre, @hoy, 'Taxi', 10;
    delete from @r;
    insert into @r exec dbo.pa_rgs_envr_rndcn @rnd_sin, @sin_apr;
    select @cod = cod_resultado, @msj = dsc_mensaje from @r; delete from @r;
    if @cod <> -1 or @msj not like '%aprobador%' throw 50001, 'envr_rndcn: envió sin aprobador asignado', 1;

    -- sin gastos no se puede enviar
    declare @rnd_vacia int;
    insert into @r exec dbo.pa_rgs_aggar_rndcn @beto, 'zz vacía';
    select @rnd_vacia = cod_resultado from @r; delete from @r;
    insert into @r exec dbo.pa_rgs_envr_rndcn @rnd_vacia, @beto;
    select @cod = cod_resultado, @msj = dsc_mensaje from @r; delete from @r;
    if @cod <> -1 or @msj not like '%al menos un gasto%' throw 50001, 'envr_rndcn: envió una rendición sin gastos', 1;

    -- enviar bien -------------------------------------------------------------------
    insert into @r exec dbo.pa_rgs_envr_rndcn @rnd, @beto;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'envr_rndcn: otra persona envió la rendición', 1;

    insert into @r exec dbo.pa_rgs_envr_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> @apr throw 50001, 'envr_rndcn: no devolvió al aprobador avisado', 1;

    insert into @r exec dbo.pa_rgs_envr_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'envr_rndcn: permitió enviar dos veces', 1;

    -- ya enviada: no se edita nada
    insert into @r exec dbo.pa_rgs_aggar_gasto @rnd, @ana, @ctg_libre, @hoy, 'Tarde', 10;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_gasto: agregó a una rendición enviada', 1;
    insert into @r exec dbo.pa_rgs_edtar_gasto @g1, @ana, @ctg_libre, @hoy, 'Taxi', 30;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_gasto: editó una rendición enviada', 1;
    insert into @r exec dbo.pa_rgs_del_gasto @g1, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'del_gasto: eliminó de una rendición enviada', 1;
    insert into @r exec dbo.pa_rgs_edtar_rndcn @rnd, @ana, 'Otro título';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'edtar_rndcn: editó una rendición enviada', 1;
    insert into @r exec dbo.pa_rgs_del_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'del_rndcn: eliminó una rendición enviada', 1;
    insert into @r exec dbo.pa_rgs_del_cmprb @g2, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'del_cmprb: quitó el comprobante de una rendición enviada', 1;

    -- listas y accesos ---------------------------------------------------------------
    declare @lr table (cod_rndcn int, nom_ttlo varchar(100), cod_usrio int, nom_usrio varchar(100), cod_estdo_rndcn int,
                       nom_estdo_rndcn varchar(50), mnt_ttal decimal(12,2), num_gastos int, fec_crcn varchar(10), fec_envio varchar(10));
    insert into @lr exec dbo.pa_rgs_lst_rndcn @ana, 'P', null;
    select @n = count(*) from @lr where cod_rndcn = @rnd and num_gastos = 3 and mnt_ttal = 170 and nom_estdo_rndcn = 'Enviada';
    if @n <> 1 throw 50001, 'lst_rndcn: la lista propia no muestra la rendición enviada', 1;
    delete from @lr;

    insert into @lr exec dbo.pa_rgs_lst_rndcn @beto, 'P', null;
    if exists (select 1 from @lr where cod_rndcn = @rnd) throw 50001, 'lst_rndcn: una persona vio las rendiciones de otra', 1;
    delete from @lr;

    insert into @lr exec dbo.pa_rgs_lst_rndcn @apr, 'A', 2;
    if not exists (select 1 from @lr where cod_rndcn = @rnd) throw 50001, 'lst_rndcn: el aprobador no ve la rendición por aprobar', 1;
    delete from @lr;

    insert into @lr exec dbo.pa_rgs_lst_rndcn @apr2, 'A', null;
    if exists (select 1 from @lr where cod_rndcn = @rnd) throw 50001, 'lst_rndcn: otro aprobador vio una rendición que no está a su cargo', 1;
    delete from @lr;

    insert into @lr exec dbo.pa_rgs_lst_rndcn @ana, 'A', null;
    if exists (select 1 from @lr) throw 50001, 'lst_rndcn: un empleado listó en modo aprobador', 1;
    delete from @lr;

    insert into @lr exec dbo.pa_rgs_lst_rndcn @adm, 'A', 2;
    if not exists (select 1 from @lr where cod_rndcn = @rnd) throw 50001, 'lst_rndcn: el administrador no ve las rendiciones enviadas', 1;
    delete from @lr;

    -- sel_rndcn devuelve tres resultados (cabecera, gastos, historial): INSERT-EXEC no admite varios,
    -- por eso sus permisos se prueban a traves de la API en las pruebas del backend.

    insert into @cb exec dbo.pa_rgs_sel_cmprb @g2, @apr;
    select @n = count(*) from @cb;
    if @n <> 1 throw 50001, 'sel_cmprb: el aprobador no vio el comprobante de la rendición enviada', 1;
    delete from @cb;
    insert into @cb exec dbo.pa_rgs_sel_cmprb @g2, @apr2;
    if exists (select 1 from @cb) throw 50001, 'sel_cmprb: otro aprobador vio un comprobante que no está a su cargo', 1;

    -- aprobar / rechazar: reglas ----------------------------------------------------
    insert into @r exec dbo.pa_rgs_aprbr_rndcn @rnd, @ana, null;
    select @cod = cod_resultado, @msj = dsc_mensaje from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aprbr_rndcn: un empleado aprobó', 1;

    insert into @r exec dbo.pa_rgs_aprbr_rndcn @rnd, @apr2, null;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aprbr_rndcn: aprobó un aprobador que no tiene a cargo a la persona', 1;

    insert into @r exec dbo.pa_rgs_rchzr_rndcn @rnd, @apr, 'corto';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'rchzr_rndcn: rechazó sin un motivo de 10 caracteres', 1;

    insert into @r exec dbo.pa_rgs_rchzr_rndcn @rnd, @apr, 'Falta aclarar el almuerzo con el cliente';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> @ana throw 50001, 'rchzr_rndcn: no devolvió al dueño avisado', 1;

    select @n = count(*) from dbo.ta_rgs_rndcn where cod_rndcn = @rnd and cod_estdo_rndcn = 4
        and dsc_mtvo_rchzo like 'Falta aclarar%' and cod_usrio_rsltr = @apr;
    if @n <> 1 throw 50001, 'rchzr_rndcn: no guardó estado, motivo y quién resolvió', 1;

    insert into @r exec dbo.pa_rgs_aprbr_rndcn @rnd, @apr, null;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aprbr_rndcn: aprobó una rendición ya rechazada', 1;

    -- el dueño recibe el aviso del rechazo
    declare @av table (cod_avso int, cod_rndcn int, nom_ttlo varchar(100), dsc_mnsje varchar(300), flg_leido char(1), fec_avso varchar(10), hor_avso varchar(5));
    insert into @av exec dbo.pa_rgs_lst_avso @ana, 'S';
    select @n = count(*) from @av where nom_ttlo = 'Rendición rechazada' and dsc_mnsje like '%Falta aclarar%' and flg_leido = 'N';
    if @n <> 1 throw 50001, 'lst_avso: el dueño no recibió el aviso del rechazo', 1;
    delete from @av;
    insert into @av exec dbo.pa_rgs_lst_avso @apr, 'S';
    select @n = count(*) from @av where nom_ttlo = 'Rendición por aprobar';
    if @n <> 1 throw 50001, 'lst_avso: el aprobador no recibió el aviso del envío', 1;
    delete from @av;

    -- reabrir, corregir y reenviar ---------------------------------------------------
    insert into @r exec dbo.pa_rgs_rabrr_rndcn @rnd, @beto;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'rabrr_rndcn: otra persona reabrió la rendición', 1;

    insert into @r exec dbo.pa_rgs_rabrr_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 1 throw 50001, 'rabrr_rndcn: no reabrió la rendición rechazada', 1;

    insert into @r exec dbo.pa_rgs_rabrr_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'rabrr_rndcn: reabrió una rendición que ya es borrador', 1;

    insert into @r exec dbo.pa_rgs_del_cmprb @g2, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 1 throw 50001, 'del_cmprb: no quitó el comprobante en borrador', 1;

    insert into @r exec dbo.pa_rgs_envr_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'envr_rndcn: envió sin comprobante tras quitarlo', 1;

    insert into @r exec dbo.pa_rgs_del_gasto @g2, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 1 throw 50001, 'del_gasto: no eliminó el gasto', 1;
    select @n = count(*) from dbo.ta_rgs_rndcn where cod_rndcn = @rnd and mnt_ttal = 80;
    if @n <> 1 throw 50001, 'del_gasto: el total no se recalculó (80)', 1;

    insert into @r exec dbo.pa_rgs_envr_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> @apr throw 50001, 'envr_rndcn: no reenvió la rendición corregida', 1;

    -- aprobar ------------------------------------------------------------------------
    insert into @r exec dbo.pa_rgs_aprbr_rndcn @rnd, @apr, 'Conforme';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> @ana throw 50001, 'aprbr_rndcn: no aprobó la rendición', 1;

    select @n = count(*) from dbo.ta_rgs_rndcn where cod_rndcn = @rnd and cod_estdo_rndcn = 3 and cod_usrio_rsltr = @apr and fec_rsln is not null;
    if @n <> 1 throw 50001, 'aprbr_rndcn: no guardó estado ni quién resolvió', 1;

    insert into @r exec dbo.pa_rgs_rabrr_rndcn @rnd, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'rabrr_rndcn: reabrió una rendición aprobada', 1;

    -- historial completo en el orden del flujo
    declare @h table (cod_hstrl int, cod_accn_hstrl int, nom_accn_hstrl varchar(50), cod_usrio int, nom_usrio varchar(100),
                      dsc_cmntr varchar(500), fec_hstrl varchar(10), hor_hstrl varchar(5));
    declare @g table (cod_gasto int, cod_ctgra int, nom_ctgra varchar(60), flg_cmprb_oblgt char(1), fec_gasto varchar(10),
                      dsc_gasto varchar(200), mnt_gasto decimal(12,2), nom_archv varchar(255), tpo_cntnd varchar(50), num_tamno int);
    -- los tres resultados de sel_rndcn se leen por separado con una consulta directa equivalente
    select @n = count(*) from dbo.ta_rgs_hstrl where cod_rndcn = @rnd and cod_accn_hstrl in (1, 2, 4, 5, 3);
    if @n <> 6 throw 50001, 'hstrl: el historial no tiene las 6 acciones esperadas (creada, enviada, rechazada, reabierta, enviada, aprobada)', 1;

    -- avisos leidos -------------------------------------------------------------------
    declare @cnt table (num_nleidos int);
    insert into @cnt exec dbo.pa_rgs_cnt_avso @ana;
    select @n = num_nleidos from @cnt;
    if @n <> 2 throw 50001, 'cnt_avso: la persona debería tener 2 avisos sin leer', 1;
    delete from @cnt;

    insert into @r exec dbo.pa_rgs_edtar_avso @beto, null;
    delete from @r;
    insert into @cnt exec dbo.pa_rgs_cnt_avso @ana;
    select @n = num_nleidos from @cnt;
    if @n <> 2 throw 50001, 'edtar_avso: otra persona marcó avisos ajenos', 1;
    delete from @cnt;

    insert into @r exec dbo.pa_rgs_edtar_avso @ana, null;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 2 throw 50001, 'edtar_avso: no marcó los 2 avisos', 1;
    insert into @cnt exec dbo.pa_rgs_cnt_avso @ana;
    select @n = num_nleidos from @cnt;
    if @n <> 0 throw 50001, 'cnt_avso: quedaron avisos sin leer', 1;

    -- suscripciones push ----------------------------------------------------------------
    insert into @r exec dbo.pa_rgs_aggar_sbscr @ana, 'https://push.zz-prueba/uno', 'p256', 'auth';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod < 1 throw 50001, 'aggar_sbscr: no registró el dispositivo', 1;

    insert into @r exec dbo.pa_rgs_aggar_sbscr @beto, 'https://push.zz-prueba/uno', 'p256b', 'authb';
    select @n = cod_resultado from @r; delete from @r;
    if @n <> @cod throw 50001, 'aggar_sbscr: duplicó el dispositivo en vez de reasignarlo', 1;

    declare @sb table (cod_sbscr int, nom_url_ptnto varchar(450), nom_llave_p256 varchar(200), nom_llave_auth varchar(100));
    insert into @sb exec dbo.pa_rgs_lst_sbscr @ana;
    if exists (select 1 from @sb) throw 50001, 'lst_sbscr: el dispositivo reasignado sigue con la persona anterior', 1;
    insert into @sb exec dbo.pa_rgs_lst_sbscr @beto;
    select @n = count(*) from @sb where nom_llave_p256 = 'p256b';
    if @n <> 1 throw 50001, 'lst_sbscr: no devolvió el dispositivo de la persona', 1;

    insert into @r exec dbo.pa_rgs_aggar_sbscr @ana, '', 'p', 'a';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_sbscr: aceptó una suscripción incompleta', 1;

    insert into @r exec dbo.pa_rgs_del_sbscr 'https://push.zz-prueba/uno';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 1 throw 50001, 'del_sbscr: no eliminó el dispositivo', 1;

    -- eliminar borrador completo -----------------------------------------------------------
    insert into @r exec dbo.pa_rgs_del_rndcn @rnd_vacia, @ana;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'del_rndcn: otra persona eliminó el borrador', 1;
    insert into @r exec dbo.pa_rgs_del_rndcn @rnd_vacia, @beto;
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> 1 throw 50001, 'del_rndcn: no eliminó el borrador', 1;

    -- límite de 10 borradores ---------------------------------------------------------------
    declare @i int = 1;
    while @i <= 10
    begin
        insert into @r exec dbo.pa_rgs_aggar_rndcn @beto, 'zz borrador de prueba';
        delete from @r;
        set @i += 1;
    end;
    insert into @r exec dbo.pa_rgs_aggar_rndcn @beto, 'zz borrador once';
    select @cod = cod_resultado from @r; delete from @r;
    if @cod <> -1 throw 50001, 'aggar_rndcn: permitió más de 10 borradores', 1;

    print 'TODAS LAS PRUEBAS PASARON';
end try
begin catch
    print 'FALLO: ' + error_message();
    exec sp_executesql @sql_limpia;
    throw;
end catch

exec sp_executesql @sql_limpia;
