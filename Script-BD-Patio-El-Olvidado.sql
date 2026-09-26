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
    Email NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    RolId INT NOT NULL,
    Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_Usuarios_Estado DEFAULT (N'Activo'),
    UltimoAcceso DATETIME2 NULL,
    IntentosFallidos INT NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT (0),
    BloqueadoHasta DATETIME2 NULL,
    CONSTRAINT UX_Usuarios_Email UNIQUE (Email),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES Roles(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Usuarios_Estado CHECK (Estado IN (N'Activo', N'Inactivo', N'Bloqueado'))
);

CREATE INDEX IX_Usuarios_RolId ON Usuarios(RolId);
CREATE INDEX IX_Usuarios_Estado ON Usuarios(Estado);

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

-- Tabla Clientes (RF-05 / CU05 ? modelo can?nico; reemplaza legado id_cliente)
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

-- Tabla Mesas (CU08; catalogo fijo, sin Activo ni CRUD)
CREATE TABLE Mesas (
    Id INT NOT NULL IDENTITY(1,1),
    Numero INT NOT NULL,
    Capacidad INT NOT NULL,
    Ubicacion NVARCHAR(100) NULL,
    CONSTRAINT PK_Mesas PRIMARY KEY (Id),
    CONSTRAINT CK_Mesas_Capacidad CHECK (Capacidad > 0)
);
CREATE UNIQUE INDEX IX_Mesas_Numero ON Mesas(Numero);

INSERT INTO Mesas (Numero, Capacidad) VALUES
(1, 2),
(2, 4),
(3, 4),
(4, 6);

-- Tabla Empleados (RF-06 / CU06 ? modelo can?nico; reemplaza legado snake_case)
CREATE TABLE Empleados (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Puesto NVARCHAR(100) NULL,
    Telefono NVARCHAR(30) NULL,
    TarifaHora DECIMAL(10,2) NOT NULL,
    HorasTrabajadas DECIMAL(10,4) NOT NULL CONSTRAINT DF_Empleados_Horas DEFAULT (0),
    UsuarioId INT NULL,                       -- opcional: usuario rol Empleado
    Activo BIT NOT NULL CONSTRAINT DF_Empleados_Activo DEFAULT (1),
    CONSTRAINT FK_Empleados_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE SET NULL,
    CONSTRAINT CK_Empleados_TarifaHora CHECK (TarifaHora > 0),
    CONSTRAINT CK_Empleados_HorasTrabajadas CHECK (HorasTrabajadas >= 0)
);
CREATE INDEX IX_Empleados_Nombre ON Empleados(Nombre);
CREATE UNIQUE INDEX IX_Empleados_UsuarioId ON Empleados(UsuarioId) WHERE UsuarioId IS NOT NULL;

-- Tabla Fichajes (RF-06 ? sustituye sem?ntica de Turnos legado)
CREATE TABLE Fichajes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EmpleadoId INT NOT NULL,
    EntradaUtc DATETIME2 NOT NULL,
    SalidaUtc DATETIME2 NULL,
    Horas DECIMAL(10,4) NULL,
    CONSTRAINT FK_Fichajes_Empleados FOREIGN KEY (EmpleadoId) REFERENCES Empleados(Id)
);
CREATE INDEX IX_Fichajes_EmpleadoId ON Fichajes(EmpleadoId);
CREATE INDEX IX_Fichajes_EntradaUtc ON Fichajes(EntradaUtc);

-- Tabla Liquidaciones (RF-06 ? c?lculo interno MVP; sin AFIP/PDF)
CREATE TABLE Liquidaciones (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EmpleadoId INT NOT NULL,
    PeriodoDesde DATE NOT NULL,
    PeriodoHasta DATE NOT NULL,
    Horas DECIMAL(10,4) NOT NULL,
    TarifaHoraSnapshot DECIMAL(10,2) NOT NULL,
    Monto DECIMAL(10,2) NOT NULL,
    GeneradaEnUtc DATETIME2 NOT NULL CONSTRAINT DF_Liquidaciones_Generada DEFAULT (SYSUTCDATETIME()),
    GeneradaPorUsuarioId INT NOT NULL,
    CONSTRAINT FK_Liquidaciones_Empleados FOREIGN KEY (EmpleadoId) REFERENCES Empleados(Id),
    CONSTRAINT FK_Liquidaciones_Usuarios FOREIGN KEY (GeneradaPorUsuarioId) REFERENCES Usuarios(Id),
    CONSTRAINT CK_Liquidaciones_Horas CHECK (Horas >= 0),
    CONSTRAINT CK_Liquidaciones_Monto CHECK (Monto >= 0)
);
CREATE INDEX IX_Liquidaciones_EmpleadoId ON Liquidaciones(EmpleadoId);

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

-- Tabla Pedidos (RF-03 / CU03 ? modelo can?nico; RF-05 FK Cliente)
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

-- Tabla Reservas (CU08). No crear Reservaciones: legado, no mapear en EF.
CREATE TABLE Reservas (
    Id INT NOT NULL IDENTITY(1,1),
    ClienteId INT NOT NULL,
    MesaId INT NOT NULL,
    Fecha DATE NOT NULL,                         -- dia civil, no UTC
    HoraInicio TIME NOT NULL,
    HoraFin TIME NOT NULL,
    Personas INT NOT NULL,
    Estado NVARCHAR(30) NOT NULL,                -- Confirmada | Cancelada | Finalizada
    CreadoPorUsuarioId INT NOT NULL,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Reservas_FechaCreacion DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Reservas PRIMARY KEY (Id),
    CONSTRAINT FK_Reservas_Clientes FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Reservas_Mesas FOREIGN KEY (MesaId) REFERENCES Mesas(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Reservas_Usuarios FOREIGN KEY (CreadoPorUsuarioId) REFERENCES Usuarios(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Reservas_HoraFin CHECK (HoraFin > HoraInicio),
    CONSTRAINT CK_Reservas_Personas CHECK (Personas > 0)
);
CREATE INDEX IX_Reservas_MesaId_Fecha ON Reservas(MesaId, Fecha);
CREATE INDEX IX_Reservas_ClienteId ON Reservas(ClienteId);

-- Tabla Turnos (LEGADO ? no usar en EF; reemplazado por Fichajes RF-06)
CREATE TABLE Turnos (
    id_turno INT PRIMARY KEY IDENTITY(1,1),
    id_empleado INT,
    fecha DATE,
    hora_entrada TIME,
    hora_salida TIME
    -- FK legado a Empleados.id_empleado: omitida tras canonicizar Empleados
);

-- Tabla Envios (LEGADO ? delivery fuera de alcance MVP; no usar en EF)
CREATE TABLE Envios (
    id_envio INT PRIMARY KEY IDENTITY(1,1),
    id_pedido INT,
    direccion_entrega VARCHAR(255),
    fecha_envio DATE,
    hora_envio TIME,
    id_empleado INT, -- repartidor (legado)
    estado BIT, -- 1 en camino, 0 entregado
    FOREIGN KEY (id_pedido) REFERENCES Pedidos(Id)
);

-- Tabla HistorialClientes (LEGADO ? no usar en EF)
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

-- Tabla StockItems (Inventario). Sin FK a Productos. Sin semilla.
CREATE TABLE StockItems (
    Id INT NOT NULL IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    Unidad NVARCHAR(10) NOT NULL,
    CantidadActual DECIMAL(12,3) NOT NULL CONSTRAINT DF_StockItems_CantidadActual DEFAULT (0),
    StockMinimo DECIMAL(12,3) NOT NULL CONSTRAINT DF_StockItems_StockMinimo DEFAULT (0),
    Activo BIT NOT NULL CONSTRAINT DF_StockItems_Activo DEFAULT (1),
    Version ROWVERSION NOT NULL,
    CONSTRAINT PK_StockItems PRIMARY KEY (Id),
    CONSTRAINT CK_StockItems_Unidad CHECK (Unidad IN (N'Unidad', N'Kg', N'L')),
    CONSTRAINT CK_StockItems_CantidadActual CHECK (CantidadActual >= 0),
    CONSTRAINT CK_StockItems_StockMinimo CHECK (StockMinimo >= 0)
);
CREATE UNIQUE INDEX UX_StockItems_Nombre ON StockItems(Nombre);
CREATE INDEX IX_StockItems_Activo ON StockItems(Activo);

-- Tabla MovimientosStock (Inventario). Append-only. Sin semilla.
CREATE TABLE MovimientosStock (
    Id INT NOT NULL IDENTITY(1,1),
    StockItemId INT NOT NULL,
    Tipo NVARCHAR(10) NOT NULL,
    Cantidad DECIMAL(12,3) NOT NULL,
    Motivo NVARCHAR(200) NULL,
    FechaUtc DATETIME2 NOT NULL CONSTRAINT DF_MovimientosStock_FechaUtc DEFAULT (SYSUTCDATETIME()),
    RegistradoPorUsuarioId INT NOT NULL,
    CONSTRAINT PK_MovimientosStock PRIMARY KEY (Id),
    CONSTRAINT FK_MovimientosStock_StockItems FOREIGN KEY (StockItemId) REFERENCES StockItems(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_MovimientosStock_Usuarios FOREIGN KEY (RegistradoPorUsuarioId) REFERENCES Usuarios(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_MovimientosStock_Tipo CHECK (Tipo IN (N'Entrada', N'Salida')),
    CONSTRAINT CK_MovimientosStock_Cantidad CHECK (Cantidad > 0)
);
CREATE INDEX IX_MovimientosStock_StockItemId_FechaUtc ON MovimientosStock(StockItemId, FechaUtc DESC);

-- Tabla Proveedores. Sin semilla, sin CUIT, sin ROWVERSION.
-- Se crea antes de la FK de MovimientosStock.
CREATE TABLE Proveedores (
    Id INT NOT NULL IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Contacto NVARCHAR(100) NULL,
    Telefono NVARCHAR(30) NULL,
    Email NVARCHAR(150) NULL,
    Notas NVARCHAR(300) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_Proveedores_Activo DEFAULT (1),
    CONSTRAINT PK_Proveedores PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX UX_Proveedores_Nombre ON Proveedores(Nombre);
CREATE INDEX IX_Proveedores_Activo ON Proveedores(Activo);

-- ProveedorId no está en el CREATE publicado de MovimientosStock.
-- El ALTER es seguro en una base nueva (COL_LENGTH) y en una que ya tiene la tabla.
IF COL_LENGTH(N'dbo.MovimientosStock', N'ProveedorId') IS NULL
    ALTER TABLE MovimientosStock ADD ProveedorId INT NULL;

-- EXEC: la columna se agrega en este mismo batch; el enlace del nombre no puede ser estático.
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_MovimientosStock_Proveedores')
    EXEC(N'
        ALTER TABLE MovimientosStock ADD CONSTRAINT FK_MovimientosStock_Proveedores
            FOREIGN KEY (ProveedorId) REFERENCES Proveedores(Id) ON DELETE NO ACTION;
    ');

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_MovimientosStock_ProveedorSoloEntrada'
      AND parent_object_id = OBJECT_ID(N'dbo.MovimientosStock'))
    EXEC(N'
        ALTER TABLE MovimientosStock ADD CONSTRAINT CK_MovimientosStock_ProveedorSoloEntrada
            CHECK (ProveedorId IS NULL OR Tipo = N''Entrada'');
    ');

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_MovimientosStock_ProveedorId'
      AND object_id = OBJECT_ID(N'dbo.MovimientosStock'))
    EXEC(N'
        CREATE INDEX IX_MovimientosStock_ProveedorId
            ON MovimientosStock(ProveedorId)
            WHERE ProveedorId IS NOT NULL;
    ');

-- Tabla Notificaciones. Sin semilla, sin ROWVERSION.
-- No altera StockItems ni MovimientosStock.
CREATE TABLE Notificaciones (
    Id INT NOT NULL IDENTITY(1,1),
    UsuarioId INT NOT NULL,
    Titulo NVARCHAR(120) NOT NULL,
    Mensaje NVARCHAR(500) NOT NULL,
    Tipo NVARCHAR(20) NOT NULL,
    Leida BIT NOT NULL CONSTRAINT DF_Notificaciones_Leida DEFAULT (0),
    LeidaUtc DATETIME2 NULL,
    FechaUtc DATETIME2 NOT NULL CONSTRAINT DF_Notificaciones_FechaUtc DEFAULT (SYSUTCDATETIME()),
    StockItemId INT NOT NULL,
    MovimientoStockId INT NOT NULL,
    CONSTRAINT PK_Notificaciones PRIMARY KEY (Id),
    CONSTRAINT FK_Notificaciones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Notificaciones_StockItems FOREIGN KEY (StockItemId) REFERENCES StockItems(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Notificaciones_MovimientosStock FOREIGN KEY (MovimientoStockId) REFERENCES MovimientosStock(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Notificaciones_Tipo CHECK (Tipo IN (N'StockAlerta'))
);
CREATE INDEX IX_Notificaciones_UsuarioId_FechaUtc ON Notificaciones(UsuarioId, FechaUtc DESC);
CREATE INDEX IX_Notificaciones_UsuarioId_NoLeidas ON Notificaciones(UsuarioId) WHERE Leida = 0;
CREATE UNIQUE INDEX UX_Notificaciones_Usuario_Movimiento ON Notificaciones(UsuarioId, MovimientoStockId);
