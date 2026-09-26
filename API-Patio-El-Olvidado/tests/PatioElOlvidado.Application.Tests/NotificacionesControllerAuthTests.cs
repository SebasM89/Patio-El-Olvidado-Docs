using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

/// <summary>RN-12: Admin y Empleado consultan la bandeja propia. Cliente no está autorizado. Anónimo cae en el fallback 401.</summary>
public class NotificacionesControllerAuthTests
{
    [Fact]
    public void Clase_AdminYEmpleado_SinCliente_SinAnonimo()
    {
        var classAuth = typeof(NotificacionesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        var roles = classAuth!.Roles!.Split(',');
        Assert.Contains(RolesSistema.Admin, roles);
        Assert.Contains(RolesSistema.Empleado, roles);
        Assert.DoesNotContain(RolesSistema.Cliente, roles);
        Assert.Null(typeof(NotificacionesController).GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(NotificacionesController.List))]
    [InlineData(nameof(NotificacionesController.Conteo))]
    [InlineData(nameof(NotificacionesController.MarcarLeida))]
    public void Endpoints_HeredanBandeja_SinAllowAnonymous(string methodName)
    {
        var method = typeof(NotificacionesController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Método {methodName} no encontrado.");

        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        var methodRoles = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Where(a => a.Roles != null)
            .SelectMany(a => a.Roles!.Split(','));
        Assert.DoesNotContain(RolesSistema.Cliente, methodRoles);
    }

    [Fact]
    public void Rutas_ConteoEstaticoAntesDeId_SinAltaNiBorrado()
    {
        var route = typeof(NotificacionesController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/notificaciones", route?.Template);

        var list = typeof(NotificacionesController).GetMethod(nameof(NotificacionesController.List));
        var conteo = typeof(NotificacionesController).GetMethod(nameof(NotificacionesController.Conteo));
        var marcar = typeof(NotificacionesController).GetMethod(nameof(NotificacionesController.MarcarLeida));

        Assert.Equal(string.Empty, list?.GetCustomAttribute<HttpGetAttribute>()?.Template ?? string.Empty);
        Assert.NotNull(list?.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal("conteo", conteo?.GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal("{id:int}/leida", marcar?.GetCustomAttribute<HttpPatchAttribute>()?.Template);
        Assert.True(conteo!.MetadataToken < marcar!.MetadataToken);

        var httpMethods = typeof(NotificacionesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .ToList();
        Assert.DoesNotContain(httpMethods, a => a is HttpPostAttribute or HttpDeleteAttribute or HttpPutAttribute);
    }
}
