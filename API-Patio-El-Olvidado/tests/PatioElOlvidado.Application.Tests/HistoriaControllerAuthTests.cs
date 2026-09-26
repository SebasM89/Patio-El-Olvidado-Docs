using System.Reflection;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Historia;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Application.Validators;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class HistoriaControllerAuthTests
{
    private const string SemillaTitulo = "Patio El Olvidado";
    private const string SemillaTexto =
        "Patio El Olvidado es el restaurante que este sistema administra. El administrador puede reemplazar este texto.";

    [Fact]
    public void Auth_SinAllowAnonymous_SinPostNiDelete_PutSoloAdmin()
    {
        var controllerType = typeof(HistoriaController);
        Assert.Null(controllerType.GetCustomAttribute<AllowAnonymousAttribute>());

        var classAuth = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Equal(
            $"{RolesSistema.Admin},{RolesSistema.Empleado},{RolesSistema.Cliente}",
            classAuth!.Roles);
        Assert.Equal("api/historia", controllerType.GetCustomAttribute<RouteAttribute>()?.Template);

        var declared = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(declared, m => m.GetCustomAttribute<HttpPostAttribute>() != null);
        Assert.DoesNotContain(declared, m => m.GetCustomAttribute<HttpDeleteAttribute>() != null);
        Assert.DoesNotContain(declared, m => m.GetCustomAttribute<AllowAnonymousAttribute>() != null);

        var get = controllerType.GetMethod(nameof(HistoriaController.Get))!;
        Assert.NotNull(get.GetCustomAttribute<HttpGetAttribute>());
        Assert.Null(get.GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.Empty(get.GetCustomAttributes<AuthorizeAttribute>(inherit: false));

        var put = controllerType.GetMethod(nameof(HistoriaController.Update))!;
        Assert.NotNull(put.GetCustomAttribute<HttpPutAttribute>());
        Assert.Null(put.GetCustomAttribute<HttpPutAttribute>()!.Template);
        var putAuth = put.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(putAuth, a => a.Roles == RolesSistema.Admin);
        Assert.DoesNotContain(putAuth, a => a.Roles != null && a.Roles.Contains(RolesSistema.Empleado));
        Assert.DoesNotContain(putAuth, a => a.Roles != null && a.Roles.Contains(RolesSistema.Cliente));
    }

    [Theory]
    [InlineData(RolesSistema.Admin)]
    [InlineData(RolesSistema.Empleado)]
    [InlineData(RolesSistema.Cliente)]
    public async Task Get_TresRoles_200ConSemilla(string rol)
    {
        var (controller, _) = Create(seed: true, rol);

        var result = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HistoriaDto>(ok.Value);
        Assert.Equal(1, dto.Id);
        Assert.Equal(SemillaTitulo, dto.Titulo);
        Assert.Equal(SemillaTexto, dto.Texto);
    }

    [Fact]
    public async Task Put_Admin_200_PersisteUsuarioDelJwt()
    {
        var (controller, db) = Create(seed: true, RolesSistema.Admin, usuarioId: 27);
        var antes = DateTime.UtcNow.AddSeconds(-2);

        var result = await controller.Update(
            new UpdateHistoriaRequest { Titulo = "  Desde el token  ", Texto = "  Texto persistido  " },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HistoriaDto>(ok.Value);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Desde el token", dto.Titulo);
        Assert.Equal("Texto persistido", dto.Texto);
        Assert.NotNull(dto.ActualizadoUtc);
        Assert.InRange(dto.ActualizadoUtc!.Value, antes, DateTime.UtcNow.AddSeconds(2));
        Assert.Equal(27, dto.ActualizadoPorUsuarioId);

        db.ChangeTracker.Clear();
        var fila = await db.HistoriaRestaurante.SingleAsync();
        Assert.Equal("Desde el token", fila.Titulo);
        Assert.Equal(27, fila.ActualizadoPorUsuarioId);
        Assert.NotNull(fila.ActualizadoUtc);
    }

    [Theory]
    [InlineData(RolesSistema.Empleado)]
    [InlineData(RolesSistema.Cliente)]
    public async Task Put_EmpleadoYCliente_403_NoCambiaLaFila(string rol)
    {
        var (controller, db) = Create(seed: true, rol, usuarioId: 4);

        var ex = await Assert.ThrowsAsync<AppException>(() => controller.Update(
            new UpdateHistoriaRequest { Titulo = "Ajeno", Texto = "No guarda" },
            CancellationToken.None));

        Assert.Equal(403, ex.StatusCode);
        db.ChangeTracker.Clear();
        var fila = await db.HistoriaRestaurante.SingleAsync();
        Assert.Equal(SemillaTitulo, fila.Titulo);
        Assert.Equal(SemillaTexto, fila.Texto);
        Assert.Null(fila.ActualizadoUtc);
        Assert.Null(fila.ActualizadoPorUsuarioId);
    }

    [Theory]
    [MemberData(nameof(PayloadsInvalidos))]
    public async Task Put_FluentValidation_400_NoCambiaLaFila(string titulo, string texto)
    {
        var (controller, db) = Create(seed: true, RolesSistema.Admin, usuarioId: 2);

        await Assert.ThrowsAsync<ValidationException>(() => controller.Update(
            new UpdateHistoriaRequest { Titulo = titulo, Texto = texto },
            CancellationToken.None));

        db.ChangeTracker.Clear();
        var fila = await db.HistoriaRestaurante.SingleAsync();
        Assert.Equal(SemillaTitulo, fila.Titulo);
        Assert.Equal(SemillaTexto, fila.Texto);
        Assert.Null(fila.ActualizadoUtc);
        Assert.Null(fila.ActualizadoPorUsuarioId);
    }

    [Fact]
    public async Task SinFila_GetYPut_404()
    {
        var (controller, db) = Create(seed: false, RolesSistema.Admin, usuarioId: 1);

        var get = await controller.Get(CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(get.Result);

        var ex = await Assert.ThrowsAsync<AppException>(() => controller.Update(
            new UpdateHistoriaRequest { Titulo = "Nueva", Texto = "Sin semilla" },
            CancellationToken.None));
        Assert.Equal(404, ex.StatusCode);
        Assert.Empty(db.HistoriaRestaurante);
    }

    public static IEnumerable<object[]> PayloadsInvalidos()
    {
        yield return new object[] { "", "Texto válido" };
        yield return new object[] { new string('a', 121), "Texto válido" };
        yield return new object[] { "Título válido", "" };
        yield return new object[] { "Título válido", new string('b', 4001) };
    }

    private static (HistoriaController Controller, AppDbContext Db) Create(bool seed, string rol, int usuarioId = 1)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        if (seed)
        {
            db.HistoriaRestaurante.Add(new HistoriaRestaurante
            {
                Id = 1,
                Titulo = SemillaTitulo,
                Texto = SemillaTexto
            });
            db.SaveChanges();
        }

        var service = new HistoriaService(new HistoriaRepository(db), new UnitOfWork(db));
        var controller = new HistoriaController(service, new UpdateHistoriaRequestValidator());
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, rol),
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())
            ],
            authenticationType: "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return (controller, db);
    }
}
