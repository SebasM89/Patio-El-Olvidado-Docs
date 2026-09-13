using System.Text;
using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.DTOs.Reportes;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Application.Validators;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Export;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class ReportesServiceTests
{
    private static (ReportesService Sut, AppDbContext Db, ReporteRangoQueryValidator Validator) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var sut = new ReportesService(
            new PagoRepository(db),
            new CajaRepository(db),
            new LiquidacionRepository(db),
            new QuestPdfVentasExporter(),
            new CsvHelperVentasExporter(),
            new QuestPdfCajaRangoExporter(),
            new CsvHelperCajaRangoExporter(),
            new QuestPdfNominaExporter(),
            new CsvHelperNominaExporter());
        return (sut, db, new ReporteRangoQueryValidator());
    }

    private static ReporteRangoQuery Rango(DateOnly desde, DateOnly hasta)
        => new() { Desde = desde, Hasta = hasta };

    [Fact]
    public async Task Ventas_SoloCompletados_TotalesPorMetodoYDias()
    {
        var (sut, db, _) = CreateSut();
        var dia = new DateOnly(2026, 9, 10);
        db.Cajas.Add(new Caja { Fecha = dia });
        await db.SaveChangesAsync();
        var cajaId = db.Cajas.Single().Id;

        db.Pagos.AddRange(
            new Pago
            {
                PedidoId = 1,
                Metodo = PagoMetodo.Efectivo,
                Estado = PagoEstado.Completado,
                Monto = 100m,
                FechaPago = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
                CajaId = cajaId
            },
            new Pago
            {
                PedidoId = 1,
                Metodo = PagoMetodo.Tarjeta,
                Estado = PagoEstado.Completado,
                Monto = 50m,
                FechaPago = new DateTime(2026, 9, 10, 15, 0, 0, DateTimeKind.Utc),
                CajaId = cajaId
            },
            new Pago
            {
                PedidoId = 2,
                Metodo = PagoMetodo.Transferencia,
                Estado = PagoEstado.Completado,
                Monto = 25m,
                FechaPago = new DateTime(2026, 9, 11, 8, 0, 0, DateTimeKind.Utc),
                CajaId = cajaId
            },
            new Pago
            {
                PedidoId = 3,
                Metodo = PagoMetodo.Efectivo,
                Estado = PagoEstado.Anulado,
                Monto = 999m,
                FechaPago = new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc),
                CajaId = cajaId
            });
        await db.SaveChangesAsync();

        var dto = await sut.GetVentasAsync(Rango(dia, new DateOnly(2026, 9, 11)));

        Assert.Equal(100m, dto.TotalEfectivo);
        Assert.Equal(50m, dto.TotalTarjeta);
        Assert.Equal(25m, dto.TotalTransferencia);
        Assert.Equal(175m, dto.Total);
        Assert.Equal(3, dto.CantidadPagos);
        Assert.Equal(2, dto.CantidadPedidos);
        Assert.Equal(2, dto.Dias.Count);
        Assert.Equal(150m, dto.Dias[0].Total);
        Assert.Equal(25m, dto.Dias[1].Total);
    }

    [Fact]
    public async Task Ventas_FueraDeRango_NoSuma()
    {
        var (sut, db, _) = CreateSut();
        var dia = new DateOnly(2026, 9, 10);
        db.Cajas.Add(new Caja { Fecha = dia });
        await db.SaveChangesAsync();
        var cajaId = db.Cajas.Single().Id;

        db.Pagos.Add(new Pago
        {
            PedidoId = 1,
            Metodo = PagoMetodo.Efectivo,
            Estado = PagoEstado.Completado,
            Monto = 40m,
            FechaPago = new DateTime(2026, 9, 9, 23, 59, 0, DateTimeKind.Utc),
            CajaId = cajaId
        });
        await db.SaveChangesAsync();

        var dto = await sut.GetVentasAsync(Rango(dia, dia));

        Assert.Equal(0m, dto.Total);
        Assert.Empty(dto.Dias);
    }

    [Fact]
    public async Task CajaRango_SumaFilasDelRango()
    {
        var (sut, db, _) = CreateSut();
        db.Cajas.AddRange(
            new Caja
            {
                Fecha = new DateOnly(2026, 9, 1),
                TotalEfectivo = 10m,
                TotalTarjeta = 20m,
                TotalTransferencia = 0
            },
            new Caja
            {
                Fecha = new DateOnly(2026, 9, 2),
                TotalEfectivo = 5m,
                TotalTarjeta = 0,
                TotalTransferencia = 15m
            },
            new Caja
            {
                Fecha = new DateOnly(2026, 9, 5),
                TotalEfectivo = 100m,
                TotalTarjeta = 0,
                TotalTransferencia = 0
            });
        await db.SaveChangesAsync();

        var dto = await sut.GetCajaRangoAsync(Rango(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2)));

        Assert.Equal(2, dto.Dias.Count);
        Assert.Equal(15m, dto.TotalEfectivo);
        Assert.Equal(20m, dto.TotalTarjeta);
        Assert.Equal(15m, dto.TotalTransferencia);
        Assert.Equal(50m, dto.Total);
    }

    [Fact]
    public async Task Nomina_IntersectaPeriodo_IncluyeNombreYTotales()
    {
        var (sut, db, _) = CreateSut();
        db.Empleados.Add(new Empleado
        {
            Nombre = "Ana Pérez",
            TarifaHora = 1000m,
            HorasTrabajadas = 0,
            Activo = true
        });
        await db.SaveChangesAsync();
        var empId = db.Empleados.Single().Id;

        db.Liquidaciones.AddRange(
            new Liquidacion
            {
                EmpleadoId = empId,
                PeriodoDesde = new DateOnly(2026, 8, 1),
                PeriodoHasta = new DateOnly(2026, 8, 15),
                Horas = 10m,
                TarifaHoraSnapshot = 1000m,
                Monto = 10000m,
                GeneradaEnUtc = DateTime.UtcNow,
                GeneradaPorUsuarioId = 1
            },
            new Liquidacion
            {
                EmpleadoId = empId,
                PeriodoDesde = new DateOnly(2026, 9, 1),
                PeriodoHasta = new DateOnly(2026, 9, 15),
                Horas = 8m,
                TarifaHoraSnapshot = 1000m,
                Monto = 8000m,
                GeneradaEnUtc = DateTime.UtcNow,
                GeneradaPorUsuarioId = 1
            });
        await db.SaveChangesAsync();

        // Interseca: periodo ago 1-15 con rango ago 10 - sep 5
        var dto = await sut.GetNominaAsync(Rango(new DateOnly(2026, 8, 10), new DateOnly(2026, 9, 5)));

        Assert.Equal(2, dto.Lineas.Count);
        Assert.All(dto.Lineas, l => Assert.Equal("Ana Pérez", l.EmpleadoNombre));
        Assert.Equal(18m, dto.TotalHoras);
        Assert.Equal(18000m, dto.TotalMonto);
    }

    [Fact]
    public async Task Nomina_SinInterseccion_ListaVacia()
    {
        var (sut, db, _) = CreateSut();
        db.Empleados.Add(new Empleado { Nombre = "Beto", TarifaHora = 500m, Activo = true });
        await db.SaveChangesAsync();
        var empId = db.Empleados.Single().Id;
        db.Liquidaciones.Add(new Liquidacion
        {
            EmpleadoId = empId,
            PeriodoDesde = new DateOnly(2026, 1, 1),
            PeriodoHasta = new DateOnly(2026, 1, 31),
            Horas = 5m,
            TarifaHoraSnapshot = 500m,
            Monto = 2500m,
            GeneradaEnUtc = DateTime.UtcNow,
            GeneradaPorUsuarioId = 1
        });
        await db.SaveChangesAsync();

        var dto = await sut.GetNominaAsync(Rango(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));

        Assert.Empty(dto.Lineas);
        Assert.Equal(0m, dto.TotalMonto);
    }

    [Fact]
    public void Validacion_DesdeMayorQueHasta_Falla()
    {
        var (_, _, validator) = CreateSut();
        var result = validator.Validate(Rango(new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 1)));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Desde"));
    }

    [Fact]
    public void Validacion_RangoMayor366_Falla()
    {
        var (_, _, validator) = CreateSut();
        var desde = new DateOnly(2026, 1, 1);
        var hasta = desde.AddDays(366); // 367 días inclusive
        var result = validator.Validate(Rango(desde, hasta));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("366"));
    }

    [Fact]
    public void Validacion_Rango366_Ok()
    {
        var (_, _, validator) = CreateSut();
        var desde = new DateOnly(2026, 1, 1);
        var hasta = desde.AddDays(365); // 366 días inclusive
        var result = validator.Validate(Rango(desde, hasta));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExportVentasPdf_YCsv_BytesValidos()
    {
        var (sut, _, _) = CreateSut();
        var query = Rango(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 12));

        var pdf = await sut.ExportVentasPdfAsync(query);
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.True(pdf.Content.Length > 100);
        Assert.Equal(0x25, pdf.Content[0]);
        Assert.Equal(0x50, pdf.Content[1]);
        Assert.Equal(0x44, pdf.Content[2]);
        Assert.Equal(0x46, pdf.Content[3]);

        var csv = await sut.ExportVentasCsvAsync(query);
        var text = Encoding.UTF8.GetString(csv.Content);
        Assert.Equal("text/csv", csv.ContentType);
        Assert.Contains("TotalEfectivo", text);
        Assert.Contains("Resumen", text);
    }

    [Fact]
    public async Task ExportCajaYNomina_PdfCsv_Ok()
    {
        var (sut, _, _) = CreateSut();
        var query = Rango(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 12));

        var cajaPdf = await sut.ExportCajaRangoPdfAsync(query);
        Assert.Equal("application/pdf", cajaPdf.ContentType);
        Assert.Equal(0x25, cajaPdf.Content[0]);

        var cajaCsv = await sut.ExportCajaRangoCsvAsync(query);
        Assert.Contains("Total", Encoding.UTF8.GetString(cajaCsv.Content));

        var nominaPdf = await sut.ExportNominaPdfAsync(query);
        Assert.Equal("application/pdf", nominaPdf.ContentType);

        var nominaCsv = await sut.ExportNominaCsvAsync(query);
        Assert.Contains("EmpleadoNombre", Encoding.UTF8.GetString(nominaCsv.Content));
    }
}
