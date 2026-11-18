/*
    RendiGastos - tablas
    Base de datos: rendigastos
    Fecha creacion: 17/11/2026

    Clasificacion (reglas de base de datos):
      mst_      maestras sin movimiento: valores fijos, sin auditoria.
      mst_mov_  maestras con movimiento: se administran y cambian, con auditoria cnr_.
      resto     operativas: con auditoria cnr_.

    El script se puede ejecutar varias veces: solo crea lo que falta.
*/

-- maestras sin movimiento -----------------------------------------------------

if object_id('dbo.ta_rgs_mst_rol', 'u') is null
    create table dbo.ta_rgs_mst_rol (
        cod_rol int          not null,
        nom_rol varchar(50)  not null,
        dsc_rol varchar(200) not null,
        constraint pk_ta_rgs_mst_rol primary key (cod_rol)
    );
go

if object_id('dbo.ta_rgs_mst_estdo_rndcn', 'u') is null
    create table dbo.ta_rgs_mst_estdo_rndcn (
        cod_estdo_rndcn int          not null,
        nom_estdo_rndcn varchar(50)  not null,
        dsc_estdo_rndcn varchar(200) not null,
        constraint pk_ta_rgs_mst_estdo_rndcn primary key (cod_estdo_rndcn)
    );
go

if object_id('dbo.ta_rgs_mst_accn_hstrl', 'u') is null
    create table dbo.ta_rgs_mst_accn_hstrl (
        cod_accn_hstrl int          not null,
        nom_accn_hstrl varchar(50)  not null,
        dsc_accn_hstrl varchar(200) not null,
        constraint pk_ta_rgs_mst_accn_hstrl primary key (cod_accn_hstrl)
    );
go

-- maestras con movimiento -----------------------------------------------------

if object_id('dbo.ta_rgs_mst_mov_usrio', 'u') is null
    create table dbo.ta_rgs_mst_mov_usrio (
        cod_usrio       int          not null,
        nom_usrio       varchar(100) not null,
        nom_email       varchar(150) not null,
        nom_clave_hsh   varchar(200) not null,
        cod_rol         int          not null,
        cod_aprbdr      int          null,
        flg_actvo       char(1)      not null,
        fec_ultmo_login datetime     null,
        cnr_fec_rgtro   datetime     not null,
        cnr_usr_rgtro   int          not null,
        cnr_fec_mdfcn   datetime     null,
        cnr_usr_mdfcn   int          null,
        cnr_estdo_rgtro int          not null,
        constraint pk_ta_rgs_mst_mov_usrio primary key (cod_usrio),
        constraint fk_ta_rgs_mst_mov_usrio_rol foreign key (cod_rol) references dbo.ta_rgs_mst_rol (cod_rol),
        constraint fk_ta_rgs_mst_mov_usrio_aprbdr foreign key (cod_aprbdr) references dbo.ta_rgs_mst_mov_usrio (cod_usrio),
        constraint ck_ta_rgs_mst_mov_usrio_actvo check (flg_actvo in ('S', 'N')),
        constraint ck_ta_rgs_mst_mov_usrio_aprbdr check (cod_aprbdr is null or cod_aprbdr <> cod_usrio)
    );
go

if not exists (select 1 from sys.indexes where name = 'ux_ta_rgs_mst_mov_usrio_email')
    create unique index ux_ta_rgs_mst_mov_usrio_email on dbo.ta_rgs_mst_mov_usrio (nom_email);
go

if object_id('dbo.ta_rgs_mst_mov_ctgra', 'u') is null
    create table dbo.ta_rgs_mst_mov_ctgra (
        cod_ctgra       int           not null,
        nom_ctgra       varchar(60)   not null,
        dsc_ctgra       varchar(200)  not null,
        mnt_lmte        decimal(12,2) not null,
        flg_cmprb_oblgt char(1)       not null,
        flg_actvo       char(1)       not null,
        cnr_fec_rgtro   datetime      not null,
        cnr_usr_rgtro   int           not null,
        cnr_fec_mdfcn   datetime      null,
        cnr_usr_mdfcn   int           null,
        cnr_estdo_rgtro int           not null,
        constraint pk_ta_rgs_mst_mov_ctgra primary key (cod_ctgra),
        constraint ck_ta_rgs_mst_mov_ctgra_lmte check (mnt_lmte > 0),
        constraint ck_ta_rgs_mst_mov_ctgra_cmprb check (flg_cmprb_oblgt in ('S', 'N')),
        constraint ck_ta_rgs_mst_mov_ctgra_actvo check (flg_actvo in ('S', 'N'))
    );
go

if not exists (select 1 from sys.indexes where name = 'ux_ta_rgs_mst_mov_ctgra_nom')
    create unique index ux_ta_rgs_mst_mov_ctgra_nom on dbo.ta_rgs_mst_mov_ctgra (nom_ctgra);
go

-- operativas ------------------------------------------------------------------

if object_id('dbo.ta_rgs_rndcn', 'u') is null
    create table dbo.ta_rgs_rndcn (
        cod_rndcn       int           not null,
        cod_usrio       int           not null,
        nom_ttlo        varchar(100)  not null,
        cod_estdo_rndcn int           not null,
        mnt_ttal        decimal(12,2) not null,
        dsc_mtvo_rchzo  varchar(500)  null,
        cod_usrio_rsltr int           null,
        fec_envio       datetime      null,
        fec_rsln        datetime      null,
        cnr_fec_rgtro   datetime      not null,
        cnr_usr_rgtro   int           not null,
        cnr_fec_mdfcn   datetime      null,
        cnr_usr_mdfcn   int           null,
        cnr_estdo_rgtro int           not null,
        constraint pk_ta_rgs_rndcn primary key (cod_rndcn),
        constraint fk_ta_rgs_rndcn_usrio foreign key (cod_usrio) references dbo.ta_rgs_mst_mov_usrio (cod_usrio),
        constraint fk_ta_rgs_rndcn_estdo foreign key (cod_estdo_rndcn) references dbo.ta_rgs_mst_estdo_rndcn (cod_estdo_rndcn)
    );
go

if not exists (select 1 from sys.indexes where name = 'ix_ta_rgs_rndcn_usrio')
    create index ix_ta_rgs_rndcn_usrio on dbo.ta_rgs_rndcn (cod_usrio, cod_estdo_rndcn);
go

if object_id('dbo.ta_rgs_gasto', 'u') is null
    create table dbo.ta_rgs_gasto (
        cod_gasto       int           not null,
        cod_rndcn       int           not null,
        cod_ctgra       int           not null,
        fec_gasto       date          not null,
        dsc_gasto       varchar(200)  not null,
        mnt_gasto       decimal(12,2) not null,
        nom_archv       varchar(255)  null,
        tpo_cntnd       varchar(50)   null,
        num_tamno       int           null,
        cnr_fec_rgtro   datetime      not null,
        cnr_usr_rgtro   int           not null,
        cnr_fec_mdfcn   datetime      null,
        cnr_usr_mdfcn   int           null,
        cnr_estdo_rgtro int           not null,
        constraint pk_ta_rgs_gasto primary key (cod_gasto),
        constraint fk_ta_rgs_gasto_rndcn foreign key (cod_rndcn) references dbo.ta_rgs_rndcn (cod_rndcn),
        constraint fk_ta_rgs_gasto_ctgra foreign key (cod_ctgra) references dbo.ta_rgs_mst_mov_ctgra (cod_ctgra),
        constraint ck_ta_rgs_gasto_mnt check (mnt_gasto > 0)
    );
go

if not exists (select 1 from sys.indexes where name = 'ix_ta_rgs_gasto_rndcn')
    create index ix_ta_rgs_gasto_rndcn on dbo.ta_rgs_gasto (cod_rndcn);
go

if object_id('dbo.ta_rgs_gasto_cmprb', 'u') is null
    create table dbo.ta_rgs_gasto_cmprb (
        cod_gasto int            not null,
        bin_cntnd varbinary(max) not null,
        constraint pk_ta_rgs_gasto_cmprb primary key (cod_gasto),
        constraint fk_ta_rgs_gasto_cmprb_gasto foreign key (cod_gasto) references dbo.ta_rgs_gasto (cod_gasto)
    );
go

if object_id('dbo.ta_rgs_hstrl', 'u') is null
    create table dbo.ta_rgs_hstrl (
        cod_hstrl       int          not null,
        cod_rndcn       int          not null,
        cod_accn_hstrl  int          not null,
        cod_usrio       int          not null,
        dsc_cmntr       varchar(500) null,
        cnr_fec_rgtro   datetime     not null,
        cnr_usr_rgtro   int          not null,
        cnr_fec_mdfcn   datetime     null,
        cnr_usr_mdfcn   int          null,
        cnr_estdo_rgtro int          not null,
        constraint pk_ta_rgs_hstrl primary key (cod_hstrl),
        constraint fk_ta_rgs_hstrl_rndcn foreign key (cod_rndcn) references dbo.ta_rgs_rndcn (cod_rndcn),
        constraint fk_ta_rgs_hstrl_accn foreign key (cod_accn_hstrl) references dbo.ta_rgs_mst_accn_hstrl (cod_accn_hstrl)
    );
go

if not exists (select 1 from sys.indexes where name = 'ix_ta_rgs_hstrl_rndcn')
    create index ix_ta_rgs_hstrl_rndcn on dbo.ta_rgs_hstrl (cod_rndcn);
go

if object_id('dbo.ta_rgs_avso', 'u') is null
    create table dbo.ta_rgs_avso (
        cod_avso        int          not null,
        cod_usrio       int          not null,
        cod_rndcn       int          null,
        nom_ttlo        varchar(100) not null,
        dsc_mnsje       varchar(300) not null,
        flg_leido       char(1)      not null,
        cnr_fec_rgtro   datetime     not null,
        cnr_usr_rgtro   int          not null,
        cnr_fec_mdfcn   datetime     null,
        cnr_usr_mdfcn   int          null,
        cnr_estdo_rgtro int          not null,
        constraint pk_ta_rgs_avso primary key (cod_avso),
        constraint fk_ta_rgs_avso_usrio foreign key (cod_usrio) references dbo.ta_rgs_mst_mov_usrio (cod_usrio),
        constraint ck_ta_rgs_avso_leido check (flg_leido in ('S', 'N'))
    );
go

if not exists (select 1 from sys.indexes where name = 'ix_ta_rgs_avso_usrio')
    create index ix_ta_rgs_avso_usrio on dbo.ta_rgs_avso (cod_usrio, flg_leido);
go

if object_id('dbo.ta_rgs_sbscr_push', 'u') is null
    create table dbo.ta_rgs_sbscr_push (
        cod_sbscr       int          not null,
        cod_usrio       int          not null,
        nom_url_ptnto   varchar(450) not null,
        nom_llave_p256  varchar(200) not null,
        nom_llave_auth  varchar(100) not null,
        cnr_fec_rgtro   datetime     not null,
        cnr_usr_rgtro   int          not null,
        cnr_fec_mdfcn   datetime     null,
        cnr_usr_mdfcn   int          null,
        cnr_estdo_rgtro int          not null,
        constraint pk_ta_rgs_sbscr_push primary key (cod_sbscr),
        constraint fk_ta_rgs_sbscr_push_usrio foreign key (cod_usrio) references dbo.ta_rgs_mst_mov_usrio (cod_usrio)
    );
go

if not exists (select 1 from sys.indexes where name = 'ux_ta_rgs_sbscr_push_url')
    create unique index ux_ta_rgs_sbscr_push_url on dbo.ta_rgs_sbscr_push (nom_url_ptnto);
go
