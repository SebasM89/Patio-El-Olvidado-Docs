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

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.EnsureCreatedAsync();

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
    }
}
