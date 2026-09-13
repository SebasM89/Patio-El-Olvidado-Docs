using PatioElOlvidado.Application.DTOs.Reportes;

namespace PatioElOlvidado.Application.Interfaces;

public interface IVentasPdfExporter
{
    byte[] Export(VentasReporteDto reporte);
}

public interface IVentasCsvExporter
{
    byte[] Export(VentasReporteDto reporte);
}

public interface ICajaRangoPdfExporter
{
    byte[] Export(CajaRangoReporteDto reporte);
}

public interface ICajaRangoCsvExporter
{
    byte[] Export(CajaRangoReporteDto reporte);
}

public interface INominaPdfExporter
{
    byte[] Export(NominaReporteDto reporte);
}

public interface INominaCsvExporter
{
    byte[] Export(NominaReporteDto reporte);
}
