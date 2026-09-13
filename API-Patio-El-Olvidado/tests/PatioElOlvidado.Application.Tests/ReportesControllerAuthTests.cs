using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.API.Controllers;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Tests;

public class ReportesControllerAuthTests
{
    [Fact]
    public void Controller_RequiresAdminOnly_NotEmpleadoNiCliente()
    {
        var classAuth = typeof(ReportesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Equal(RolesSistema.Admin, classAuth!.Roles);
        Assert.DoesNotContain(RolesSistema.Empleado, classAuth.Roles ?? "");
        Assert.DoesNotContain(RolesSistema.Cliente, classAuth.Roles ?? "");
    }

    [Fact]
    public void Controller_IsApiControllerWithReportesRoute()
    {
        var route = typeof(ReportesController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/reportes", route?.Template);
    }

    [Fact]
    public void Endpoints_ExistAndInheritAuthorize()
    {
        var ventas = typeof(ReportesController).GetMethod(nameof(ReportesController.GetVentas))!;
        Assert.Equal("ventas", ventas.GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Null(ventas.GetCustomAttribute<AllowAnonymousAttribute>());

        var caja = typeof(ReportesController).GetMethod(nameof(ReportesController.GetCaja))!;
        Assert.Equal("caja", caja.GetCustomAttribute<HttpGetAttribute>()?.Template);

        var nomina = typeof(ReportesController).GetMethod(nameof(ReportesController.GetNomina))!;
        Assert.Equal("nomina", nomina.GetCustomAttribute<HttpGetAttribute>()?.Template);

        Assert.Equal(
            "ventas/export/pdf",
            typeof(ReportesController).GetMethod(nameof(ReportesController.ExportVentasPdf))!
                .GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal(
            "caja/export/csv",
            typeof(ReportesController).GetMethod(nameof(ReportesController.ExportCajaCsv))!
                .GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal(
            "nomina/export/pdf",
            typeof(ReportesController).GetMethod(nameof(ReportesController.ExportNominaPdf))!
                .GetCustomAttribute<HttpGetAttribute>()?.Template);
    }
}
