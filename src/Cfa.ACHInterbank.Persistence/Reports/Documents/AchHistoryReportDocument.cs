using Cfa.ACHInterbank.Persistence.Reports.Base;
using Cfa.ACHInterbank.Persistence.Reports.Components;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Documents;

internal sealed class AchHistoryReportDocument : BaseReportDocument<AchHistoryReportDocumentModel>
{
    public AchHistoryReportDocument(AchHistoryReportDocumentModel model) : base(model)
    {
    }

    protected override string Title => "Reporte histórico ACH";

    protected override DateTime GeneratedAtUtc => Model.GeneratedAtUtc;

    protected override void ComposeBody(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            ReportSectionComposer.ComposeTitleAndMetadata(column.Item(),
                "Trazabilidad histórica",
                new Dictionary<string, string>
                {
                    ["Eventos"] = Model.Rows.Count.ToString(),
                    ["Desde"] = Model.Filter.FromUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                    ["Hasta"] = Model.Filter.ToUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
                });

            ReportSectionComposer.ComposeDataTable(column.Item(),
                headers: ["Tx", "Estado origen", "Estado destino", "Fuente", "Causal", "Fecha UTC", "Usuario"],
                defineColumns: cols =>
                {
                    cols.RelativeColumn(0.9f);
                    cols.RelativeColumn(1.2f);
                    cols.RelativeColumn(1.2f);
                    cols.RelativeColumn(1.1f);
                    cols.RelativeColumn(1.1f);
                    cols.RelativeColumn(1.8f);
                    cols.RelativeColumn(1.4f);
                },
                rows: Model.Rows,
                composeRow: (table, row) =>
                {
                    table.Cell().PaddingVertical(3).Text(row.TransactionId.ToString());
                    table.Cell().PaddingVertical(3).Text(row.FromState.ToString());
                    table.Cell().PaddingVertical(3).Text(row.ToState.ToString());
                    table.Cell().PaddingVertical(3).Text(row.Source.ToString());
                    table.Cell().PaddingVertical(3).Text(row.ReasonCode ?? "-");
                    table.Cell().PaddingVertical(3).Text(row.DateUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                    table.Cell().PaddingVertical(3).Text(row.ChangedBy ?? "-");
                },
                emptyMessage: "No hay histórico para los filtros indicados.");
        });
    }
}
