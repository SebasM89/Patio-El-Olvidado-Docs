using PatioElOlvidado.Application.DTOs.Reportes;
using PatioElOlvidado.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PatioElOlvidado.Infrastructure.Export;

public class QuestPdfCajaRangoExporter : ICajaRangoPdfExporter
{
    static QuestPdfCajaRangoExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(CajaRangoReporteDto reporte)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Reporte de caja (rango) — Patio El Olvidado")
                    .SemiBold().FontSize(16).FontColor(Colors.Brown.Darken2);

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Rango (UTC): {reporte.Desde:yyyy-MM-dd} → {reporte.Hasta:yyyy-MM-dd}");
                    col.Item().Text($"Efectivo: {reporte.TotalEfectivo:0.00}");
                    col.Item().Text($"Tarjeta: {reporte.TotalTarjeta:0.00}");
                    col.Item().Text($"Transferencia: {reporte.TotalTransferencia:0.00}");
                    col.Item().Text($"Total: {reporte.Total:0.00}").SemiBold();

                    col.Item().PaddingTop(12).Text("Días con caja").SemiBold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Fecha").SemiBold();
                            header.Cell().AlignRight().Text("Efectivo").SemiBold();
                            header.Cell().AlignRight().Text("Tarjeta").SemiBold();
                            header.Cell().AlignRight().Text("Transfer.").SemiBold();
                            header.Cell().AlignRight().Text("Total").SemiBold();
                        });

                        foreach (var d in reporte.Dias)
                        {
                            table.Cell().Text($"{d.Fecha:yyyy-MM-dd}");
                            table.Cell().AlignRight().Text($"{d.TotalEfectivo:0.00}");
                            table.Cell().AlignRight().Text($"{d.TotalTarjeta:0.00}");
                            table.Cell().AlignRight().Text($"{d.TotalTransferencia:0.00}");
                            table.Cell().AlignRight().Text($"{d.Total:0.00}");
                        }
                    });
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
