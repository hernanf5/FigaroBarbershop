-- Proyecto Figaro Barbershop
-- Script de creación e inicialización de la base de datos

DROP DATABASE IF EXISTS FigaroDB;
CREATE DATABASE FigaroDB
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE FigaroDB;


-- Tabla: Servicio

CREATE TABLE Servicio (
    IdServicio       INT AUTO_INCREMENT PRIMARY KEY,
    Nombre           VARCHAR(100) NOT NULL,
    DuracionMinutos  INT NOT NULL,
    Precio           DECIMAL(12,2) NOT NULL,
    Activo           TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;


-- Tabla: Producto

CREATE TABLE Producto (
    IdProducto   INT AUTO_INCREMENT PRIMARY KEY,
    Nombre       VARCHAR(100) NOT NULL,
    Precio       DECIMAL(12,2) NOT NULL,
    ImagenUrl    VARCHAR(300) NULL,
    Activo       TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;


-- Datos iniciales

INSERT INTO Servicio (Nombre, DuracionMinutos, Precio) VALUES
    ('Corte clásico', 30, 6000.00),
    ('Corte + barba', 50, 9500.00),
    ('Arreglo de barba', 20, 4000.00);

INSERT INTO Producto (Nombre, Precio) VALUES
    ('Cera moldeadora', 4500.00),
    ('Aceite para barba', 5200.00),
    ('Shampoo anticaída', 6800.00);


-- Tabla: Usuario
CREATE TABLE Usuario (
    IdUsuario   INT AUTO_INCREMENT PRIMARY KEY,
    Nombre      VARCHAR(100) NOT NULL,
    Apellido    VARCHAR(100) NOT NULL,
    Email       VARCHAR(150) NOT NULL UNIQUE,
    Clave       VARCHAR(256) NOT NULL,
    Rol         VARCHAR(20)  NOT NULL,
    AvatarUrl   VARCHAR(300) NULL,
    Activo      TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT CK_Usuario_Rol CHECK (Rol IN ('Administrador', 'Barbero'))
) ENGINE=InnoDB;

-- Usuarios de prueba (requieren que appsettings.json tenga: "Salt": "figaro-2026-barberia")
INSERT INTO Usuario (Nombre, Apellido, Email, Clave, Rol) VALUES
    ('Hernán', 'Funes', 'admin@figaro.com', 'vuGuAj9hr0a4ldXxIJmNr7Ygd+Mw2V0dH+HkPIhHqTY=', 'Administrador'),
    ('Juan', 'Pérez', 'barbero@figaro.com', 'c3B7uDbeJ1WWyrvOdJ8BogtsORlbggM5oInjqIXkgwc=', 'Barbero');


-- Tabla: Agenda

CREATE TABLE Agenda (
    IdAgenda    INT AUTO_INCREMENT PRIMARY KEY,
    IdUsuario   INT NOT NULL,
    Fecha       DATE NOT NULL,
    HoraInicio  TIME NOT NULL,
    HoraFin     TIME NOT NULL,
    Estado      VARCHAR(20) NOT NULL DEFAULT 'Disponible',
    CONSTRAINT CK_Agenda_Estado CHECK (Estado IN ('Disponible', 'No disponible', 'Reservado')),
    CONSTRAINT FK_Agenda_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
    CONSTRAINT UQ_Agenda_Slot UNIQUE (IdUsuario, Fecha, HoraInicio)
) ENGINE=InnoDB;
