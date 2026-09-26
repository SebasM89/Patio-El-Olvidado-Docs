using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Usuarios;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;
using PatioElOlvidado.Infrastructure.Security;

namespace PatioElOlvidado.Application.Tests;

public class UsuarioServiceTests
{
    private static (
        UsuarioService Service,
        AppDbContext Db,
        BcryptPasswordHasher Hasher,
        Rol Admin,
        Rol Empleado,
        Rol Cliente) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var admin = new Rol { Nombre = RolesSistema.Admin, Descripcion = "Admin" };
        var empleado = new Rol { Nombre = RolesSistema.Empleado, Descripcion = "Empleado" };
        var cliente = new Rol { Nombre = RolesSistema.Cliente, Descripcion = "Cliente" };
        db.Roles.AddRange(admin, empleado, cliente);
        db.SaveChanges();

        var hasher = new BcryptPasswordHasher();
        var service = new UsuarioService(
            new UsuarioRepository(db),
            new RolRepository(db),
            new ClienteRepository(db),
            new EmpleadoRepository(db),
            new RefreshTokenRepository(db),
            hasher,
            new UnitOfWork(db));

        return (service, db, hasher, admin, empleado, cliente);
    }

    [Fact]
    public async Task Create_NormalizaEmail_HasheaPassword_DtoSinHash()
    {
        var (service, db, hasher, admin, _, _) = CreateSut();
        const string plain = "ClaveSegura1";

        var dto = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = " Ana ",
            Email = " Ana@TEST.local ",
            Password = plain,
            RolId = admin.Id
        });

        Assert.Equal("Ana", dto.Nombre);
        Assert.Equal("ana@test.local", dto.Email);
        Assert.Equal(UsuarioEstado.Activo, dto.Estado);
        Assert.Equal(RolesSistema.Admin, dto.RolNombre);
        Assert.Null(typeof(UsuarioDto).GetProperty("PasswordHash"));
        Assert.Null(typeof(UsuarioDto).GetProperty("Password"));

        var json = JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(plain, json, StringComparison.Ordinal);

        var stored = await db.Usuarios.FirstAsync(u => u.Id == dto.Id);
        Assert.NotEqual(plain, stored.PasswordHash);
        Assert.StartsWith("$2", stored.PasswordHash, StringComparison.Ordinal);
        Assert.True(hasher.Verify(plain, stored.PasswordHash));
        Assert.DoesNotContain(stored.PasswordHash, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_EmailDuplicado_IgnoraMayusculas_409()
    {
        var (service, _, _, admin, _, _) = CreateSut();
        await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Uno",
            Email = "dup@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Dos",
            Email = "DUP@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Update_PasswordOpcional_NoPisaHashSiVieneVacia()
    {
        var (service, db, hasher, admin, _, _) = CreateSut();
        var created = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Ana",
            Email = "ana@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });
        var hashOriginal = (await db.Usuarios.FirstAsync(u => u.Id == created.Id)).PasswordHash;

        var updated = await service.UpdateAsync(created.Id, new UpdateUsuarioRequest
        {
            Nombre = "Ana Edit",
            Email = "ana@test.local",
            RolId = admin.Id,
            Password = "   "
        });

        var stored = await db.Usuarios.FirstAsync(u => u.Id == created.Id);
        Assert.Equal(hashOriginal, stored.PasswordHash);
        Assert.Equal("Ana Edit", updated.Nombre);
        Assert.True(hasher.Verify("ClaveSegura1", stored.PasswordHash));

        await service.UpdateAsync(created.Id, new UpdateUsuarioRequest
        {
            Nombre = "Ana Edit",
            Email = "ana@test.local",
            RolId = admin.Id,
            Password = "OtraClave99"
        });

        stored = await db.Usuarios.FirstAsync(u => u.Id == created.Id);
        Assert.NotEqual(hashOriginal, stored.PasswordHash);
        Assert.True(hasher.Verify("OtraClave99", stored.PasswordHash));
        Assert.False(hasher.Verify("ClaveSegura1", stored.PasswordHash));
    }

    [Fact]
    public async Task Update_VinculadoACliente_CambioDeRol_409()
    {
        var (service, db, _, admin, _, clienteRol) = CreateSut();
        var created = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Clien",
            Email = "clien@test.local",
            Password = "ClaveSegura1",
            RolId = clienteRol.Id
        });
        db.Clientes.Add(new Cliente
        {
            Nombre = "Ficha",
            Telefono = "111",
            UsuarioId = created.Id,
            Activo = true
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(created.Id, new UpdateUsuarioRequest
        {
            Nombre = "Clien",
            Email = "clien@test.local",
            RolId = admin.Id
        }));

        Assert.Equal(409, ex.StatusCode);
        var stored = await db.Usuarios.FirstAsync(u => u.Id == created.Id);
        Assert.Equal(clienteRol.Id, stored.RolId);
    }

    [Fact]
    public async Task Update_VinculadoAEmpleado_CambioDeRol_409()
    {
        var (service, db, _, admin, empleadoRol, _) = CreateSut();
        var created = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Emp",
            Email = "emp@test.local",
            Password = "ClaveSegura1",
            RolId = empleadoRol.Id
        });
        db.Empleados.Add(new Empleado
        {
            Nombre = "Ficha",
            TarifaHora = 1000m,
            UsuarioId = created.Id,
            Activo = true
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(created.Id, new UpdateUsuarioRequest
        {
            Nombre = "Emp",
            Email = "emp@test.local",
            RolId = admin.Id
        }));

        Assert.Equal(409, ex.StatusCode);
        var stored = await db.Usuarios.Include(u => u.Rol).FirstAsync(u => u.Id == created.Id);
        Assert.Equal(RolesSistema.Empleado, stored.Rol.Nombre);
    }

    [Fact]
    public async Task Update_VinculadoACliente_MismoRol_Ok()
    {
        var (service, db, _, _, _, clienteRol) = CreateSut();
        var created = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Clien",
            Email = "mismo@test.local",
            Password = "ClaveSegura1",
            RolId = clienteRol.Id
        });
        db.Clientes.Add(new Cliente
        {
            Nombre = "Ficha",
            Telefono = "222",
            UsuarioId = created.Id,
            Activo = true
        });
        await db.SaveChangesAsync();

        var updated = await service.UpdateAsync(created.Id, new UpdateUsuarioRequest
        {
            Nombre = "Clien Edit",
            Email = "mismo@test.local",
            RolId = clienteRol.Id
        });

        Assert.Equal("Clien Edit", updated.Nombre);
        Assert.Equal(clienteRol.Id, updated.RolId);
    }

    [Fact]
    public async Task CambiarEstado_PropioAdmin_400()
    {
        var (service, db, _, admin, _, _) = CreateSut();
        var self = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Yo",
            Email = "yo@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });
        await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Otro",
            Email = "otro@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CambiarEstadoAsync(
            self.Id,
            new CambiarEstadoUsuarioRequest { Estado = UsuarioEstado.Inactivo },
            self.Id));

        Assert.Equal(400, ex.StatusCode);
        var stored = await db.Usuarios.FirstAsync(u => u.Id == self.Id);
        Assert.Equal(UsuarioEstado.Activo, stored.Estado);
    }

    [Fact]
    public async Task CambiarEstado_UltimoAdminActivo_400_NoRevoca()
    {
        var (service, db, _, admin, _, _) = CreateSut();
        var unico = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Unico",
            Email = "unico@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = unico.Id,
            Token = "activo",
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CambiarEstadoAsync(
            unico.Id,
            new CambiarEstadoUsuarioRequest { Estado = "Inactivo" },
            actorUsuarioId: 999));

        Assert.Equal(400, ex.StatusCode);
        var stored = await db.Usuarios.FirstAsync(u => u.Id == unico.Id);
        Assert.Equal(UsuarioEstado.Activo, stored.Estado);
        var token = await db.RefreshTokens.FirstAsync(t => t.UsuarioId == unico.Id);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public async Task CambiarEstado_Inactivo_RevocaSoloTokensActivos()
    {
        var (service, db, _, admin, empleadoRol, _) = CreateSut();
        await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Admin",
            Email = "admin@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });
        var target = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Emple",
            Email = "emple@test.local",
            Password = "ClaveSegura1",
            RolId = empleadoRol.Id
        });

        var yaRevocado = DateTime.UtcNow.AddDays(-1);
        db.RefreshTokens.AddRange(
            new RefreshToken
            {
                UsuarioId = target.Id,
                Token = "vivo",
                ExpiresAt = DateTime.UtcNow.AddHours(4),
                CreatedAt = DateTime.UtcNow
            },
            new RefreshToken
            {
                UsuarioId = target.Id,
                Token = "vencido",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-10),
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new RefreshToken
            {
                UsuarioId = target.Id,
                Token = "ya-revocado",
                ExpiresAt = DateTime.UtcNow.AddHours(4),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                RevokedAt = yaRevocado
            });
        await db.SaveChangesAsync();

        var dto = await service.CambiarEstadoAsync(
            target.Id,
            new CambiarEstadoUsuarioRequest { Estado = UsuarioEstado.Inactivo },
            actorUsuarioId: 1);

        Assert.Equal(UsuarioEstado.Inactivo, dto.Estado);
        var vivo = await db.RefreshTokens.FirstAsync(t => t.Token == "vivo");
        var vencido = await db.RefreshTokens.FirstAsync(t => t.Token == "vencido");
        var revocado = await db.RefreshTokens.FirstAsync(t => t.Token == "ya-revocado");
        Assert.NotNull(vivo.RevokedAt);
        Assert.Null(vencido.RevokedAt);
        Assert.Equal(yaRevocado, revocado.RevokedAt);
    }

    [Fact]
    public async Task CambiarEstado_Bloqueado_400()
    {
        var (service, db, _, admin, _, _) = CreateSut();
        var created = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Ana",
            Email = "bloq@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });
        await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Otro",
            Email = "otro2@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CambiarEstadoAsync(
            created.Id,
            new CambiarEstadoUsuarioRequest { Estado = UsuarioEstado.Bloqueado },
            actorUsuarioId: 999));

        Assert.Equal(400, ex.StatusCode);
        var stored = await db.Usuarios.FirstAsync(u => u.Id == created.Id);
        Assert.Equal(UsuarioEstado.Activo, stored.Estado);
    }

    [Fact]
    public async Task Update_UltimoAdmin_CambioDeRol_400()
    {
        var (service, db, _, admin, empleadoRol, _) = CreateSut();
        var unico = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Unico",
            Email = "rol@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });

        var ex = await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(unico.Id, new UpdateUsuarioRequest
        {
            Nombre = "Unico",
            Email = "rol@test.local",
            RolId = empleadoRol.Id
        }));

        Assert.Equal(400, ex.StatusCode);
        var stored = await db.Usuarios.FirstAsync(u => u.Id == unico.Id);
        Assert.Equal(admin.Id, stored.RolId);
    }

    [Fact]
    public async Task List_FiltraPorTextoRolYEstado()
    {
        var (service, _, _, admin, empleadoRol, _) = CreateSut();
        await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Ana Admin",
            Email = "ana.admin@test.local",
            Password = "ClaveSegura1",
            RolId = admin.Id
        });
        var empleado = await service.CreateAsync(new CreateUsuarioRequest
        {
            Nombre = "Beto",
            Email = "beto@test.local",
            Password = "ClaveSegura1",
            RolId = empleadoRol.Id
        });
        await service.CambiarEstadoAsync(
            empleado.Id,
            new CambiarEstadoUsuarioRequest { Estado = UsuarioEstado.Inactivo },
            actorUsuarioId: 999);

        var porRol = await service.ListAsync(new UsuarioFilterQuery { Rol = "empleado" });
        Assert.Single(porRol);
        Assert.Equal("beto@test.local", porRol[0].Email);

        var porEstado = await service.ListAsync(new UsuarioFilterQuery { Estado = UsuarioEstado.Activo });
        Assert.DoesNotContain(porEstado, u => u.Id == empleado.Id);

        var porTexto = await service.ListAsync(new UsuarioFilterQuery { Q = "ANA.ADMIN" });
        Assert.Single(porTexto);
        Assert.Equal(RolesSistema.Admin, porTexto[0].RolNombre);
    }
}
