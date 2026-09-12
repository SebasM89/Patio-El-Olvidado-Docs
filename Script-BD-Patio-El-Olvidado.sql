CREATE DATABASE ElOlvidado;
GO

USE ElOlvidado;
GO

-- =========================
-- Autenticación (MVP)
-- =========================

CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(50) NOT NULL UNIQUE,
    Descripcion NVARCHAR(200) NULL
);

CREATE TABLE Permisos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Codigo NVARCHAR(80) NOT NULL UNIQUE,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(200) NULL
);

CREATE TABLE RolPermisos (
    RolId INT NOT NULL,
    PermisoId INT NOT NULL,
    PRIMARY KEY (RolId, PermisoId),
    FOREIGN KEY (RolId) REFERENCES Roles(Id),
    FOREIGN KEY (PermisoId) REFERENCES Permisos(Id)
);

CREATE TABLE Usuarios (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    RolId INT NOT NULL,
    Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_Usuarios_Estado DEFAULT ('Activo'),
    UltimoAcceso DATETIME2 NULL,
    IntentosFallidos INT NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT (0),
    BloqueadoHasta DATETIME2 NULL,
    FOREIGN KEY (RolId) REFERENCES Roles(Id)
);

CREATE TABLE RefreshTokens (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UsuarioId INT NOT NULL,
    Token NVARCHAR(500) NOT NULL UNIQUE,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    RevokedAt DATETIME2 NULL,
    ReplacedByToken NVARCHAR(500) NULL,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE
);

CREATE TABLE PasswordResetTokens (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UsuarioId INT NOT NULL,
    Token NVARCHAR(500) NOT NULL UNIQUE,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UsedAt DATETIME2 NULL,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE
);

-- Seed roles
INSERT INTO Roles (Nombre, Descripcion) VALUES
(N'Admin', N'Administrador del sistema'),
(N'Empleado', N'Empleado del restaurante'),
(N'Cliente', N'Cliente');

-- =========================
-- Dominio operativo (legado / MVP futuro)
-- =========================

-- Tabla Clientes
CREATE TABLE Clientes (
    id_cliente INT PRIMARY KEY IDENTITY(1,1),
    nombre VARCHAR(100),
    telefono VARCHAR(20),
    correo VARCHAR(100),
    direccion VARCHAR(255)
);

-- Tabla Mesas
CREATE TABLE Mesas (
    id_mesa INT PRIMARY KEY IDENTITY(1,1),
    numero INT,
    capacidad INT,
    ubicacion VARCHAR(100)
);

-- Tabla Empleados
CREATE TABLE Empleados (
    id_empleado INT PRIMARY KEY IDENTITY(1,1),
    nombre VARCHAR(100),
    apellido VARCHAR(100),
    puesto VARCHAR(100),
    telefono VARCHAR(20)
);

-- Tabla Menus
CREATE TABLE Menus (
    id_producto INT PRIMARY KEY IDENTITY(1,1),
    nombre VARCHAR(100),
    descripcion TEXT,
    precio DECIMAL(10,2),
    es_promocion BIT
);

-- Tabla Pedidos
CREATE TABLE Pedidos (
    id_pedido INT PRIMARY KEY IDENTITY(1,1),
    id_cliente INT,
    fecha_pedido DATE,
    hora TIME,
    estado VARCHAR(50), -- en preparación, enviado, entregado, cancelado
    tipo VARCHAR(20),   -- en local, delivery
    FOREIGN KEY (id_cliente) REFERENCES Clientes(id_cliente)
);

-- Tabla DetallePedidos
CREATE TABLE DetallePedidos (
    id_detalle INT PRIMARY KEY IDENTITY(1,1),
    id_pedido INT,
    id_producto INT,
    cantidad INT,
    subtotal DECIMAL(10,2),
    FOREIGN KEY (id_pedido) REFERENCES Pedidos(id_pedido),
    FOREIGN KEY (id_producto) REFERENCES Menus(id_producto)
);

-- Tabla Reservaciones
CREATE TABLE Reservaciones (
    id_reservacion INT PRIMARY KEY IDENTITY(1,1),
    id_cliente INT,
    id_mesa INT,
    fecha DATE,
    hora_inicio TIME,
    hora_fin TIME,
    estado VARCHAR(50), -- confirmada, cancelada, finalizada
    FOREIGN KEY (id_cliente) REFERENCES Clientes(id_cliente),
    FOREIGN KEY (id_mesa) REFERENCES Mesas(id_mesa)
);

-- Tabla Turnos
CREATE TABLE Turnos (
    id_turno INT PRIMARY KEY IDENTITY(1,1),
    id_empleado INT,
    fecha DATE,
    hora_entrada TIME,
    hora_salida TIME,
    FOREIGN KEY (id_empleado) REFERENCES Empleados(id_empleado)
);

-- Tabla Envios
CREATE TABLE Envios (
    id_envio INT PRIMARY KEY IDENTITY(1,1),
    id_pedido INT,
    direccion_entrega VARCHAR(255),
    fecha_envio DATE,
    hora_envio TIME,
    id_empleado INT, -- repartidor
    estado BIT, -- 1 en camino, 0 entregado
    FOREIGN KEY (id_pedido) REFERENCES Pedidos(id_pedido),
    FOREIGN KEY (id_empleado) REFERENCES Empleados(id_empleado)
);

-- Tabla HistorialClientes
CREATE TABLE HistorialClientes (
    id_historial INT PRIMARY KEY IDENTITY(1,1),
    id_cliente INT,
    id_pedido INT,
    fecha DATE,
    monto_total DECIMAL(10,2),
    FOREIGN KEY (id_cliente) REFERENCES Clientes(id_cliente),
    FOREIGN KEY (id_pedido) REFERENCES Pedidos(id_pedido)
);

-- Tabla Caja
CREATE TABLE Caja (
    id_caja INT PRIMARY KEY IDENTITY(1,1),
    fecha DATE UNIQUE, -- una fila por día
    total_efectivo DECIMAL(10,2),
    total_tarjeta DECIMAL(10,2),
    total_transferencia DECIMAL(10,2)
    -- total_general puede calcularse como suma de los anteriores
);

-- Tabla Pagos
CREATE TABLE Pagos (
    id_pago INT PRIMARY KEY IDENTITY(1,1),
    id_pedido INT,
    metodo_pago VARCHAR(50), -- Efectivo, Tarjeta, Transferencia
    total DECIMAL(10,2),
    fecha_pago DATE,
    hora_pago TIME,
    id_caja INT,
    FOREIGN KEY (id_pedido) REFERENCES Pedidos(id_pedido),
    FOREIGN KEY (id_caja) REFERENCES Caja(id_caja)
);
