using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports;

[Scoped]
public class QuestPdfReportGenerator : IReportGenerator
{
    private readonly IAchTraceabilityService _traceabilityService;

    public QuestPdfReportGenerator(IAchTraceabilityService traceabilityService)
    {
        _traceabilityService = traceabilityService;
        Settings.License = LicenseType.Community;
    }

    public async Task<GeneratedReportFile> GenerateTraceabilityPdfAsync(TraceabilityReportFilter filter, CancellationToken ct = default)
    {
        var rows = await _traceabilityService.GetTraceabilityReportAsync(
            filter.FromUtc,
            filter.ToUtc,
            filter.State,
            filter.AchCycleId,
            ct);

        var generatedAt = DateTime.UtcNow;
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4.Landscape());

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("Reporte de trazabilidad ACH").SemiBold().FontSize(18);
                        column.Item().Text($"Generado UTC: {generatedAt:yyyy-MM-dd HH:mm:ss}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    });

                page.Content()
                    .PaddingVertical(10)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f); // TransactionId
                            columns.RelativeColumn(2f);   // Reference
                            columns.RelativeColumn(2f);   // Trace
                            columns.RelativeColumn(1.2f); // Amount
                            columns.RelativeColumn(1.6f); // State
                            columns.RelativeColumn(2f);   // Cycle
                            columns.RelativeColumn(2.2f); // Updated
                            columns.RelativeColumn(2.2f); // Institutions
                        });

                        static void HeaderCell(IContainer cell, string text)
                        {
                            cell.BorderBottom(1).PaddingVertical(4).Text(text).SemiBold().FontSize(10);
                        }

                        HeaderCell(table.Header().Cell(), "Transacción");
                        HeaderCell(table.Header().Cell(), "Referencia");
                        HeaderCell(table.Header().Cell(), "Trace");
                        HeaderCell(table.Header().Cell(), "Monto");
                        HeaderCell(table.Header().Cell(), "Estado");
                        HeaderCell(table.Header().Cell(), "Ciclo ACH");
                        HeaderCell(table.Header().Cell(), "Actualizado (UTC)");
                        HeaderCell(table.Header().Cell(), "Instituciones");

                        if (rows.Count == 0)
                        {
                            table.Cell().ColumnSpan(8).Padding(6).Text("No hay datos para los filtros indicados.").Italic();
                            return;
                        }

                        foreach (var row in rows)
                        {
                            table.Cell().PaddingVertical(3).Text(row.TransactionId.ToString());
                            table.Cell().PaddingVertical(3).Text(row.Reference ?? string.Empty);
                            table.Cell().PaddingVertical(3).Text(row.TraceNumber ?? string.Empty);
                            table.Cell().PaddingVertical(3).AlignRight().Text($"{row.Amount:N2}");
                            table.Cell().PaddingVertical(3).Text(row.State.ToString());
                            table.Cell().PaddingVertical(3).Text(row.AchCycleId ?? string.Empty);
                            table.Cell().PaddingVertical(3).Text($"{row.StateChangedAtUtc:yyyy-MM-dd HH:mm:ss}");
                            table.Cell().PaddingVertical(3).Text($"{row.SourceInstitutionName} → {row.DestinationInstitutionName}");
                        }
                    });

                page.Footer()
                    .AlignRight()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf();

        return new GeneratedReportFile
        {
            Content = bytes,
            ContentType = "application/pdf",
            FileName = $"ACH_Traceability_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }
}

