using Cfa.ACHInterbank.Persistence.Reports.Base;
using Cfa.ACHInterbank.Persistence.Reports.Components;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Documents;

internal sealed class AchNachaFileReportDocument : BaseReportDocument<AchNachaFileReportDocumentModel>
{
    public AchNachaFileReportDocument(AchNachaFileReportDocumentModel model) : base(model)
    {
    }

    protected override string Title => "Reporte de archivos NACHA";

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
                    ["Archivos"] = Model.Totals.TotalFiles.ToString(),
                    ["Total registros"] = Model.Totals.TotalRecords.ToString(),
                    ["Total transacciones"] = Model.Totals.TotalTransactions.ToString()
                });

            ReportSectionComposer.ComposeFiltersBlock(column.Item(),
                new Dictionary<string, string>
                {
                    ["Fecha"] = Model.Filter.Date?.ToString("yyyy-MM-dd") ?? "Todas",
                    ["Cámara"] = Model.Filter.ClearingHouseId?.ToString() ?? "Todas"
                });

            ReportSectionComposer.ComposeDataTable(column.Item(),
                headers: ["Archivo", "Fecha UTC", "Cámara", "Tipo", "Registros", "Transacciones"],
                defineColumns: columns =>
                {
                    columns.RelativeColumn(2.6f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.3f);
                },
                rows: Model.Rows,
                composeRow: (table, row) =>
                {
                    table.Cell().PaddingVertical(3).Text(row.FileName);
                    table.Cell().PaddingVertical(3).Text(row.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm"));
                    table.Cell().PaddingVertical(3).Text(row.ClearingHouseName);
                    table.Cell().PaddingVertical(3).Text(row.ExportKind);
                    table.Cell().PaddingVertical(3).AlignRight().Text(row.TotalRecords.ToString());
                    table.Cell().PaddingVertical(3).AlignRight().Text(row.TotalTransactions.ToString());
                },
                emptyMessage: "No hay archivos para los filtros indicados.");
        });
    }
}
