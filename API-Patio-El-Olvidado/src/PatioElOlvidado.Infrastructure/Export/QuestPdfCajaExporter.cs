using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PatioElOlvidado.Infrastructure.Export;

public class QuestPdfCajaExporter : ICajaPdfExporter
{
    static QuestPdfCajaExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(CajaDiaDto caja)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Text("Caja diaria — Patio El Olvidado")
                    .SemiBold().FontSize(18).FontColor(Colors.Brown.Darken2);

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Fecha (UTC): {caja.Fecha:yyyy-MM-dd}");
                    col.Item().Text($"Efectivo: {caja.TotalEfectivo:0.00}");
                    col.Item().Text($"Tarjeta: {caja.TotalTarjeta:0.00}");
                    col.Item().Text($"Transferencia: {caja.TotalTransferencia:0.00}");
                    col.Item().PaddingTop(8).Text($"Total: {caja.Total:0.00}").SemiBold().FontSize(14);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generado UTC ");
                    text.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm}");
                });
            });
        });

        return document.GeneratePdf();
    }
}
