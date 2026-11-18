/*
    RendiGastos - datos iniciales de las tablas maestras.
    Se puede repetir sin duplicar. cnr_usr_rgtro = 0 identifica al sistema.
*/

merge dbo.ta_rgs_mst_rol as d
using (values
    (1, 'Empleado',      'Registra sus rendiciones de gastos y las envía a aprobar'),
    (2, 'Aprobador',     'Aprueba o rechaza las rendiciones de las personas a su cargo'),
    (3, 'Administrador', 'Administra usuarios, aprobadores y categorías de gasto')
) as o (cod_rol, nom_rol, dsc_rol)
on d.cod_rol = o.cod_rol
when not matched then insert (cod_rol, nom_rol, dsc_rol) values (o.cod_rol, o.nom_rol, o.dsc_rol);
go

merge dbo.ta_rgs_mst_estdo_rndcn as d
using (values
    (1, 'Borrador',  'La persona todavía arma la rendición; se puede editar'),
    (2, 'Enviada',   'Espera la decisión del aprobador; no se puede editar'),
    (3, 'Aprobada',  'El aprobador la aceptó; queda cerrada'),
    (4, 'Rechazada', 'El aprobador la devolvió con un motivo; se puede reabrir')
) as o (cod_estdo_rndcn, nom_estdo_rndcn, dsc_estdo_rndcn)
on d.cod_estdo_rndcn = o.cod_estdo_rndcn
when not matched then insert (cod_estdo_rndcn, nom_estdo_rndcn, dsc_estdo_rndcn)
    values (o.cod_estdo_rndcn, o.nom_estdo_rndcn, o.dsc_estdo_rndcn);
go

merge dbo.ta_rgs_mst_accn_hstrl as d
using (values
    (1, 'Creada',    'Se creó la rendición'),
    (2, 'Enviada',   'Se envió a aprobación'),
    (3, 'Aprobada',  'El aprobador la aprobó'),
    (4, 'Rechazada', 'El aprobador la rechazó'),
    (5, 'Reabierta', 'Se devolvió a borrador para corregirla')
) as o (cod_accn_hstrl, nom_accn_hstrl, dsc_accn_hstrl)
on d.cod_accn_hstrl = o.cod_accn_hstrl
when not matched then insert (cod_accn_hstrl, nom_accn_hstrl, dsc_accn_hstrl)
    values (o.cod_accn_hstrl, o.nom_accn_hstrl, o.dsc_accn_hstrl);
go

merge dbo.ta_rgs_mst_mov_ctgra as d
using (values
    (1, 'Transporte',   'Pasajes, taxis, combustible y peajes',          200.00, 'N'),
    (2, 'Alimentación', 'Comidas durante comisiones de trabajo',         150.00, 'S'),
    (3, 'Hospedaje',    'Alojamiento en viajes de trabajo',              500.00, 'S'),
    (4, 'Materiales',   'Útiles y materiales de oficina',                300.00, 'S'),
    (5, 'Otros',        'Gastos que no encajan en las demás categorías', 100.00, 'S')
) as o (cod_ctgra, nom_ctgra, dsc_ctgra, mnt_lmte, flg_cmprb_oblgt)
on d.cod_ctgra = o.cod_ctgra
when not matched then insert
    (cod_ctgra, nom_ctgra, dsc_ctgra, mnt_lmte, flg_cmprb_oblgt, flg_actvo,
     cnr_fec_rgtro, cnr_usr_rgtro, cnr_estdo_rgtro)
    values (o.cod_ctgra, o.nom_ctgra, o.dsc_ctgra, o.mnt_lmte, o.flg_cmprb_oblgt, 'S', getdate(), 0, 1);
go
