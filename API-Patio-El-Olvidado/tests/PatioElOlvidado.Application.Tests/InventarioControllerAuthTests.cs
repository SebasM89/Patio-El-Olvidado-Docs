using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

/// <summary>RN-10: Admin muta; Empleado consulta; Cliente no está autorizado.</summary>
public class InventarioControllerAuthTests
{
    [Theory]
    [InlineData(nameof(InventarioController.Create))]
    [InlineData(nameof(InventarioController.Update))]
    [InlineData(nameof(InventarioController.RegistrarMovimiento))]
    public void Mutaciones_SoloAdmin(string methodName)
    {
        var method = typeof(InventarioController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Admin);
        Assert.DoesNotContain(authorize, a => a.Roles != null && a.Roles.Contains(RolesSistema.Empleado));
        Assert.DoesNotContain(authorize, a => a.Roles != null && a.Roles.Contains(RolesSistema.Cliente));
    }

    [Theory]
    [InlineData(nameof(InventarioController.List))]
    [InlineData(nameof(InventarioController.Alertas))]
    [InlineData(nameof(InventarioController.GetById))]
    [InlineData(nameof(InventarioController.ListMovimientos))]
    public void Lectura_AdminYEmpleado_SinCliente(string methodName)
    {
        var method = typeof(InventarioController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var classAuth = typeof(InventarioController).GetCustomAttribute<AuthorizeAttribute>();
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
    public void Rutas_AlertasAntesDeId_YMovimientos()
    {
        var route = typeof(InventarioController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/inventario", route?.Template);

        var alertas = typeof(InventarioController).GetMethod(nameof(InventarioController.Alertas));
        var getById = typeof(InventarioController).GetMethod(nameof(InventarioController.GetById));
        Assert.Equal("alertas", alertas?.GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal("{id:int}", getById?.GetCustomAttribute<HttpGetAttribute>()?.Template);

        var listMov = typeof(InventarioController).GetMethod(nameof(InventarioController.ListMovimientos));
        var postMov = typeof(InventarioController).GetMethod(nameof(InventarioController.RegistrarMovimiento));
        Assert.Equal("{id:int}/movimientos", listMov?.GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal("{id:int}/movimientos", postMov?.GetCustomAttribute<HttpPostAttribute>()?.Template);

        Assert.True(alertas!.MetadataToken < getById!.MetadataToken);
    }
}
