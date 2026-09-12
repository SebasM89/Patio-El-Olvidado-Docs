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
                """);
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
