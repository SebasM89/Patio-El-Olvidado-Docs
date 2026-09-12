using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

public class ClientesControllerAuthTests
{
    [Fact]
    public void Delete_RequiresAdminRole()
    {
        var method = typeof(ClientesController).GetMethod(nameof(ClientesController.Delete))
            ?? throw new InvalidOperationException("Delete no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Admin);
    }

    [Theory]
    [InlineData(nameof(ClientesController.List))]
    [InlineData(nameof(ClientesController.Create))]
    [InlineData(nameof(ClientesController.Update))]
    public void StaffMutationsAndList_AllowAdminEmpleado(string methodName)
    {
        var method = typeof(ClientesController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a =>
            a.Roles != null &&
            a.Roles.Contains(RolesSistema.Admin) &&
            a.Roles.Contains(RolesSistema.Empleado) &&
            !a.Roles.Contains(RolesSistema.Cliente));
    }

    [Theory]
    [InlineData(nameof(ClientesController.GetMe))]
    [InlineData(nameof(ClientesController.GetMeHistorial))]
    public void MeEndpoints_RequireClienteRole(string methodName)
    {
        var method = typeof(ClientesController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Cliente);
    }

    [Fact]
    public void Controller_IsApiControllerWithClientesRoute()
    {
        var route = typeof(ClientesController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/clientes", route?.Template);

        var classAuth = typeof(ClientesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
    }
}
