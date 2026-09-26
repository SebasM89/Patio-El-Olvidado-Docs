using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Infrastructure.Persistence;

public static class DbSeeder
{
    public const string DevAdminEmail = "admin@patioelolvidado.local";
    public const string DevAdminPassword = "Admin123!";
    public const string DevEmpleadoEmail = "empleado@patioelolvidado.local";
    public const string DevEmpleadoPassword = "Empleado123!";
    public const string DevClienteEmail = "cliente@patioelolvidado.local";
    public const string DevClientePassword = "Cliente123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.EnsureCreatedAsync();

        // EnsureCreated no altera BD ya existente: crear Productos / Pedidos si faltan
        if (db.Database.IsSqlServer())
        {
            // Usuarios ya existe: solo check e índices que falten. No recrear UX_Usuarios_Email.
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.check_constraints
                        WHERE name = N'CK_Usuarios_Estado'
                          AND parent_object_id = OBJECT_ID(N'dbo.Usuarios'))
                        ALTER TABLE [dbo].[Usuarios] ADD CONSTRAINT [CK_Usuarios_Estado]
                            CHECK ([Estado] IN (N'Activo', N'Inactivo', N'Bloqueado'));

                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_Usuarios_RolId'
                          AND object_id = OBJECT_ID(N'dbo.Usuarios'))
                        CREATE INDEX [IX_Usuarios_RolId] ON [dbo].[Usuarios]([RolId]);

                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_Usuarios_Estado'
                          AND object_id = OBJECT_ID(N'dbo.Usuarios'))
                        CREATE INDEX [IX_Usuarios_Estado] ON [dbo].[Usuarios]([Estado]);
                END
                """);

            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Productos] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [Nombre] NVARCHAR(100) NOT NULL,
                        [Descripcion] NVARCHAR(500) NULL,
                        [Precio] DECIMAL(10,2) NOT NULL,
                        [Categoria] NVARCHAR(50) NOT NULL,
                        [Imagen] NVARCHAR(500) NULL,
                        [Etiquetas] NVARCHAR(300) NULL,
                        [Activo] BIT NOT NULL CONSTRAINT [DF_Productos_Activo] DEFAULT (1),
                        CONSTRAINT [PK_Productos] PRIMARY KEY ([Id])
                    );
                    CREATE INDEX [IX_Productos_Categoria] ON [Productos]([Categoria]);
                    CREATE INDEX [IX_Productos_Nombre] ON [Productos]([Nombre]);
                END
                """);

            // Pedidos canónicos; si hay esquema legado (id_pedido), recrear (dev)
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Pedidos', N'Id') IS NULL
                BEGIN
                    IF OBJECT_ID(N'dbo.DetallePedidos', N'U') IS NOT NULL
                        DROP TABLE [DetallePedidos];

                    IF OBJECT_ID(N'dbo.Envios', N'U') IS NOT NULL
                        DROP TABLE [Envios];
                    IF OBJECT_ID(N'dbo.Pagos', N'U') IS NOT NULL
                        DROP TABLE [Pagos];
                    IF OBJECT_ID(N'dbo.HistorialClientes', N'U') IS NOT NULL
                        DROP TABLE [HistorialClientes];

                    DROP TABLE [Pedidos];
                END

                IF OBJECT_ID(N'dbo.DetallePedidos', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DetallePedidos', N'PedidoId') IS NULL
                    DROP TABLE [DetallePedidos];

                IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Pedidos] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [Tipo] NVARCHAR(20) NOT NULL,
                        [Estado] NVARCHAR(30) NOT NULL,
                        [Subtotal] DECIMAL(10,2) NOT NULL,
                        [Total] DECIMAL(10,2) NOT NULL,
                        [ClienteId] INT NULL,
                        [VisitaContabilizada] BIT NOT NULL CONSTRAINT [DF_Pedidos_VisitaContabilizada] DEFAULT (0),
                        [CreadoPorUsuarioId] INT NOT NULL,
                        [FechaCreacion] DATETIME2 NOT NULL CONSTRAINT [DF_Pedidos_Fecha] DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Pedidos_Usuarios] FOREIGN KEY ([CreadoPorUsuarioId]) REFERENCES [Usuarios]([Id])
                    );
                    CREATE INDEX [IX_Pedidos_Estado] ON [Pedidos]([Estado]);
                    CREATE INDEX [IX_Pedidos_FechaCreacion] ON [Pedidos]([FechaCreacion]);
                END

                IF OBJECT_ID(N'dbo.DetallePedidos', N'U') IS NULL
                BEGIN
                    CREATE TABLE [DetallePedidos] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [PedidoId] INT NOT NULL,
                        [ProductoId] INT NOT NULL,
                        [Cantidad] INT NOT NULL,
                        [PrecioUnitario] DECIMAL(10,2) NOT NULL,
                        CONSTRAINT [PK_DetallePedidos] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_DetallePedidos_Pedidos] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_DetallePedidos_Productos] FOREIGN KEY ([ProductoId]) REFERENCES [Productos]([Id]),
                        CONSTRAINT [CK_DetallePedidos_Cantidad] CHECK ([Cantidad] > 0)
                    );
                END
                """);

            // Caja / Pagos canónicos; si hay esquema legado (id_caja / id_pago), recrear (dev)
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Caja', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Caja', N'Id') IS NULL
                BEGIN
                    IF OBJECT_ID(N'dbo.Pagos', N'U') IS NOT NULL
                        DROP TABLE [Pagos];
                    DROP TABLE [Caja];
                END

                IF OBJECT_ID(N'dbo.Pagos', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Pagos', N'Id') IS NULL
                    DROP TABLE [Pagos];

                IF OBJECT_ID(N'dbo.Caja', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Caja] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [Fecha] DATE NOT NULL,
                        [TotalEfectivo] DECIMAL(10,2) NOT NULL CONSTRAINT [DF_Caja_Efectivo] DEFAULT (0),
                        [TotalTarjeta] DECIMAL(10,2) NOT NULL CONSTRAINT [DF_Caja_Tarjeta] DEFAULT (0),
                        [TotalTransferencia] DECIMAL(10,2) NOT NULL CONSTRAINT [DF_Caja_Transferencia] DEFAULT (0),
                        CONSTRAINT [PK_Caja] PRIMARY KEY ([Id])
                    );
                    CREATE UNIQUE INDEX [IX_Caja_Fecha] ON [Caja]([Fecha]);
                END

                IF OBJECT_ID(N'dbo.Pagos', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Pagos] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [PedidoId] INT NOT NULL,
                        [Metodo] NVARCHAR(30) NOT NULL,
                        [Estado] NVARCHAR(30) NOT NULL,
                        [Monto] DECIMAL(10,2) NOT NULL,
                        [FechaPago] DATETIME2 NOT NULL CONSTRAINT [DF_Pagos_Fecha] DEFAULT (SYSUTCDATETIME()),
                        [CajaId] INT NOT NULL,
                        CONSTRAINT [PK_Pagos] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Pagos_Pedidos] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos]([Id]),
                        CONSTRAINT [FK_Pagos_Caja] FOREIGN KEY ([CajaId]) REFERENCES [Caja]([Id]),
                        CONSTRAINT [CK_Pagos_Monto] CHECK ([Monto] > 0)
                    );
                    CREATE INDEX [IX_Pagos_PedidoId] ON [Pagos]([PedidoId]);
                END
                """);

            // Clientes canónicos (RF-05) + VisitaContabilizada + FK Pedidos→Clientes
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Clientes', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Clientes', N'Id') IS NULL
                BEGIN
                    IF OBJECT_ID(N'dbo.HistorialClientes', N'U') IS NOT NULL
                        DROP TABLE [HistorialClientes];

                    IF OBJECT_ID(N'dbo.Reservaciones', N'U') IS NOT NULL
                        DROP TABLE [Reservaciones];

                    DROP TABLE [Clientes];
                END

                IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Clientes] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [Nombre] NVARCHAR(100) NOT NULL,
                        [Telefono] NVARCHAR(30) NOT NULL,
                        [Email] NVARCHAR(150) NULL,
                        [Visitas] INT NOT NULL CONSTRAINT [DF_Clientes_Visitas] DEFAULT (0),
                        [UsuarioId] INT NULL,
                        [Activo] BIT NOT NULL CONSTRAINT [DF_Clientes_Activo] DEFAULT (1),
                        CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Clientes_Usuarios] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id]) ON DELETE SET NULL,
                        CONSTRAINT [CK_Clientes_Visitas] CHECK ([Visitas] >= 0)
                    );
                    CREATE INDEX [IX_Clientes_Telefono] ON [Clientes]([Telefono]);
                    CREATE UNIQUE INDEX [IX_Clientes_Email] ON [Clientes]([Email]) WHERE [Email] IS NOT NULL;
                    CREATE UNIQUE INDEX [IX_Clientes_UsuarioId] ON [Clientes]([UsuarioId]) WHERE [UsuarioId] IS NOT NULL;
                END

                IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Pedidos', N'VisitaContabilizada') IS NULL
                BEGIN
                    ALTER TABLE [Pedidos] ADD [VisitaContabilizada] BIT NOT NULL
                        CONSTRAINT [DF_Pedidos_VisitaContabilizada] DEFAULT (0);
                END

                IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NOT NULL
                   AND OBJECT_ID(N'dbo.Clientes', N'U') IS NOT NULL
                   AND OBJECT_ID(N'dbo.FK_Pedidos_Clientes', N'F') IS NULL
                BEGIN
                    UPDATE [Pedidos]
                    SET [ClienteId] = NULL
                    WHERE [ClienteId] IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM [Clientes] c WHERE c.[Id] = [Pedidos].[ClienteId]);

                    ALTER TABLE [Pedidos] WITH CHECK
                    ADD CONSTRAINT [FK_Pedidos_Clientes]
                        FOREIGN KEY ([ClienteId]) REFERENCES [Clientes]([Id]) ON DELETE SET NULL;

                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_Pedidos_ClienteId' AND object_id = OBJECT_ID(N'dbo.Pedidos'))
                        CREATE INDEX [IX_Pedidos_ClienteId] ON [Pedidos]([ClienteId]);
                END

                -- HistorialClientes legado: no usar en EF; fuente de verdad = Pedidos.ClienteId
                IF OBJECT_ID(N'dbo.HistorialClientes', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.HistorialClientes', N'id_cliente') IS NOT NULL
                   AND COL_LENGTH(N'dbo.Clientes', N'Id') IS NOT NULL
                   AND COL_LENGTH(N'dbo.Clientes', N'id_cliente') IS NULL
                BEGIN
                    -- Esquema Clientes ya canónico: historial legado incompatible → deprecar tabla
                    DROP TABLE [HistorialClientes];
                END

                -- Empleados canónicos (RF-06): reemplaza legado snake_case + Turnos
                IF OBJECT_ID(N'dbo.Empleados', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Empleados', N'Id') IS NULL
                BEGIN
                    IF OBJECT_ID(N'dbo.Envios', N'U') IS NOT NULL
                        DROP TABLE [Envios];
                    IF OBJECT_ID(N'dbo.Turnos', N'U') IS NOT NULL
                        DROP TABLE [Turnos];
                    IF OBJECT_ID(N'dbo.Fichajes', N'U') IS NOT NULL
                        DROP TABLE [Fichajes];
                    IF OBJECT_ID(N'dbo.Liquidaciones', N'U') IS NOT NULL
                        DROP TABLE [Liquidaciones];
                    DROP TABLE [Empleados];
                END

                IF OBJECT_ID(N'dbo.Empleados', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Empleados] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [Nombre] NVARCHAR(100) NOT NULL,
                        [Puesto] NVARCHAR(100) NULL,
                        [Telefono] NVARCHAR(30) NULL,
                        [TarifaHora] DECIMAL(10,2) NOT NULL,
                        [HorasTrabajadas] DECIMAL(10,4) NOT NULL CONSTRAINT [DF_Empleados_Horas] DEFAULT (0),
                        [UsuarioId] INT NULL,
                        [Activo] BIT NOT NULL CONSTRAINT [DF_Empleados_Activo] DEFAULT (1),
                        CONSTRAINT [PK_Empleados] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Empleados_Usuarios] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id]) ON DELETE SET NULL,
                        CONSTRAINT [CK_Empleados_TarifaHora] CHECK ([TarifaHora] > 0),
                        CONSTRAINT [CK_Empleados_HorasTrabajadas] CHECK ([HorasTrabajadas] >= 0)
                    );
                    CREATE INDEX [IX_Empleados_Nombre] ON [Empleados]([Nombre]);
                    CREATE UNIQUE INDEX [IX_Empleados_UsuarioId] ON [Empleados]([UsuarioId]) WHERE [UsuarioId] IS NOT NULL;
                END

                IF OBJECT_ID(N'dbo.Fichajes', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Fichajes] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [EmpleadoId] INT NOT NULL,
                        [EntradaUtc] DATETIME2 NOT NULL,
                        [SalidaUtc] DATETIME2 NULL,
                        [Horas] DECIMAL(10,4) NULL,
                        CONSTRAINT [PK_Fichajes] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Fichajes_Empleados] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados]([Id])
                    );
                    CREATE INDEX [IX_Fichajes_EmpleadoId] ON [Fichajes]([EmpleadoId]);
                    CREATE INDEX [IX_Fichajes_EntradaUtc] ON [Fichajes]([EntradaUtc]);
                END

                IF OBJECT_ID(N'dbo.Liquidaciones', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Liquidaciones] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [EmpleadoId] INT NOT NULL,
                        [PeriodoDesde] DATE NOT NULL,
                        [PeriodoHasta] DATE NOT NULL,
                        [Horas] DECIMAL(10,4) NOT NULL,
                        [TarifaHoraSnapshot] DECIMAL(10,2) NOT NULL,
                        [Monto] DECIMAL(10,2) NOT NULL,
                        [GeneradaEnUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Liquidaciones_Generada] DEFAULT (SYSUTCDATETIME()),
                        [GeneradaPorUsuarioId] INT NOT NULL,
                        CONSTRAINT [PK_Liquidaciones] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Liquidaciones_Empleados] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados]([Id]),
                        CONSTRAINT [FK_Liquidaciones_Usuarios] FOREIGN KEY ([GeneradaPorUsuarioId]) REFERENCES [Usuarios]([Id]),
                        CONSTRAINT [CK_Liquidaciones_Horas] CHECK ([Horas] >= 0),
                        CONSTRAINT [CK_Liquidaciones_Monto] CHECK ([Monto] >= 0)
                    );
                    CREATE INDEX [IX_Liquidaciones_EmpleadoId] ON [Liquidaciones]([EmpleadoId]);
                END
                """);

            // Mesas / Reservas canónicas (CU08). No mapear Reservaciones legado.
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Mesas', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Mesas', N'Id') IS NULL
                BEGIN
                    IF OBJECT_ID(N'dbo.Reservaciones', N'U') IS NOT NULL
                        DROP TABLE [Reservaciones];
                    IF OBJECT_ID(N'dbo.Reservas', N'U') IS NOT NULL
                        DROP TABLE [Reservas];
                    DROP TABLE [Mesas];
                END

                IF OBJECT_ID(N'dbo.Mesas', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Mesas] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [Numero] INT NOT NULL,
                        [Capacidad] INT NOT NULL,
                        [Ubicacion] NVARCHAR(100) NULL,
                        CONSTRAINT [PK_Mesas] PRIMARY KEY ([Id]),
                        CONSTRAINT [CK_Mesas_Capacidad] CHECK ([Capacidad] > 0)
                    );
                    CREATE UNIQUE INDEX [IX_Mesas_Numero] ON [Mesas]([Numero]);
                END

                IF OBJECT_ID(N'dbo.Reservas', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Reservas] (
                        [Id] INT NOT NULL IDENTITY(1,1),
                        [ClienteId] INT NOT NULL,
                        [MesaId] INT NOT NULL,
                        [Fecha] DATE NOT NULL,
                        [HoraInicio] TIME NOT NULL,
                        [HoraFin] TIME NOT NULL,
                        [Personas] INT NOT NULL,
                        [Estado] NVARCHAR(30) NOT NULL,
                        [CreadoPorUsuarioId] INT NOT NULL,
                        [FechaCreacion] DATETIME2 NOT NULL CONSTRAINT [DF_Reservas_FechaCreacion] DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT [PK_Reservas] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Reservas_Clientes] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes]([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_Reservas_Mesas] FOREIGN KEY ([MesaId]) REFERENCES [Mesas]([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_Reservas_Usuarios] FOREIGN KEY ([CreadoPorUsuarioId]) REFERENCES [Usuarios]([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [CK_Reservas_HoraFin] CHECK ([HoraFin] > [HoraInicio]),
                        CONSTRAINT [CK_Reservas_Personas] CHECK ([Personas] > 0)
                    );
                    CREATE INDEX [IX_Reservas_MesaId_Fecha] ON [Reservas]([MesaId], [Fecha]);
                    CREATE INDEX [IX_Reservas_ClienteId] ON [Reservas]([ClienteId]);
                END
                """);

            // StockItems / MovimientosStock. Sin semilla. Sin FK a Productos.
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.StockItems', N'U') IS NULL
                BEGIN
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
                END

                IF OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NULL
                BEGIN
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
                END
                """);

            // Proveedores antes de la FK. Sin semilla. ALTER idempotente si MovimientosStock ya existe.
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Proveedores', N'U') IS NULL
                BEGIN
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
                END

                IF OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.MovimientosStock', N'ProveedorId') IS NULL
                    ALTER TABLE MovimientosStock ADD ProveedorId INT NULL;

                IF OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NOT NULL
                   AND OBJECT_ID(N'dbo.Proveedores', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.MovimientosStock', N'ProveedorId') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys
                        WHERE name = N'FK_MovimientosStock_Proveedores')
                    EXEC(N'
                        ALTER TABLE MovimientosStock ADD CONSTRAINT FK_MovimientosStock_Proveedores
                            FOREIGN KEY (ProveedorId) REFERENCES Proveedores(Id) ON DELETE NO ACTION;
                    ');

                IF OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.MovimientosStock', N'ProveedorId') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1 FROM sys.check_constraints
                        WHERE name = N'CK_MovimientosStock_ProveedorSoloEntrada'
                          AND parent_object_id = OBJECT_ID(N'dbo.MovimientosStock'))
                    EXEC(N'
                        ALTER TABLE MovimientosStock ADD CONSTRAINT CK_MovimientosStock_ProveedorSoloEntrada
                            CHECK (ProveedorId IS NULL OR Tipo = N''Entrada'');
                    ');

                IF OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.MovimientosStock', N'ProveedorId') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_MovimientosStock_ProveedorId'
                          AND object_id = OBJECT_ID(N'dbo.MovimientosStock'))
                    EXEC(N'
                        CREATE INDEX IX_MovimientosStock_ProveedorId
                            ON MovimientosStock(ProveedorId)
                            WHERE ProveedorId IS NOT NULL;
                    ');
                """);

            // Notificaciones. Sin semilla. No altera StockItems ni MovimientosStock.
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NULL
                   AND OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
                   AND OBJECT_ID(N'dbo.StockItems', N'U') IS NOT NULL
                   AND OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NOT NULL
                BEGIN
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
                END
                """);

            // Promociones. Sin FKs, sin semilla, sin ROWVERSION. No altera otras tablas.
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Promociones', N'U') IS NULL
                BEGIN
                    CREATE TABLE Promociones (
                        Id INT NOT NULL IDENTITY(1,1),
                        Nombre NVARCHAR(100) NOT NULL,
                        Descripcion NVARCHAR(500) NOT NULL,
                        VigenteDesde DATE NOT NULL,
                        VigenteHasta DATE NOT NULL,
                        Activo BIT NOT NULL CONSTRAINT DF_Promociones_Activo DEFAULT (1),
                        CONSTRAINT PK_Promociones PRIMARY KEY (Id),
                        CONSTRAINT CK_Promociones_Vigencia CHECK (VigenteHasta >= VigenteDesde)
                    );
                    CREATE INDEX IX_Promociones_Activo_Vigencia ON Promociones(Activo, VigenteDesde, VigenteHasta);
                END
                """);
        }

        if (!await db.Mesas.AnyAsync())
        {
            db.Mesas.AddRange(
                new Mesa { Numero = 1, Capacidad = 2 },
                new Mesa { Numero = 2, Capacidad = 4 },
                new Mesa { Numero = 3, Capacidad = 4 },
                new Mesa { Numero = 4, Capacidad = 6 });
            await db.SaveChangesAsync();
            logger.LogInformation("Mesas seed aplicadas (numeros 1-4, capacidades 2, 4, 4, 6)");
        }

        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Rol { Nombre = RolesSistema.Admin, Descripcion = "Administrador del sistema" },
                new Rol { Nombre = RolesSistema.Empleado, Descripcion = "Empleado del restaurante" },
                new Rol { Nombre = RolesSistema.Cliente, Descripcion = "Cliente" });
            await db.SaveChangesAsync();
            logger.LogInformation("Roles seed aplicados: Admin, Empleado, Cliente");
        }

        if (!await db.Permisos.AnyAsync())
        {
            db.Permisos.AddRange(
                new Permiso { Codigo = "auth.login", Nombre = "Iniciar sesión" },
                new Permiso { Codigo = "menu.manage", Nombre = "Gestionar menú" },
                new Permiso { Codigo = "pedidos.manage", Nombre = "Gestionar pedidos" });
            await db.SaveChangesAsync();

            var admin = await db.Roles.FirstAsync(r => r.Nombre == RolesSistema.Admin);
            var permisos = await db.Permisos.ToListAsync();
            foreach (var permiso in permisos)
            {
                db.RolPermisos.Add(new RolPermiso { RolId = admin.Id, PermisoId = permiso.Id });
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Permisos seed aplicados");
        }

        var adminEmail = (config["Seed:AdminEmail"] ?? DevAdminEmail).Trim().ToLowerInvariant();
        var adminPassword = config["Seed:AdminPassword"] ?? DevAdminPassword;

        if (env.IsDevelopment() && !await db.Usuarios.AnyAsync(u => u.Email == adminEmail))
        {
            var adminRol = await db.Roles.FirstAsync(r => r.Nombre == RolesSistema.Admin);
            db.Usuarios.Add(new Usuario
            {
                Nombre = "Administrador Dev",
                Email = adminEmail,
                PasswordHash = hasher.Hash(adminPassword),
                RolId = adminRol.Id,
                Estado = UsuarioEstado.Activo
            });
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Usuario Admin Development creado: {Email} / password documentado en appsettings.Development.json",
                adminEmail);
        }

        if (env.IsDevelopment() && !await db.Usuarios.AnyAsync(u => u.Email == DevEmpleadoEmail))
        {
            var empleadoRol = await db.Roles.FirstAsync(r => r.Nombre == RolesSistema.Empleado);
            db.Usuarios.Add(new Usuario
            {
                Nombre = "Empleado Dev",
                Email = DevEmpleadoEmail,
                PasswordHash = hasher.Hash(DevEmpleadoPassword),
                RolId = empleadoRol.Id,
                Estado = UsuarioEstado.Activo
            });
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Usuario Empleado Development creado: {Email} (para validar RN-03)",
                DevEmpleadoEmail);
        }

        if (env.IsDevelopment() && !await db.Usuarios.AnyAsync(u => u.Email == DevClienteEmail))
        {
            var clienteRol = await db.Roles.FirstAsync(r => r.Nombre == RolesSistema.Cliente);
            db.Usuarios.Add(new Usuario
            {
                Nombre = "Cliente Dev",
                Email = DevClienteEmail,
                PasswordHash = hasher.Hash(DevClientePassword),
                RolId = clienteRol.Id,
                Estado = UsuarioEstado.Activo
            });
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Usuario Cliente Development creado: {Email} (para validar ownership de pedidos)",
                DevClienteEmail);
        }

        if (env.IsDevelopment() && !await db.Clientes.AnyAsync())
        {
            var usuarioCliente = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == DevClienteEmail);
            db.Clientes.AddRange(
                new Cliente
                {
                    Nombre = "Cliente Dev",
                    Telefono = "1111111111",
                    Email = DevClienteEmail,
                    Visitas = 0,
                    UsuarioId = usuarioCliente?.Id,
                    Activo = true
                },
                new Cliente
                {
                    Nombre = "Walk-in Mostrador",
                    Telefono = "2222222222",
                    Email = null,
                    Visitas = 4,
                    UsuarioId = null,
                    Activo = true
                });
            await db.SaveChangesAsync();
            logger.LogInformation("Clientes seed Development aplicados (perfil vinculado + walk-in con Visitas=4 para RN-05)");
        }

        if (env.IsDevelopment() && !await db.Empleados.AnyAsync())
        {
            var usuarioEmpleado = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == DevEmpleadoEmail);
            db.Empleados.Add(new Empleado
            {
                Nombre = "Empleado Dev",
                Puesto = "Mozo",
                Telefono = "3333333333",
                TarifaHora = 2500m,
                HorasTrabajadas = 0m,
                UsuarioId = usuarioEmpleado?.Id,
                Activo = true
            });
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Empleado seed Development aplicado (perfil vinculado a {Email} para RF-06 / RN-07)",
                DevEmpleadoEmail);
        }

        if (env.IsDevelopment() && !await db.Productos.AnyAsync())
        {
            db.Productos.AddRange(
                new Producto
                {
                    Nombre = "Empanada de carne",
                    Descripcion = "Empanada criolla al horno",
                    Precio = 1200m,
                    Categoria = "Entradas",
                    Imagen = "https://placehold.co/400x300?text=Empanada",
                    Etiquetas = "clasico,horno",
                    Activo = true
                },
                new Producto
                {
                    Nombre = "Milanesa napolitana",
                    Descripcion = "Con papas fritas",
                    Precio = 8500m,
                    Categoria = "Platos",
                    Imagen = "https://placehold.co/400x300?text=Milanesa",
                    Etiquetas = "clasico",
                    Activo = true
                },
                new Producto
                {
                    Nombre = "Limonada",
                    Descripcion = "Natural con menta",
                    Precio = 2500m,
                    Categoria = "Bebidas",
                    Imagen = "https://placehold.co/400x300?text=Limonada",
                    Etiquetas = "fresco,sin alcohol",
                    Activo = true
                });
            await db.SaveChangesAsync();
            logger.LogInformation("Productos seed Development aplicados (3 ítems)");
        }
    }
}
