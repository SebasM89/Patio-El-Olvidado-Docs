using System.Text;
using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Export;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class CajaServiceTests
{
    private static (CajaService Sut, AppDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var sut = new CajaService(
            new CajaRepository(db),
            new QuestPdfCajaExporter(),
            new CsvHelperCajaExporter());
        return (sut, db);
    }

    [Fact]
    public async Task GetByFecha_SinFila_DevuelveCeros()
    {
        var (sut, _) = CreateSut();
        var fecha = new DateOnly(2026, 1, 15);

        var dto = await sut.GetByFechaAsync(fecha);

        Assert.Equal(fecha, dto.Fecha);
        Assert.Equal(0m, dto.TotalEfectivo);
        Assert.Equal(0m, dto.TotalTarjeta);
        Assert.Equal(0m, dto.TotalTransferencia);
        Assert.Equal(0m, dto.Total);
    }

    [Fact]
    public async Task GetByFecha_ConDatos_DevuelveTotales()
    {
        var (sut, db) = CreateSut();
        var fecha = new DateOnly(2026, 3, 10);
        db.Cajas.Add(new Caja
        {
            Fecha = fecha,
            TotalEfectivo = 100.50m,
            TotalTarjeta = 200m,
            TotalTransferencia = 50.25m
        });
        await db.SaveChangesAsync();

        var dto = await sut.GetByFechaAsync(fecha);

        Assert.Equal(100.50m, dto.TotalEfectivo);
        Assert.Equal(200m, dto.TotalTarjeta);
        Assert.Equal(50.25m, dto.TotalTransferencia);
        Assert.Equal(350.75m, dto.Total);
    }

    [Fact]
    public async Task GetByFecha_Null_UsaHoyUtc()
    {
        var (sut, db) = CreateSut();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Cajas.Add(new Caja
        {
            Fecha = hoy,
            TotalEfectivo = 10m,
            TotalTarjeta = 0,
            TotalTransferencia = 0
        });
        await db.SaveChangesAsync();

        var dto = await sut.GetByFechaAsync(null);
        var hoyDto = await sut.GetCajaHoyAsync();

        Assert.Equal(hoy, dto.Fecha);
        Assert.Equal(10m, dto.TotalEfectivo);
        Assert.Equal(dto.Fecha, hoyDto.Fecha);
        Assert.Equal(dto.TotalEfectivo, hoyDto.TotalEfectivo);
    }

    [Fact]
    public async Task ExportCsv_ContieneColumnasYValores()
    {
        var (sut, db) = CreateSut();
        var fecha = new DateOnly(2026, 9, 12);
        db.Cajas.Add(new Caja
        {
            Fecha = fecha,
            TotalEfectivo = 11.00m,
            TotalTarjeta = 22.00m,
            TotalTransferencia = 33.00m
        });
        await db.SaveChangesAsync();

        var file = await sut.ExportCsvAsync(fecha);
        var text = Encoding.UTF8.GetString(file.Content);

        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("caja-2026-09-12.csv", file.FileName);
        Assert.Contains("Fecha", text);
        Assert.Contains("TotalEfectivo", text);
        Assert.Contains("TotalTarjeta", text);
        Assert.Contains("TotalTransferencia", text);
        Assert.Contains("Total", text);
        Assert.Contains("2026-09-12", text);
        Assert.Contains("11.00", text);
        Assert.Contains("22.00", text);
        Assert.Contains("33.00", text);
        Assert.Contains("66.00", text);
    }

    [Fact]
    public async Task ExportPdf_ContentTypeYBytesValidos()
    {
        var (sut, _) = CreateSut();
        var fecha = new DateOnly(2026, 9, 12);

        var file = await sut.ExportPdfAsync(fecha);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("caja-2026-09-12.pdf", file.FileName);
        Assert.True(file.Content.Length > 100);
        // %PDF
        Assert.Equal(0x25, file.Content[0]);
        Assert.Equal(0x50, file.Content[1]);
        Assert.Equal(0x44, file.Content[2]);
        Assert.Equal(0x46, file.Content[3]);
    }
}
