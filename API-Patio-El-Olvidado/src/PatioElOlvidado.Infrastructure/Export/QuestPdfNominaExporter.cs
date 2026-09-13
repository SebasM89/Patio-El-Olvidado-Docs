using PatioElOlvidado.Application.DTOs.Reportes;
using PatioElOlvidado.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PatioElOlvidado.Infrastructure.Export;

public class QuestPdfNominaExporter : INominaPdfExporter
{
    static QuestPdfNominaExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(NominaReporteDto reporte)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Text("Reporte de nómina — Patio El Olvidado")
                    .SemiBold().FontSize(16).FontColor(Colors.Brown.Darken2);

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Rango (UTC): {reporte.Desde:yyyy-MM-dd} → {reporte.Hasta:yyyy-MM-dd}");
                    col.Item().Text($"Total horas: {reporte.TotalHoras:0.00}");
                    col.Item().Text($"Total monto: {reporte.TotalMonto:0.00}").SemiBold();

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(0.7f);
                            columns.RelativeColumn(0.7f);
                            columns.RelativeColumn(0.8f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Id").SemiBold();
                            header.Cell().Text("Empleado").SemiBold();
                            header.Cell().Text("Desde").SemiBold();
                            header.Cell().Text("Hasta").SemiBold();
                            header.Cell().AlignRight().Text("Horas").SemiBold();
                            header.Cell().AlignRight().Text("Tarifa").SemiBold();
                            header.Cell().AlignRight().Text("Monto").SemiBold();
                        });

                        foreach (var l in reporte.Lineas)
                        {
                            table.Cell().Text($"{l.Id}");
                            table.Cell().Text(l.EmpleadoNombre);
                            table.Cell().Text($"{l.PeriodoDesde:yyyy-MM-dd}");
                            table.Cell().Text($"{l.PeriodoHasta:yyyy-MM-dd}");
                            table.Cell().AlignRight().Text($"{l.Horas:0.00}");
                            table.Cell().AlignRight().Text($"{l.TarifaHoraSnapshot:0.00}");
                            table.Cell().AlignRight().Text($"{l.Monto:0.00}");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generado UTC ");
                    text.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm}");
                    text.Span(" — Sin datos AFIP");
                });
            });
        });

        return document.GeneratePdf();
    }
}
