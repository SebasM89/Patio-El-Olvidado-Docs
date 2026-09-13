using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PatioElOlvidado.Application.DTOs.Reportes;
using PatioElOlvidado.Application.Interfaces;

namespace PatioElOlvidado.Infrastructure.Export;

public class CsvHelperVentasExporter : IVentasCsvExporter
{
    public byte[] Export(VentasReporteDto reporte)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            csv.WriteField("Seccion");
            csv.WriteField("Fecha");
            csv.WriteField("TotalEfectivo");
            csv.WriteField("TotalTarjeta");
            csv.WriteField("TotalTransferencia");
            csv.WriteField("Total");
            csv.WriteField("CantidadPagos");
            csv.WriteField("CantidadPedidos");
            csv.NextRecord();

            csv.WriteField("Resumen");
            csv.WriteField($"{reporte.Desde:yyyy-MM-dd}_{reporte.Hasta:yyyy-MM-dd}");
            csv.WriteField(reporte.TotalEfectivo.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField(reporte.TotalTarjeta.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField(reporte.TotalTransferencia.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField(reporte.Total.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField(reporte.CantidadPagos.ToString(CultureInfo.InvariantCulture));
            csv.WriteField(reporte.CantidadPedidos.ToString(CultureInfo.InvariantCulture));
            csv.NextRecord();

            foreach (var d in reporte.Dias)
            {
                csv.WriteField("Dia");
                csv.WriteField(d.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                csv.WriteField(d.TotalEfectivo.ToString("0.00", CultureInfo.InvariantCulture));
                csv.WriteField(d.TotalTarjeta.ToString("0.00", CultureInfo.InvariantCulture));
                csv.WriteField(d.TotalTransferencia.ToString("0.00", CultureInfo.InvariantCulture));
                csv.WriteField(d.Total.ToString("0.00", CultureInfo.InvariantCulture));
                csv.WriteField(d.CantidadPagos.ToString(CultureInfo.InvariantCulture));
                csv.WriteField("");
                csv.NextRecord();
            }
        }

        return stream.ToArray();
    }
}
