using Cfa.ACHInterbank.Persistence.Reports.Base;
using Cfa.ACHInterbank.Persistence.Reports.Components;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Documents;

internal sealed class TraceabilityReportDocument : BaseReportDocument<TraceabilityReportDocumentModel>
{
    public TraceabilityReportDocument(TraceabilityReportDocumentModel model) : base(model)
    {
    }

    protected override string Title => "Reporte de trazabilidad ACH";

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
                    ["Registros"] = Model.Rows.Count.ToString(),
                    ["Estado"] = string.IsNullOrWhiteSpace(Model.Filter.State) ? "Todos" : Model.Filter.State,
                    ["Ciclo ACH"] = string.IsNullOrWhiteSpace(Model.Filter.AchCycleId) ? "Todos" : Model.Filter.AchCycleId
                });

            ReportSectionComposer.ComposeFiltersBlock(column.Item(),
                new Dictionary<string, string>
                {
                    ["Desde (UTC)"] = Model.Filter.FromUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Sin límite",
                    ["Hasta (UTC)"] = Model.Filter.ToUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Sin límite",
                    ["Estado"] = string.IsNullOrWhiteSpace(Model.Filter.State) ? "Todos" : Model.Filter.State,
                    ["Ciclo ACH"] = string.IsNullOrWhiteSpace(Model.Filter.AchCycleId) ? "Todos" : Model.Filter.AchCycleId
                });

            ReportSectionComposer.ComposeDataTable(column.Item(),
                headers:
                [
                    "Transacción",
                    "Referencia",
                    "Trace",
                    "Monto",
                    "Estado",
                    "Ciclo ACH",
                    "Actualizado (UTC)",
                    "Instituciones"
                ],
                defineColumns: columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(2.2f);
                },
                rows: Model.Rows,
                composeRow: (table, row) =>
                {
                    table.Cell().PaddingVertical(3).Text(row.TransactionId.ToString()).FontSize(9);
                    table.Cell().PaddingVertical(3).Text(row.Reference ?? string.Empty).FontSize(9);
                    table.Cell().PaddingVertical(3).Text(row.TraceNumber ?? string.Empty).FontSize(9);
                    table.Cell().PaddingVertical(3).AlignRight().Text($"{row.Amount:N2}").FontSize(9);
                    table.Cell().PaddingVertical(3).Text(row.State.ToString()).FontSize(9);
                    table.Cell().PaddingVertical(3).Text(row.AchCycleId ?? string.Empty).FontSize(9);
                    table.Cell().PaddingVertical(3).Text($"{row.StateChangedAtUtc:yyyy-MM-dd HH:mm:ss}").FontSize(9);
                    table.Cell().PaddingVertical(3).Text($"{row.SourceInstitutionName} → {row.DestinationInstitutionName}").FontSize(9);
                },
                emptyMessage: "No hay datos para los filtros indicados.");
        });
    }
}
