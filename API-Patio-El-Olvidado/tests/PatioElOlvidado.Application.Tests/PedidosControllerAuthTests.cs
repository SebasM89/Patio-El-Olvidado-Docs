using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

public class PedidosControllerAuthTests
{
    [Fact]
    public void Controller_RequiresAdminEmpleadoCliente()
    {
        var classAuth = typeof(PedidosController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Contains(RolesSistema.Admin, classAuth!.Roles!.Split(','));
        Assert.Contains(RolesSistema.Empleado, classAuth.Roles!.Split(','));
        Assert.Contains(RolesSistema.Cliente, classAuth.Roles!.Split(','));
    }

    [Theory]
    [InlineData(nameof(PedidosController.List))]
    [InlineData(nameof(PedidosController.GetById))]
    [InlineData(nameof(PedidosController.Create))]
    [InlineData(nameof(PedidosController.Update))]
    [InlineData(nameof(PedidosController.CambiarEstado))]
    public void Actions_InheritAuthenticatedRoles(string methodName)
    {
        var method = typeof(PedidosController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        // Reglas finas (ownership / Listo) viven en el servicio; el controller no restringe solo Admin.
        var methodAdminOnly = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Any(a => a.Roles == RolesSistema.Admin);
        Assert.False(methodAdminOnly);
    }

    [Fact]
    public void Controller_IsApiControllerWithPedidosRoute()
    {
        var route = typeof(PedidosController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/pedidos", route?.Template);
    }

    [Fact]
    public void CambiarEstado_UsesHttpPatch()
    {
        var method = typeof(PedidosController).GetMethod(nameof(PedidosController.CambiarEstado))!;
        var patch = method.GetCustomAttribute<HttpPatchAttribute>();
        Assert.NotNull(patch);
        Assert.Equal("{id:int}/estado", patch!.Template);
    }
}
