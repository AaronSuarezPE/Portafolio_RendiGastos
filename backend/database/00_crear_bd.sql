/*
    Crea la base de datos local de RendiGastos.
    Solo para desarrollo: en el hosting la base ya viene creada por el proveedor.
*/
if db_id('rendigastos') is null
    create database rendigastos;
go
