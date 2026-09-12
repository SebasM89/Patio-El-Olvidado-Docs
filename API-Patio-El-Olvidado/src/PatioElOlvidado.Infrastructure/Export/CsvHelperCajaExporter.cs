using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.Interfaces;

namespace PatioElOlvidado.Infrastructure.Export;

public class CsvHelperCajaExporter : ICajaCsvExporter
{
    public byte[] Export(CajaDiaDto caja)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            csv.WriteField("Fecha");
            csv.WriteField("TotalEfectivo");
            csv.WriteField("TotalTarjeta");
            csv.WriteField("TotalTransferencia");
            csv.WriteField("Total");
            csv.NextRecord();

            csv.WriteField(caja.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            csv.WriteField(caja.TotalEfectivo.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField(caja.TotalTarjeta.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField(caja.TotalTransferencia.ToString("0.00", CultureInfo.InvariantCulture));
            csv.WriteField(caja.Total.ToString("0.00", CultureInfo.InvariantCulture));
            csv.NextRecord();
        }

        return stream.ToArray();
    }
}
