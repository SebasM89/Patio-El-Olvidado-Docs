using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.DTOs.Reportes;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

public class ReportesService : IReportesService
{
    private readonly IPagoRepository _pagos;
    private readonly ICajaRepository _cajas;
    private readonly ILiquidacionRepository _liquidaciones;
    private readonly IVentasPdfExporter _ventasPdf;
    private readonly IVentasCsvExporter _ventasCsv;
    private readonly ICajaRangoPdfExporter _cajaPdf;
    private readonly ICajaRangoCsvExporter _cajaCsv;
    private readonly INominaPdfExporter _nominaPdf;
    private readonly INominaCsvExporter _nominaCsv;

    public ReportesService(
        IPagoRepository pagos,
        ICajaRepository cajas,
        ILiquidacionRepository liquidaciones,
        IVentasPdfExporter ventasPdf,
        IVentasCsvExporter ventasCsv,
        ICajaRangoPdfExporter cajaPdf,
        ICajaRangoCsvExporter cajaCsv,
        INominaPdfExporter nominaPdf,
        INominaCsvExporter nominaCsv)
    {
        _pagos = pagos;
        _cajas = cajas;
        _liquidaciones = liquidaciones;
        _ventasPdf = ventasPdf;
        _ventasCsv = ventasCsv;
        _cajaPdf = cajaPdf;
        _cajaCsv = cajaCsv;
        _nominaPdf = nominaPdf;
        _nominaCsv = nominaCsv;
    }

    public async Task<VentasReporteDto> GetVentasAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagos = await _pagos.ListCompletadosByFechaPagoRangoAsync(
            query.Desde, query.Hasta, cancellationToken);

        decimal efectivo = 0, tarjeta = 0, transferencia = 0;
        foreach (var p in pagos)
        {
            switch (p.Metodo)
            {
                case PagoMetodo.Efectivo:
                    efectivo += p.Monto;
                    break;
                case PagoMetodo.Tarjeta:
                    tarjeta += p.Monto;
                    break;
                case PagoMetodo.Transferencia:
                    transferencia += p.Monto;
                    break;
            }
        }

        var dias = pagos
            .GroupBy(p => DateOnly.FromDateTime(DateTime.SpecifyKind(p.FechaPago, DateTimeKind.Utc)))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                decimal dEfectivo = 0, dTarjeta = 0, dTransferencia = 0;
                foreach (var p in g)
                {
                    switch (p.Metodo)
                    {
                        case PagoMetodo.Efectivo:
                            dEfectivo += p.Monto;
                            break;
                        case PagoMetodo.Tarjeta:
                            dTarjeta += p.Monto;
                            break;
                        case PagoMetodo.Transferencia:
                            dTransferencia += p.Monto;
                            break;
                    }
                }

                return new VentasDiaDto
                {
                    Fecha = g.Key,
                    TotalEfectivo = dEfectivo,
                    TotalTarjeta = dTarjeta,
                    TotalTransferencia = dTransferencia,
                    Total = dEfectivo + dTarjeta + dTransferencia,
                    CantidadPagos = g.Count()
                };
            })
            .ToList();

        return new VentasReporteDto
        {
            Desde = query.Desde,
            Hasta = query.Hasta,
            TotalEfectivo = efectivo,
            TotalTarjeta = tarjeta,
            TotalTransferencia = transferencia,
            Total = efectivo + tarjeta + transferencia,
            CantidadPagos = pagos.Count,
            CantidadPedidos = pagos.Select(p => p.PedidoId).Distinct().Count(),
            Dias = dias
        };
    }

    public async Task<CajaRangoReporteDto> GetCajaRangoAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var filas = await _cajas.ListByFechaRangoAsync(query.Desde, query.Hasta, cancellationToken);
        var dias = filas.Select(c => new CajaDiaDto
        {
            Fecha = c.Fecha,
            TotalEfectivo = c.TotalEfectivo,
            TotalTarjeta = c.TotalTarjeta,
            TotalTransferencia = c.TotalTransferencia,
            Total = c.Total
        }).ToList();

        return new CajaRangoReporteDto
        {
            Desde = query.Desde,
            Hasta = query.Hasta,
            TotalEfectivo = dias.Sum(d => d.TotalEfectivo),
            TotalTarjeta = dias.Sum(d => d.TotalTarjeta),
            TotalTransferencia = dias.Sum(d => d.TotalTransferencia),
            Total = dias.Sum(d => d.Total),
            Dias = dias
        };
    }

    public async Task<NominaReporteDto> GetNominaAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var liquidaciones = await _liquidaciones.ListByPeriodoIntersectAsync(
            query.Desde, query.Hasta, cancellationToken);

        var lineas = liquidaciones.Select(l => new NominaLineaDto
        {
            Id = l.Id,
            EmpleadoId = l.EmpleadoId,
            EmpleadoNombre = l.Empleado?.Nombre ?? string.Empty,
            PeriodoDesde = l.PeriodoDesde,
            PeriodoHasta = l.PeriodoHasta,
            Horas = l.Horas,
            TarifaHoraSnapshot = l.TarifaHoraSnapshot,
            Monto = l.Monto,
            GeneradaEnUtc = l.GeneradaEnUtc
        }).ToList();

        return new NominaReporteDto
        {
            Desde = query.Desde,
            Hasta = query.Hasta,
            TotalHoras = lineas.Sum(x => x.Horas),
            TotalMonto = lineas.Sum(x => x.Monto),
            Lineas = lineas
        };
    }

    public async Task<ReportExportResult> ExportVentasPdfAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetVentasAsync(query, cancellationToken);
        return new ReportExportResult
        {
            Content = _ventasPdf.Export(dto),
            ContentType = "application/pdf",
            FileName = $"ventas-{query.Desde:yyyy-MM-dd}_{query.Hasta:yyyy-MM-dd}.pdf"
        };
    }

    public async Task<ReportExportResult> ExportVentasCsvAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetVentasAsync(query, cancellationToken);
        return new ReportExportResult
        {
            Content = _ventasCsv.Export(dto),
            ContentType = "text/csv",
            FileName = $"ventas-{query.Desde:yyyy-MM-dd}_{query.Hasta:yyyy-MM-dd}.csv"
        };
    }

    public async Task<ReportExportResult> ExportCajaRangoPdfAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetCajaRangoAsync(query, cancellationToken);
        return new ReportExportResult
        {
            Content = _cajaPdf.Export(dto),
            ContentType = "application/pdf",
            FileName = $"caja-rango-{query.Desde:yyyy-MM-dd}_{query.Hasta:yyyy-MM-dd}.pdf"
        };
    }

    public async Task<ReportExportResult> ExportCajaRangoCsvAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetCajaRangoAsync(query, cancellationToken);
        return new ReportExportResult
        {
            Content = _cajaCsv.Export(dto),
            ContentType = "text/csv",
            FileName = $"caja-rango-{query.Desde:yyyy-MM-dd}_{query.Hasta:yyyy-MM-dd}.csv"
        };
    }

    public async Task<ReportExportResult> ExportNominaPdfAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetNominaAsync(query, cancellationToken);
        return new ReportExportResult
        {
            Content = _nominaPdf.Export(dto),
            ContentType = "application/pdf",
            FileName = $"nomina-{query.Desde:yyyy-MM-dd}_{query.Hasta:yyyy-MM-dd}.pdf"
        };
    }

    public async Task<ReportExportResult> ExportNominaCsvAsync(
        ReporteRangoQuery query,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetNominaAsync(query, cancellationToken);
        return new ReportExportResult
        {
            Content = _nominaCsv.Export(dto),
            ContentType = "text/csv",
            FileName = $"nomina-{query.Desde:yyyy-MM-dd}_{query.Hasta:yyyy-MM-dd}.csv"
        };
    }
}
