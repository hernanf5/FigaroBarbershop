-- =========================================================
-- Proyecto Figaro Barbershop
-- Script de creación e inicialización de la base de datos
-- =========================================================

DROP DATABASE IF EXISTS FigaroDB;
CREATE DATABASE FigaroDB
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE FigaroDB;

-- =========================================================
-- Tabla: Servicio
-- =========================================================
CREATE TABLE Servicio (
    IdServicio       INT AUTO_INCREMENT PRIMARY KEY,
    Nombre           VARCHAR(100) NOT NULL,
    DuracionMinutos  INT NOT NULL,
    Precio           DECIMAL(12,2) NOT NULL,
    Activo           TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;

-- =========================================================
-- Tabla: Producto
-- =========================================================
CREATE TABLE Producto (
    IdProducto   INT AUTO_INCREMENT PRIMARY KEY,
    Nombre       VARCHAR(100) NOT NULL,
    Precio       DECIMAL(12,2) NOT NULL,
    ImagenUrl    VARCHAR(300) NULL,
    Activo       TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;

-- =========================================================
-- Datos iniciales
-- =========================================================
INSERT INTO Servicio (Nombre, DuracionMinutos, Precio) VALUES
    ('Corte clásico', 30, 6000.00),
    ('Corte + barba', 50, 9500.00),
    ('Arreglo de barba', 20, 4000.00);

INSERT INTO Producto (Nombre, Precio) VALUES
    ('Cera moldeadora', 4500.00),
    ('Aceite para barba', 5200.00),
    ('Shampoo anticaída', 6800.00);