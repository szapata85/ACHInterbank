using Cfa.ACHInterbank.Persistence.Reports.Base;
using Cfa.ACHInterbank.Persistence.Reports.Components;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Documents;

internal sealed class AchReturnRejectionReportDocument : BaseReportDocument<AchReturnRejectionReportDocumentModel>
{
    public AchReturnRejectionReportDocument(AchReturnRejectionReportDocumentModel model) : base(model)
    {
    }

    protected override string Title => Model.Title;

    protected override DateTime GeneratedAtUtc => Model.GeneratedAtUtc;

    protected override void ComposeBody(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            ReportSectionComposer.ComposeTitleAndMetadata(column.Item(),
                "Resumen del reporte",
                new Dictionary<string, string>
                {
                    ["Registros"] = Model.Totals.TotalRecords.ToString(),
                    ["Monto total"] = $"{Model.Totals.TotalAmount:N2}"
                });

            ReportSectionComposer.ComposeFiltersBlock(column.Item(),
                new Dictionary<string, string>
                {
                    ["Fecha"] = Model.Filter.Date?.ToString("yyyy-MM-dd") ?? "Todas",
                    ["Causal"] = string.IsNullOrWhiteSpace(Model.Filter.Causal) ? "Todas" : Model.Filter.Causal,
                    ["Cámara"] = Model.Filter.ClearingHouseId?.ToString() ?? "Todas",
                    ["Estado"] = Model.Filter.State?.ToString() ?? "Todos",
                    ["Referencia"] = string.IsNullOrWhiteSpace(Model.Filter.Reference) ? "Todas" : Model.Filter.Reference
                });

            ReportSectionComposer.ComposeDataTable(column.Item(),
                headers:
                [
                    "ID",
                    "Fecha",
                    "Referencia",
                    "Monto",
                    "Estado",
                    "Causal",
                    "Relación transacción",
                    "Cámara/Ciclo"
                ],
                defineColumns: columns =>
                {
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.8f);
                },
                rows: Model.Rows,
                composeRow: (table, row) =>
                {
                    table.Cell().PaddingVertical(3).Text(row.TransactionId.ToString());
                    table.Cell().PaddingVertical(3).Text(row.EffectiveEntryDate.ToString("yyyy-MM-dd"));
                    table.Cell().PaddingVertical(3).Text(row.Reference);
                    table.Cell().PaddingVertical(3).AlignRight().Text($"{row.Amount:N2}");
                    table.Cell().PaddingVertical(3).Text(row.State.ToString());
                    table.Cell().PaddingVertical(3).Text($"{row.CausalCode} {row.CausalDescription}".Trim());
                    table.Cell().PaddingVertical(3).Text(
                        row.OriginalTransactionId.HasValue
                            ? $"Tx {row.OriginalTransactionId} / Ref {row.OriginalTransactionReference} / Trace {row.OriginalTraceRef}"
                            : string.IsNullOrWhiteSpace(row.OriginalTraceRef)
                                ? "N/A"
                                : $"Trace {row.OriginalTraceRef}");
                    table.Cell().PaddingVertical(3).Text($"{row.ClearingHouseName} / {row.AchCycleName}");
                },
                emptyMessage: "No hay datos para los filtros indicados.");
        });
    }
}
