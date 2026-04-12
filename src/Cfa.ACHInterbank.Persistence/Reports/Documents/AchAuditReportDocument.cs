using Cfa.ACHInterbank.Persistence.Reports.Base;
using Cfa.ACHInterbank.Persistence.Reports.Components;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Documents;

internal sealed class AchAuditReportDocument : BaseReportDocument<AchAuditReportDocumentModel>
{
    public AchAuditReportDocument(AchAuditReportDocumentModel model) : base(model)
    {
    }

    protected override string Title => "Reporte de auditoría";

    protected override DateTime GeneratedAtUtc => Model.GeneratedAtUtc;

    protected override void ComposeBody(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            ReportSectionComposer.ComposeTitleAndMetadata(column.Item(),
                "Trazabilidad",
                new Dictionary<string, string>
                {
                    ["Eventos"] = Model.Rows.Count.ToString(),
                    ["Usuario"] = string.IsNullOrWhiteSpace(Model.Filter.User) ? "Todos" : Model.Filter.User,
                    ["Entidad"] = string.IsNullOrWhiteSpace(Model.Filter.Entity) ? "Todas" : Model.Filter.Entity
                });

            ReportSectionComposer.ComposeDataTable(column.Item(),
                headers: ["Usuario", "Acción", "Entidad", "ID Entidad", "Fecha UTC"],
                defineColumns: cols =>
                {
                    cols.RelativeColumn(1.6f);
                    cols.RelativeColumn(1.6f);
                    cols.RelativeColumn(1.5f);
                    cols.RelativeColumn(1.5f);
                    cols.RelativeColumn(1.7f);
                },
                rows: Model.Rows,
                composeRow: (table, row) =>
                {
                    table.Cell().PaddingVertical(3).Text(row.User);
                    table.Cell().PaddingVertical(3).Text(row.Action);
                    table.Cell().PaddingVertical(3).Text(row.Entity);
                    table.Cell().PaddingVertical(3).Text(row.EntityId);
                    table.Cell().PaddingVertical(3).Text(row.DateUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                },
                emptyMessage: "No hay registros de auditoría para los filtros indicados.");
        });
    }
}
