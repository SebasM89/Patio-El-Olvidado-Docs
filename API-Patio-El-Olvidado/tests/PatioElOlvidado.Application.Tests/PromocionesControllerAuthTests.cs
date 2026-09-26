using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Validators;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

/// <summary>RN-13: Admin y Cliente consultan; solo Admin muta. Empleado no está en el atributo (403). Anónimo, 401.</summary>
public class PromocionesControllerAuthTests
{
    [Fact]
    public void Lectura_AdminYCliente_SinEmpleado_RequiereAuth()
    {
        var classAuth = typeof(PromocionesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Null(typeof(PromocionesController).GetCustomAttribute<AllowAnonymousAttribute>());

        var roles = classAuth!.Roles!.Split(',');
        Assert.Contains(RolesSistema.Admin, roles);
        Assert.Contains(RolesSistema.Cliente, roles);
        Assert.DoesNotContain(RolesSistema.Empleado, roles);

        foreach (var name in new[] { nameof(PromocionesController.List), nameof(PromocionesController.GetById) })
        {
            var method = typeof(PromocionesController).GetMethod(name)!;
            Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
            var adminOnly = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
                .Any(a => a.Roles == RolesSistema.Admin);
            Assert.False(adminOnly);
        }
    }

    [Theory]
    [InlineData(nameof(PromocionesController.Create))]
    [InlineData(nameof(PromocionesController.Update))]
    [InlineData(nameof(PromocionesController.Delete))]
    public void Mutaciones_SoloAdmin_ClienteYEmpleado403(string methodName)
    {
        var method = typeof(PromocionesController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Admin);
        Assert.DoesNotContain(authorize, a => a.Roles != null && a.Roles.Contains(RolesSistema.Empleado));
        Assert.DoesNotContain(authorize, a => a.Roles != null && a.Roles.Contains(RolesSistema.Cliente));
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Rutas_Catalogo()
    {
        var route = typeof(PromocionesController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/promociones", route?.Template);

        Assert.Null(typeof(PromocionesController).GetMethod(nameof(PromocionesController.List))!
            .GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal("{id:int}", typeof(PromocionesController).GetMethod(nameof(PromocionesController.GetById))!
            .GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Null(typeof(PromocionesController).GetMethod(nameof(PromocionesController.Create))!
            .GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Equal("{id:int}", typeof(PromocionesController).GetMethod(nameof(PromocionesController.Update))!
            .GetCustomAttribute<HttpPutAttribute>()?.Template);
        Assert.Equal("{id:int}", typeof(PromocionesController).GetMethod(nameof(PromocionesController.Delete))!
            .GetCustomAttribute<HttpDeleteAttribute>()?.Template);
    }

    [Fact]
    public async Task Delete_SegundaBaja_204()
    {
        var service = new Mock<IPromocionService>();
        service.Setup(s => s.DeleteAsync(7, RolesSistema.Admin, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(service.Object);

        var first = await controller.Delete(7, CancellationToken.None);
        var second = await controller.Delete(7, CancellationToken.None);

        Assert.Equal(StatusCodes.Status204NoContent, Assert.IsType<NoContentResult>(first).StatusCode);
        Assert.Equal(StatusCodes.Status204NoContent, Assert.IsType<NoContentResult>(second).StatusCode);
        service.Verify(s => s.DeleteAsync(7, RolesSistema.Admin, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetById_CuandoElServicioNoDevuelve_404()
    {
        var service = new Mock<IPromocionService>();
        service.Setup(s => s.GetByIdAsync(3, RolesSistema.Cliente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.DTOs.Promociones.PromocionDto?)null);

        var controller = CreateController(service.Object, RolesSistema.Cliente);
        var result = await controller.GetById(3, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static PromocionesController CreateController(IPromocionService service, string rol = RolesSistema.Admin)
    {
        var controller = new PromocionesController(
            service,
            new CreatePromocionRequestValidator(),
            new UpdatePromocionRequestValidator());
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, rol)],
            authenticationType: "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }
}
