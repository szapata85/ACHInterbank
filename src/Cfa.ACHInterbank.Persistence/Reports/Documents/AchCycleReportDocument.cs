using Cfa.ACHInterbank.Persistence.Reports.Base;
using Cfa.ACHInterbank.Persistence.Reports.Components;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Documents;

internal sealed class AchCycleReportDocument : BaseReportDocument<AchCycleReportDocumentModel>
{
    public AchCycleReportDocument(AchCycleReportDocumentModel model) : base(model)
    {
    }

    protected override string Title => "Reporte de ciclos ACH";

    protected override DateTime GeneratedAtUtc => Model.GeneratedAtUtc;

    protected override void ComposeBody(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            ReportSectionComposer.ComposeTitleAndMetadata(column.Item(),
                "Resumen ejecutivo",
                new Dictionary<string, string>
                {
                    ["Ciclos"] = Model.Totals.TotalCycles.ToString(),
                    ["Total transacciones"] = Model.Totals.TotalTransactions.ToString(),
                    ["Monto total"] = $"{Model.Totals.TotalAmount:N2}"
                });

            ReportSectionComposer.ComposeFiltersBlock(column.Item(),
                new Dictionary<string, string>
                {
                    ["Fecha"] = Model.Filter.Date?.ToString("yyyy-MM-dd") ?? "Todas",
                    ["Cámara"] = Model.Filter.ClearingHouseId?.ToString() ?? "Todas",
                    ["Nombre ciclo"] = string.IsNullOrWhiteSpace(Model.Filter.Name) ? "Todos" : Model.Filter.Name
                });

            ReportSectionComposer.ComposeDataTable(column.Item(),
                headers: ["Nombre", "Horario", "Fecha", "Cámara", "Estado", "Transacciones", "Monto"],
                defineColumns: columns =>
                {
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.7f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.4f);
                },
                rows: Model.Rows,
                composeRow: (table, row) =>
                {
                    table.Cell().PaddingVertical(3).Text(row.CycleName);
                    table.Cell().PaddingVertical(3).Text(row.Schedule);
                    table.Cell().PaddingVertical(3).Text(row.ProcessingDate.ToString("yyyy-MM-dd"));
                    table.Cell().PaddingVertical(3).Text(row.ClearingHouseName);
                    table.Cell().PaddingVertical(3).Text(row.Status);
                    table.Cell().PaddingVertical(3).AlignRight().Text(row.TotalTransactions.ToString());
                    table.Cell().PaddingVertical(3).AlignRight().Text($"{row.TotalAmount:N2}");
                },
                emptyMessage: "No hay ciclos para los filtros indicados.");
        });
    }
}
