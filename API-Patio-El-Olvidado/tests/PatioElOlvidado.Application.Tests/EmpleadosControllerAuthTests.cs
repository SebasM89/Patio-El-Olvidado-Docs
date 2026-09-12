using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

public class EmpleadosControllerAuthTests
{
    [Fact]
    public void Controller_IsApiControllerWithEmpleadosRoute()
    {
        var route = typeof(EmpleadosController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/empleados", route?.Template);

        var classAuth = typeof(EmpleadosController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
    }

    [Theory]
    [InlineData(nameof(EmpleadosController.List))]
    [InlineData(nameof(EmpleadosController.Create))]
    [InlineData(nameof(EmpleadosController.Update))]
    [InlineData(nameof(EmpleadosController.Delete))]
    [InlineData(nameof(EmpleadosController.Entrada))]
    [InlineData(nameof(EmpleadosController.Salida))]
    [InlineData(nameof(EmpleadosController.GenerarLiquidacion))]
    public void AdminOnlyEndpoints_RequireAdmin(string methodName)
    {
        var method = typeof(EmpleadosController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Admin);
        Assert.DoesNotContain(authorize, a =>
            a.Roles != null && a.Roles.Contains(RolesSistema.Empleado));
        Assert.DoesNotContain(authorize, a =>
            a.Roles != null && a.Roles.Contains(RolesSistema.Cliente));
    }

    [Theory]
    [InlineData(nameof(EmpleadosController.GetMe))]
    [InlineData(nameof(EmpleadosController.GetMeFichajes))]
    [InlineData(nameof(EmpleadosController.EntradaMe))]
    [InlineData(nameof(EmpleadosController.SalidaMe))]
    [InlineData(nameof(EmpleadosController.PreviewMeLiquidacion))]
    [InlineData(nameof(EmpleadosController.GetMeLiquidaciones))]
    public void MeEndpoints_RequireEmpleadoRole_RN07(string methodName)
    {
        var method = typeof(EmpleadosController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Empleado);
    }

    [Fact]
    public void GetById_AllowsAdminAndEmpleado_NotCliente()
    {
        var method = typeof(EmpleadosController).GetMethod(nameof(EmpleadosController.GetById))
            ?? throw new InvalidOperationException("GetById no encontrado.");

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a =>
            a.Roles != null &&
            a.Roles.Contains(RolesSistema.Admin) &&
            a.Roles.Contains(RolesSistema.Empleado) &&
            !a.Roles.Contains(RolesSistema.Cliente));
    }
}
