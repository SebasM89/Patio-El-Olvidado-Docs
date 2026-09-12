using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

/// <summary>RN-03: mutaciones de menú restringidas a Admin a nivel de controller.</summary>
public class ProductosControllerAuthTests
{
    [Theory]
    [InlineData(nameof(ProductosController.Create))]
    [InlineData(nameof(ProductosController.Update))]
    [InlineData(nameof(ProductosController.Delete))]
    public void MutatingActions_RequireAdminRole(string methodName)
    {
        var method = typeof(ProductosController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Admin);
    }

    [Theory]
    [InlineData(nameof(ProductosController.List))]
    [InlineData(nameof(ProductosController.GetById))]
    public void ReadActions_AllowAdminEmpleadoCliente(string methodName)
    {
        var method = typeof(ProductosController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        // Hereda del controller: Admin, Empleado, Cliente
        var classAuth = typeof(ProductosController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Contains(RolesSistema.Admin, classAuth!.Roles!.Split(','));
        Assert.Contains(RolesSistema.Empleado, classAuth.Roles!.Split(','));
        Assert.Contains(RolesSistema.Cliente, classAuth.Roles!.Split(','));

        // Lectura no debe restringir solo a Admin
        var methodAdminOnly = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Any(a => a.Roles == RolesSistema.Admin);
        Assert.False(methodAdminOnly);
    }

    [Fact]
    public void Controller_IsApiControllerWithProductosRoute()
    {
        var route = typeof(ProductosController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/productos", route?.Template);
    }
}
