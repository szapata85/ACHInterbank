using Cfa.ACHInterbank.Persistence.Reports.Base;
using Cfa.ACHInterbank.Persistence.Reports.Components;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using Cfa.ACHInterbank.Application.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Documents;

internal sealed class AchReconciliationReportDocument : BaseReportDocument<AchReconciliationReportDocumentModel>
{
    public AchReconciliationReportDocument(AchReconciliationReportDocumentModel model, PdfBrandingOptions? branding = null) : base(model, branding)
    {
    }

    protected override string Title => "Reporte de conciliación ACH";

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
                    ["Enviados"] = $"{Model.Totals.SentCount} / {Model.Totals.SentAmount:N2}",
                    ["Recibidos"] = $"{Model.Totals.ReceivedCount} / {Model.Totals.ReceivedAmount:N2}",
                    ["Devueltos"] = $"{Model.Totals.ReturnedCount} / {Model.Totals.ReturnedAmount:N2}"
                });

            ReportSectionComposer.ComposeFiltersBlock(column.Item(),
                new Dictionary<string, string>
                {
                    ["Fecha"] = Model.Filter.Date?.ToString("yyyy-MM-dd") ?? "Todas",
                    ["Cámara"] = Model.Filter.ClearingHouseId?.ToString() ?? "Todas",
                    ["Ciclo"] = string.IsNullOrWhiteSpace(Model.Filter.AchCycleId) ? "Todos" : Model.Filter.AchCycleId
                });

            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(diff =>
            {
                diff.Spacing(4);
                diff.Item().Text("Diferencias destacadas").SemiBold();
                diff.Item().Text($"Enviados vs Recibidos: {Model.Differences.SentVsReceivedCountDiff} / {Model.Differences.SentVsReceivedAmountDiff:N2}")
                    .FontColor(Model.Differences.SentVsReceivedCountDiff == 0 && Model.Differences.SentVsReceivedAmountDiff == 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                diff.Item().Text($"Enviados vs Devueltos: {Model.Differences.SentVsReturnedCountDiff} / {Model.Differences.SentVsReturnedAmountDiff:N2}")
                    .FontColor(Model.Differences.SentVsReturnedCountDiff == 0 && Model.Differences.SentVsReturnedAmountDiff == 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                diff.Item().Text($"Recibidos vs Devueltos: {Model.Differences.ReceivedVsReturnedCountDiff} / {Model.Differences.ReceivedVsReturnedAmountDiff:N2}")
                    .FontColor(Model.Differences.ReceivedVsReturnedCountDiff == 0 && Model.Differences.ReceivedVsReturnedAmountDiff == 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
            });

            ReportSectionComposer.ComposeDataTable(column.Item(),
                headers: ["Código", "Descripción", "Afectados"],
                defineColumns: cols =>
                {
                    cols.RelativeColumn(1.2f);
                    cols.RelativeColumn(3f);
                    cols.RelativeColumn(1f);
                },
                rows: Model.Inconsistencies,
                composeRow: (table, row) =>
                {
                    table.Cell().PaddingVertical(3).Text(row.Code);
                    table.Cell().PaddingVertical(3).Text(row.Description);
                    table.Cell().PaddingVertical(3).AlignRight().Text(row.AffectedCount.ToString());
                },
                emptyMessage: "Sin inconsistencias para los filtros indicados.");
        });
    }
}
