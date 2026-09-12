using PatioElOlvidado.Application.DTOs.Caja;
using PatioElOlvidado.Application.DTOs.Pagos;

namespace PatioElOlvidado.Application.Interfaces;

public interface ICajaService
{
    /// <summary>Resumen del día. <paramref name="fecha"/> null = hoy UTC. Sin fila → ceros.</summary>
    Task<CajaDiaDto> GetByFechaAsync(DateOnly? fecha, CancellationToken cancellationToken = default);

    Task<CajaDiaDto> GetCajaHoyAsync(CancellationToken cancellationToken = default);

    Task<CajaExportResult> ExportPdfAsync(DateOnly? fecha, CancellationToken cancellationToken = default);

    Task<CajaExportResult> ExportCsvAsync(DateOnly? fecha, CancellationToken cancellationToken = default);
}
