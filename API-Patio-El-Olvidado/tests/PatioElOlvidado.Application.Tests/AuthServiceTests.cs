using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PatioElOlvidado.Application.DTOs.Auth;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Options;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;
using PatioElOlvidado.Infrastructure.Security;

namespace PatioElOlvidado.Application.Tests;

public class AuthServiceTests
{
    private static (AuthService Service, AppDbContext Db, IPasswordHasher Hasher) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var adminRol = new Rol { Nombre = RolesSistema.Admin, Descripcion = "Admin" };
        db.Roles.Add(adminRol);
        db.SaveChanges();

        var hasher = new BcryptPasswordHasher();
        db.Usuarios.Add(new Usuario
        {
            Nombre = "Admin Test",
            Email = "admin@test.local",
            PasswordHash = hasher.Hash("Admin123!"),
            RolId = adminRol.Id,
            Estado = UsuarioEstado.Activo
        });
        db.SaveChanges();

        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            Key = "TestSigningKey_AtLeast_32_Characters!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

        var email = new Mock<IEmailSender>();
        email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AuthService(
            new UsuarioRepository(db),
            new RefreshTokenRepository(db),
            new PasswordResetTokenRepository(db),
            new UnitOfWork(db),
            hasher,
            new JwtTokenService(jwtOptions),
            email.Object,
            jwtOptions);

        return (service, db, hasher);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var (service, _, _) = CreateSut();

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "admin@test.local",
            Password = "Admin123!"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal("Admin", result.Usuario.Rol);
    }

    [Fact]
    public async Task Login_ThreeFailedAttempts_LocksAccount_RN02()
    {
        var (service, db, _) = CreateSut();

        for (var i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<PatioElOlvidado.Application.Common.AuthException>(() =>
                service.LoginAsync(new LoginRequest
                {
                    Email = "admin@test.local",
                    Password = "wrong"
                }));
        }

        var usuario = db.Usuarios.Single();
        Assert.NotNull(usuario.BloqueadoHasta);
        Assert.True(usuario.BloqueadoHasta > DateTime.UtcNow);

        var locked = await Assert.ThrowsAsync<PatioElOlvidado.Application.Common.AuthException>(() =>
            service.LoginAsync(new LoginRequest
            {
                Email = "admin@test.local",
                Password = "Admin123!"
            }));

        Assert.Equal(423, locked.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        var (service, _, _) = CreateSut();

        var login = await service.LoginAsync(new LoginRequest
        {
            Email = "admin@test.local",
            Password = "Admin123!"
        });

        var refreshed = await service.RefreshAsync(new RefreshRequest
        {
            RefreshToken = login.RefreshToken
        });

        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
    }
}
