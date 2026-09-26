using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

public class UsuariosControllerAuthTests
{
    [Fact]
    public void UsuariosController_RouteYAdmin()
    {
        var route = typeof(UsuariosController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/usuarios", route?.Template);
        AssertAdminOnly(typeof(UsuariosController));
    }

    [Fact]
    public void RolesController_RouteYAdmin()
    {
        var route = typeof(RolesController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/roles", route?.Template);
        AssertAdminOnly(typeof(RolesController));
    }

    [Theory]
    [InlineData(nameof(UsuariosController.List))]
    [InlineData(nameof(UsuariosController.GetById))]
    [InlineData(nameof(UsuariosController.Create))]
    [InlineData(nameof(UsuariosController.Update))]
    [InlineData(nameof(UsuariosController.CambiarEstado))]
    public void UsuariosEndpoints_SoloAdmin(string methodName)
    {
        var method = typeof(UsuariosController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");
        AssertMethodAdminOnly(method);
    }

    [Fact]
    public void RolesList_SoloAdmin()
    {
        var method = typeof(RolesController).GetMethod(nameof(RolesController.List))
            ?? throw new InvalidOperationException("List no encontrado.");
        AssertMethodAdminOnly(method);
    }

    [Fact]
    public void CambiarEstado_EsPatchSobreEstado()
    {
        var method = typeof(UsuariosController).GetMethod(nameof(UsuariosController.CambiarEstado))
            ?? throw new InvalidOperationException("CambiarEstado no encontrado.");
        var patch = method.GetCustomAttribute<HttpPatchAttribute>();
        Assert.Equal("{id:int}/estado", patch?.Template);
    }

    private static void AssertAdminOnly(Type controller)
    {
        var classAuth = controller.GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal(RolesSistema.Admin, classAuth?.Roles);
        Assert.DoesNotContain(RolesSistema.Empleado, classAuth?.Roles ?? "");
        Assert.DoesNotContain(RolesSistema.Cliente, classAuth?.Roles ?? "");
    }

    private static void AssertMethodAdminOnly(MethodInfo method)
    {
        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        Assert.Contains(authorize, a => a.Roles == RolesSistema.Admin);
        Assert.DoesNotContain(authorize, a =>
            a.Roles != null && a.Roles.Contains(RolesSistema.Empleado));
        Assert.DoesNotContain(authorize, a =>
            a.Roles != null && a.Roles.Contains(RolesSistema.Cliente));
    }
}
