using PatioElOlvidado.Application.DTOs.Caja;
using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Services;

public class CajaService : ICajaService
{
    private readonly ICajaRepository _cajas;
    private readonly ICajaPdfExporter _pdfExporter;
    private readonly ICajaCsvExporter _csvExporter;

    public CajaService(
        ICajaRepository cajas,
        ICajaPdfExporter pdfExporter,
        ICajaCsvExporter csvExporter)
    {
        _cajas = cajas;
        _pdfExporter = pdfExporter;
        _csvExporter = csvExporter;
    }

    public Task<CajaDiaDto> GetCajaHoyAsync(CancellationToken cancellationToken = default)
        => GetByFechaAsync(null, cancellationToken);

    public async Task<CajaDiaDto> GetByFechaAsync(
        DateOnly? fecha,
        CancellationToken cancellationToken = default)
    {
        var dia = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var caja = await _cajas.GetByFechaAsync(dia, cancellationToken);
        return caja is null ? Empty(dia) : Map(caja);
    }

    public async Task<CajaExportResult> ExportPdfAsync(
        DateOnly? fecha,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetByFechaAsync(fecha, cancellationToken);
        return new CajaExportResult
        {
            Content = _pdfExporter.Export(dto),
            ContentType = "application/pdf",
            FileName = $"caja-{dto.Fecha:yyyy-MM-dd}.pdf"
        };
    }

    public async Task<CajaExportResult> ExportCsvAsync(
        DateOnly? fecha,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetByFechaAsync(fecha, cancellationToken);
        return new CajaExportResult
        {
            Content = _csvExporter.Export(dto),
            ContentType = "text/csv",
            FileName = $"caja-{dto.Fecha:yyyy-MM-dd}.csv"
        };
    }

    private static CajaDiaDto Empty(DateOnly fecha) => new()
    {
        Fecha = fecha,
        TotalEfectivo = 0,
        TotalTarjeta = 0,
        TotalTransferencia = 0,
        Total = 0
    };

    private static CajaDiaDto Map(Caja caja) => new()
    {
        Fecha = caja.Fecha,
        TotalEfectivo = caja.TotalEfectivo,
        TotalTarjeta = caja.TotalTarjeta,
        TotalTransferencia = caja.TotalTransferencia,
        Total = caja.Total
    };
}
