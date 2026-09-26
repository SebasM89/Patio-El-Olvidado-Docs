using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

/// <summary>RN-11: Admin muta proveedores; Empleado consulta; Cliente no está autorizado.</summary>
public class ProveedorControllerAuthTests
{
    [Theory]
    [InlineData(nameof(ProveedoresController.Create))]
    [InlineData(nameof(ProveedoresController.Update))]
    public void Mutaciones_SoloAdmin(string methodName)
    {
        var method = typeof(ProveedoresController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Admin);
        Assert.DoesNotContain(authorize, a => a.Roles != null && a.Roles.Contains(RolesSistema.Empleado));
        Assert.DoesNotContain(authorize, a => a.Roles != null && a.Roles.Contains(RolesSistema.Cliente));
    }

    [Theory]
    [InlineData(nameof(ProveedoresController.List))]
    [InlineData(nameof(ProveedoresController.GetById))]
    public void Lectura_AdminYEmpleado_SinCliente(string methodName)
    {
        var method = typeof(ProveedoresController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var classAuth = typeof(ProveedoresController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        var roles = classAuth!.Roles!.Split(',');
        Assert.Contains(RolesSistema.Admin, roles);
        Assert.Contains(RolesSistema.Empleado, roles);
        Assert.DoesNotContain(RolesSistema.Cliente, roles);

        var methodAdminOnly = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Any(a => a.Roles == RolesSistema.Admin);
        Assert.False(methodAdminOnly);
    }

    [Fact]
    public void Rutas_Catalogo()
    {
        var route = typeof(ProveedoresController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/proveedores", route?.Template);

        Assert.Null(typeof(ProveedoresController).GetMethod(nameof(ProveedoresController.List))!
            .GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal("{id:int}", typeof(ProveedoresController).GetMethod(nameof(ProveedoresController.GetById))!
            .GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Null(typeof(ProveedoresController).GetMethod(nameof(ProveedoresController.Create))!
            .GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Equal("{id:int}", typeof(ProveedoresController).GetMethod(nameof(ProveedoresController.Update))!
            .GetCustomAttribute<HttpPutAttribute>()?.Template);
    }
}
