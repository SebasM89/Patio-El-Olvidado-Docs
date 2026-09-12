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
