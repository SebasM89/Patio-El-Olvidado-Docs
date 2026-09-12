CREATE DATABASE ElOlvidado;
GO

USE ElOlvidado;
GO

-- =========================
-- Autenticaci?n (MVP)
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

-- Tabla Clientes (RF-05 / CU05  modelo cannico; reemplaza legado id_cliente)
CREATE TABLE Clientes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Telefono NVARCHAR(30) NOT NULL,
    Email NVARCHAR(150) NULL,
    Visitas INT NOT NULL CONSTRAINT DF_Clientes_Visitas DEFAULT (0),
    UsuarioId INT NULL,                       -- opcional: usuario rol Cliente
    Activo BIT NOT NULL CONSTRAINT DF_Clientes_Activo DEFAULT (1),
    CONSTRAINT FK_Clientes_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE SET NULL,
    CONSTRAINT CK_Clientes_Visitas CHECK (Visitas >= 0)
);
CREATE INDEX IX_Clientes_Telefono ON Clientes(Telefono);
CREATE UNIQUE INDEX IX_Clientes_Email ON Clientes(Email) WHERE Email IS NOT NULL;
CREATE UNIQUE INDEX IX_Clientes_UsuarioId ON Clientes(UsuarioId) WHERE UsuarioId IS NOT NULL;

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

-- Tabla Productos (RF-02 / CU02 - reemplaza legado Menus)
CREATE TABLE Productos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    Precio DECIMAL(10,2) NOT NULL,
    Categoria NVARCHAR(50) NOT NULL,
    Imagen NVARCHAR(500) NULL,
    Etiquetas NVARCHAR(300) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_Productos_Activo DEFAULT (1)
);

CREATE INDEX IX_Productos_Categoria ON Productos(Categoria);
CREATE INDEX IX_Productos_Nombre ON Productos(Nombre);

-- Tabla Pedidos (RF-03 / CU03  modelo cannico; RF-05 FK Cliente)
CREATE TABLE Pedidos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Tipo NVARCHAR(20) NOT NULL,              -- Local | ParaLlevar
    Estado NVARCHAR(30) NOT NULL,            -- EnPreparacion | Listo | Entregado | Cancelado
    Subtotal DECIMAL(10,2) NOT NULL,
    Total DECIMAL(10,2) NOT NULL,
    ClienteId INT NULL,
    VisitaContabilizada BIT NOT NULL CONSTRAINT DF_Pedidos_VisitaContabilizada DEFAULT (0),
    CreadoPorUsuarioId INT NOT NULL,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Pedidos_Fecha DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_Pedidos_Usuarios FOREIGN KEY (CreadoPorUsuarioId) REFERENCES Usuarios(Id),
    CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE SET NULL
);

CREATE INDEX IX_Pedidos_Estado ON Pedidos(Estado);
CREATE INDEX IX_Pedidos_FechaCreacion ON Pedidos(FechaCreacion);
CREATE INDEX IX_Pedidos_ClienteId ON Pedidos(ClienteId);

-- Tabla DetallePedidos
CREATE TABLE DetallePedidos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PedidoId INT NOT NULL,
    ProductoId INT NOT NULL,
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_DetallePedidos_Pedidos FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE,
    CONSTRAINT FK_DetallePedidos_Productos FOREIGN KEY (ProductoId) REFERENCES Productos(Id),
    CONSTRAINT CK_DetallePedidos_Cantidad CHECK (Cantidad > 0)
);

-- Tabla Reservaciones (legado MVP futuro; FK a Clientes cannico)
CREATE TABLE Reservaciones (
    id_reservacion INT PRIMARY KEY IDENTITY(1,1),
    id_cliente INT,
    id_mesa INT,
    fecha DATE,
    hora_inicio TIME,
    hora_fin TIME,
    estado VARCHAR(50), -- confirmada, cancelada, finalizada
    FOREIGN KEY (id_cliente) REFERENCES Clientes(Id),
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
    FOREIGN KEY (id_pedido) REFERENCES Pedidos(Id),
    FOREIGN KEY (id_empleado) REFERENCES Empleados(id_empleado)
);

-- Tabla HistorialClientes (LEGADO — no usar en EF)
-- Fuente de verdad del historial de consumo (CU09) = Pedidos con ClienteId.
-- Se mantiene solo por compatibilidad de scripts antiguos; no escribir desde la API.
CREATE TABLE HistorialClientes (
    id_historial INT PRIMARY KEY IDENTITY(1,1),
    id_cliente INT,
    id_pedido INT,
    fecha DATE,
    monto_total DECIMAL(10,2),
    FOREIGN KEY (id_cliente) REFERENCES Clientes(Id),
    FOREIGN KEY (id_pedido) REFERENCES Pedidos(Id)
);

-- Tabla Caja (RN-08: un registro por dia UTC; totales por metodo)
CREATE TABLE Caja (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Fecha DATE NOT NULL,
    TotalEfectivo DECIMAL(10,2) NOT NULL CONSTRAINT DF_Caja_Efectivo DEFAULT (0),
    TotalTarjeta DECIMAL(10,2) NOT NULL CONSTRAINT DF_Caja_Tarjeta DEFAULT (0),
    TotalTransferencia DECIMAL(10,2) NOT NULL CONSTRAINT DF_Caja_Transferencia DEFAULT (0)
);
CREATE UNIQUE INDEX IX_Caja_Fecha ON Caja(Fecha);

-- Tabla Pagos (RF-04 / CU04: cobros parciales / division de cuenta)
CREATE TABLE Pagos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PedidoId INT NOT NULL,
    Metodo NVARCHAR(30) NOT NULL,            -- Efectivo | Tarjeta | Transferencia
    Estado NVARCHAR(30) NOT NULL,            -- Completado | Anulado
    Monto DECIMAL(10,2) NOT NULL,
    FechaPago DATETIME2 NOT NULL CONSTRAINT DF_Pagos_Fecha DEFAULT (SYSUTCDATETIME()),
    CajaId INT NOT NULL,
    CONSTRAINT FK_Pagos_Pedidos FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id),
    CONSTRAINT FK_Pagos_Caja FOREIGN KEY (CajaId) REFERENCES Caja(Id),
    CONSTRAINT CK_Pagos_Monto CHECK (Monto > 0)
);
CREATE INDEX IX_Pagos_PedidoId ON Pagos(PedidoId);
