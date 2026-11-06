-- Proyecto Figaro Barbershop
-- Script de creación e inicialización de la base de datos

DROP DATABASE IF EXISTS FigaroDB;
CREATE DATABASE FigaroDB
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE FigaroDB;


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

-- Tabla: Cliente
CREATE TABLE Cliente (
    IdCliente  INT AUTO_INCREMENT PRIMARY KEY,
    Nombre     VARCHAR(100) NOT NULL,
    Apellido   VARCHAR(100) NOT NULL,
    Email      VARCHAR(150) NOT NULL UNIQUE,
    Telefono   VARCHAR(30)  NOT NULL
) ENGINE=InnoDB;

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

-- Tabla: Turno
CREATE TABLE Turno (
    IdTurno         INT AUTO_INCREMENT PRIMARY KEY,
    IdAgenda        INT NOT NULL,
    IdCliente       INT NOT NULL,
    IdServicio      INT NOT NULL,
    CantidadSlots   INT NOT NULL,
    PrecioServicio  DECIMAL(12,2) NOT NULL,
    Sena            DECIMAL(12,2) NOT NULL,
    Estado          VARCHAR(20) NOT NULL DEFAULT 'Confirmado',
    FechaCreacion   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT CK_Turno_Estado CHECK (Estado IN ('Confirmado', 'Finalizado', 'Cancelado', 'No se presentó')),
    CONSTRAINT FK_Turno_Agenda FOREIGN KEY (IdAgenda) REFERENCES Agenda(IdAgenda),
    CONSTRAINT FK_Turno_Cliente FOREIGN KEY (IdCliente) REFERENCES Cliente(IdCliente),
    CONSTRAINT FK_Turno_Servicio FOREIGN KEY (IdServicio) REFERENCES Servicio(IdServicio)
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

-- Usuarios de prueba 
-- Admin: admin@figaro.com / Admin1234
-- Barberos: barbero@figaro.com y barbero2@figaro.com / Barbero1234
INSERT INTO Usuario (Nombre, Apellido, Email, Clave, Rol, AvatarUrl) VALUES
    ('Hernán', 'Funes', 'admin@figaro.com', 'vuGuAj9hr0a4ldXxIJmNr7Ygd+Mw2V0dH+HkPIhHqTY=', 'Administrador', NULL),
    ('Juan', 'Pérez', 'barbero@figaro.com', 'c3B7uDbeJ1WWyrvOdJ8BogtsORlbggM5oInjqIXkgwc=', 'Barbero', '/uploads/avatares/avatar_2.jpg'),
    ('María', 'González', 'barbero2@figaro.com', 'c3B7uDbeJ1WWyrvOdJ8BogtsORlbggM5oInjqIXkgwc=', 'Barbero', '/uploads/avatares/avatar_3.jpg');



-- Datos de prueba


-- Clientes
INSERT INTO Cliente (Nombre, Apellido, Email, Telefono) VALUES
    ('Lucas',     'Fernández', 'lucas.fernandez@example.com', '3515550101'),
    ('Martín',    'Gómez',     'martin.gomez@example.com',    '3515550102'),
    ('Diego',     'Sosa',      'diego.sosa@example.com',      '3515550103'),
    ('Pablo',     'Ruiz',      'pablo.ruiz@example.com',      '3515550104'),
    ('Sebastián', 'Díaz',      'sebastian.diaz@example.com',  '3515550105'),
    ('Nicolás',   'Ledesma',   'nicolas.ledesma@example.com', '3515550106');

-- Agenda: lunes a sábado, 9:00 a 20:00, franjas de 30 min, para cada barbero
-- (14 días hacia atrás y 30 hacia adelante, relativo a la fecha de ejecución)
INSERT INTO Agenda (IdUsuario, Fecha, HoraInicio, HoraFin, Estado)
WITH RECURSIVE dias (Fecha) AS (
    SELECT CURDATE() - INTERVAL 14 DAY
    UNION ALL
    SELECT Fecha + INTERVAL 1 DAY FROM dias WHERE Fecha < CURDATE() + INTERVAL 30 DAY
), franjas (n) AS (
    SELECT 0
    UNION ALL
    SELECT n + 1 FROM franjas WHERE n < 21
)
SELECT u.IdUsuario, d.Fecha,
       SEC_TO_TIME(32400 + f.n * 1800),
       SEC_TO_TIME(32400 + (f.n + 1) * 1800),
       'Disponible'
FROM Usuario u
CROSS JOIN dias d
CROSS JOIN franjas f
WHERE u.Rol = 'Barbero' AND DAYOFWEEK(d.Fecha) <> 1;   -- 1 = domingo

-- Turnos de ejemplo (fechas relativas, saltando domingos)
SET @b1 = (SELECT IdUsuario FROM Usuario WHERE Email = 'barbero@figaro.com');
SET @b2 = (SELECT IdUsuario FROM Usuario WHERE Email = 'barbero2@figaro.com');

SET @p1 = (SELECT MAX(Fecha) FROM Agenda WHERE Fecha <= CURDATE() - INTERVAL 1 DAY);
SET @p2 = (SELECT MAX(Fecha) FROM Agenda WHERE Fecha <= CURDATE() - INTERVAL 2 DAY);
SET @p3 = (SELECT MAX(Fecha) FROM Agenda WHERE Fecha <= CURDATE() - INTERVAL 3 DAY);
SET @p4 = (SELECT MAX(Fecha) FROM Agenda WHERE Fecha <= CURDATE() - INTERVAL 4 DAY);
SET @p5 = (SELECT MAX(Fecha) FROM Agenda WHERE Fecha <= CURDATE() - INTERVAL 5 DAY);
SET @p6 = (SELECT MAX(Fecha) FROM Agenda WHERE Fecha <= CURDATE() - INTERVAL 6 DAY);
SET @f1 = (SELECT MIN(Fecha) FROM Agenda WHERE Fecha >= CURDATE() + INTERVAL 1 DAY);
SET @f2 = (SELECT MIN(Fecha) FROM Agenda WHERE Fecha >= CURDATE() + INTERVAL 2 DAY);
SET @f3 = (SELECT MIN(Fecha) FROM Agenda WHERE Fecha >= CURDATE() + INTERVAL 3 DAY);
SET @f5 = (SELECT MIN(Fecha) FROM Agenda WHERE Fecha >= CURDATE() + INTERVAL 5 DAY);
SET @f7 = (SELECT MIN(Fecha) FROM Agenda WHERE Fecha >= CURDATE() + INTERVAL 7 DAY);

CREATE TEMPORARY TABLE SeedTurno (
    IdBarbero INT, Fecha DATE, Hora TIME, Email VARCHAR(150), Servicio VARCHAR(100), Estado VARCHAR(20)
);

INSERT INTO SeedTurno VALUES
    -- pasados
    (@b1, @p1, '10:00:00', 'lucas.fernandez@example.com', 'Corte clásico',    'Finalizado'),
    (@b1, @p1, '11:30:00', 'martin.gomez@example.com',    'Corte + barba',    'Finalizado'),
    (@b1, @p3, '15:00:00', 'diego.sosa@example.com',      'Arreglo de barba', 'No se presentó'),
    (@b1, @p5, '17:00:00', 'lucas.fernandez@example.com', 'Corte clásico',    'Cancelado'),
    (@b2, @p2, '09:30:00', 'pablo.ruiz@example.com',      'Corte clásico',    'Finalizado'),
    (@b2, @p4, '16:00:00', 'martin.gomez@example.com',    'Corte + barba',    'Finalizado'),
    (@b2, @p6, '12:00:00', 'sebastian.diaz@example.com',  'Corte clásico',    'Cancelado'),
    -- futuros
    (@b1, @f1, '10:00:00', 'diego.sosa@example.com',      'Corte + barba',    'Confirmado'),
    (@b1, @f1, '16:30:00', 'pablo.ruiz@example.com',      'Corte clásico',    'Confirmado'),
    (@b1, @f3, '11:00:00', 'sebastian.diaz@example.com',  'Arreglo de barba', 'Confirmado'),
    (@b1, @f7, '18:00:00', 'lucas.fernandez@example.com', 'Corte clásico',    'Confirmado'),
    (@b2, @f1, '12:30:00', 'martin.gomez@example.com',    'Corte clásico',    'Confirmado'),
    (@b2, @f2, '15:00:00', 'lucas.fernandez@example.com', 'Corte + barba',    'Confirmado'),
    (@b2, @f5, '10:00:00', 'diego.sosa@example.com',      'Corte clásico',    'Confirmado');

INSERT INTO Turno (IdAgenda, IdCliente, IdServicio, CantidadSlots, PrecioServicio, Sena, Estado)
SELECT a.IdAgenda, c.IdCliente, s.IdServicio,
       CEIL(s.DuracionMinutos / 30), s.Precio, LEAST(5000, s.Precio), st.Estado
FROM SeedTurno st
JOIN Agenda   a ON a.IdUsuario = st.IdBarbero AND a.Fecha = st.Fecha AND a.HoraInicio = st.Hora
JOIN Cliente  c ON c.Email = st.Email
JOIN Servicio s ON s.Nombre = st.Servicio;

-- Marcar como Reservadas las franjas de los turnos no cancelados
UPDATE Agenda a, Turno t, Agenda ini
SET a.Estado = 'Reservado'
WHERE t.Estado <> 'Cancelado'
  AND ini.IdAgenda = t.IdAgenda
  AND a.IdUsuario = ini.IdUsuario
  AND a.Fecha = ini.Fecha
  AND a.HoraInicio >= ini.HoraInicio
  AND a.HoraInicio < ADDTIME(ini.HoraInicio, SEC_TO_TIME(t.CantidadSlots * 1800));

-- Un día completo bloqueado para María (ejemplo de "No disponible")
UPDATE Agenda SET Estado = 'No disponible'
WHERE IdUsuario = @b2 AND Fecha = @f7 AND Estado = 'Disponible';

DROP TEMPORARY TABLE SeedTurno;