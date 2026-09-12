using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

public class PagosControllerAuthTests
{
    [Fact]
    public void Controller_RequiresAdminEmpleado_NotCliente()
    {
        var classAuth = typeof(PagosController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Contains(RolesSistema.Admin, classAuth!.Roles!.Split(','));
        Assert.Contains(RolesSistema.Empleado, classAuth.Roles!.Split(','));
        Assert.DoesNotContain(RolesSistema.Cliente, classAuth.Roles!.Split(','));
    }

    [Fact]
    public void Anular_RequiresAdminOnly()
    {
        var method = typeof(PagosController).GetMethod(nameof(PagosController.Anular))!;
        var auth = method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal(RolesSistema.Admin, auth!.Roles);
    }

    [Fact]
    public void Controller_IsApiControllerWithPagosRoute()
    {
        var route = typeof(PagosController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/pagos", route?.Template);
    }

    [Fact]
    public void CajaController_RequiresAdminEmpleado()
    {
        var classAuth = typeof(CajaController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Contains(RolesSistema.Admin, classAuth!.Roles!.Split(','));
        Assert.Contains(RolesSistema.Empleado, classAuth.Roles!.Split(','));
        Assert.DoesNotContain(RolesSistema.Cliente, classAuth.Roles!.Split(','));

        var route = typeof(CajaController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/caja", route?.Template);

        var hoy = typeof(CajaController).GetMethod(nameof(CajaController.GetHoy))!;
        var getHoy = hoy.GetCustomAttribute<HttpGetAttribute>();
        Assert.Equal("hoy", getHoy?.Template);
    }

    [Fact]
    public void CajaController_ExportEndpoints_ExistAndAuthorizeInherited()
    {
        var getByFecha = typeof(CajaController).GetMethod(nameof(CajaController.GetByFecha))!;
        Assert.NotNull(getByFecha.GetCustomAttribute<HttpGetAttribute>());

        var pdf = typeof(CajaController).GetMethod(nameof(CajaController.ExportPdf))!;
        var pdfGet = pdf.GetCustomAttribute<HttpGetAttribute>();
        Assert.Equal("export/pdf", pdfGet?.Template);

        var csv = typeof(CajaController).GetMethod(nameof(CajaController.ExportCsv))!;
        var csvGet = csv.GetCustomAttribute<HttpGetAttribute>();
        Assert.Equal("export/csv", csvGet?.Template);

        // Sin [AllowAnonymous]: hereda Admin|Empleado (Cliente → 403)
        Assert.Null(pdf.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(csv.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(getByFecha.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
