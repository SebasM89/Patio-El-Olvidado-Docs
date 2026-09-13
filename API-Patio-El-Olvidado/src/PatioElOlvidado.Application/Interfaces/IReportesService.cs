using PatioElOlvidado.Application.DTOs.Reportes;

namespace PatioElOlvidado.Application.Interfaces;

public interface IReportesService
{
    Task<VentasReporteDto> GetVentasAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);
    Task<CajaRangoReporteDto> GetCajaRangoAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);
    Task<NominaReporteDto> GetNominaAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportVentasPdfAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);
    Task<ReportExportResult> ExportVentasCsvAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);
    Task<ReportExportResult> ExportCajaRangoPdfAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);
    Task<ReportExportResult> ExportCajaRangoCsvAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);
    Task<ReportExportResult> ExportNominaPdfAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);
    Task<ReportExportResult> ExportNominaCsvAsync(ReporteRangoQuery query, CancellationToken cancellationToken = default);
}
