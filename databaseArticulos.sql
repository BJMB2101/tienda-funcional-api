create database servicio_web;
use servicio_web;
create table articulos
(
    id_articulo integer auto_increment primary key,
    descripcion varchar(200) not null,
    precio float not null,
    cantidad integer not null
);
create table fotos_articulos
(
    id_foto integer auto_increment primary key,
    foto longblob,
    id_articulo integer not null
);
create table carrito_compra(
    id_carrito integer auto_increment primary key,
    id_articulo integer not null,
    cantidad integer not null
)
alter table fotos_articulos add foreign key (id_articulo) references articulos(id_articulo);
alter table carrito_compra add foreign key (id_articulo) references articulos(id_articulo);

create unique index articulos_1 on articulos(descripcion);
