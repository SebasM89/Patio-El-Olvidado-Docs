using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PatioElOlvidado.Application.DTOs.Reportes;
using PatioElOlvidado.Application.Interfaces;

namespace PatioElOlvidado.Infrastructure.Export;

public class CsvHelperNominaExporter : INominaCsvExporter
{
    public byte[] Export(NominaReporteDto reporte)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            csv.WriteField("Id");
            csv.WriteField("EmpleadoId");
            csv.WriteField("EmpleadoNombre");
            csv.WriteField("PeriodoDesde");
            csv.WriteField("PeriodoHasta");
            csv.WriteField("Horas");
            csv.WriteField("TarifaHoraSnapshot");
            csv.WriteField("Monto");
            csv.WriteField("GeneradaEnUtc");
            csv.NextRecord();

            foreach (var l in reporte.Lineas)
            {
                csv.WriteField(l.Id.ToString(CultureInfo.InvariantCulture));
                csv.WriteField(l.EmpleadoId.ToString(CultureInfo.InvariantCulture));
                csv.WriteField(l.EmpleadoNombre);
                csv.WriteField(l.PeriodoDesde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                csv.WriteField(l.PeriodoHasta.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                csv.WriteField(l.Horas.ToString("0.00", CultureInfo.InvariantCulture));
                csv.WriteField(l.TarifaHoraSnapshot.ToString("0.00", CultureInfo.InvariantCulture));
                csv.WriteField(l.Monto.ToString("0.00", CultureInfo.InvariantCulture));
                csv.WriteField(l.GeneradaEnUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                csv.NextRecord();
            }

            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField("TOTALES");
            csv.WriteField("");
            csv.WriteField("");
            csv.WriteField(reporte.TotalHoras.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField("");
            csv.WriteField(reporte.TotalMonto.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField("");
            csv.NextRecord();
        }

        return stream.ToArray();
    }
}
