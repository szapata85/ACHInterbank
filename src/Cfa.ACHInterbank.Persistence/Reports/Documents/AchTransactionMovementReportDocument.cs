using Cfa.ACHInterbank.Persistence.Reports.Base;
using Cfa.ACHInterbank.Persistence.Reports.Components;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Documents;

internal sealed class AchTransactionMovementReportDocument : BaseReportDocument<AchTransactionMovementReportDocumentModel>
{
    public AchTransactionMovementReportDocument(AchTransactionMovementReportDocumentModel model) : base(model)
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
                    ["Total créditos"] = $"{Model.Totals.TotalCreditAmount:N2}",
                    ["Total débitos"] = $"{Model.Totals.TotalDebitAmount:N2}"
                });

            ReportSectionComposer.ComposeFiltersBlock(column.Item(),
                new Dictionary<string, string>
                {
                    ["Fecha"] = Model.Filter.Date?.ToString("yyyy-MM-dd") ?? "Todas",
                    ["Cámara"] = Model.Filter.ClearingHouseId?.ToString() ?? "Todas",
                    ["Ciclo"] = string.IsNullOrWhiteSpace(Model.Filter.AchCycleId) ? "Todos" : Model.Filter.AchCycleId,
                    ["Estado"] = Model.Filter.State?.ToString() ?? "Todos",
                    ["Referencia"] = string.IsNullOrWhiteSpace(Model.Filter.Reference) ? "Todas" : Model.Filter.Reference,
                    ["Banco"] = Model.Filter.BankId?.ToString() ?? "Todos",
                    ["Tipo transacción"] = Model.Filter.TransactionType?.ToString() ?? "Todos"
                });

            ReportSectionComposer.ComposeDataTable(column.Item(),
                headers:
                [
                    "ID",
                    "Fecha",
                    "Referencia",
                    "Tipo",
                    "Monto",
                    "Estado",
                    "Cámara/Ciclo",
                    "Lote",
                    "Bancos",
                    "Archivo NACHA"
                ],
                defineColumns: columns =>
                {
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.8f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.6f);
                },
                rows: Model.Rows,
                composeRow: (table, row) =>
                {
                    table.Cell().PaddingVertical(3).Text(row.TransactionId.ToString());
                    table.Cell().PaddingVertical(3).Text(row.EffectiveEntryDate.ToString("yyyy-MM-dd"));
                    table.Cell().PaddingVertical(3).Text(row.Reference);
                    table.Cell().PaddingVertical(3).Text(row.TransactionType.ToString());
                    table.Cell().PaddingVertical(3).AlignRight().Text($"{row.Amount:N2}");
                    table.Cell().PaddingVertical(3).Text(row.State.ToString());
                    table.Cell().PaddingVertical(3).Text($"{row.ClearingHouseName} / {row.AchCycleName}");
                    table.Cell().PaddingVertical(3).Text(row.BatchSequenceNumber.ToString());
                    table.Cell().PaddingVertical(3).Text($"{row.SourceBankName} → {row.DestinationBankName}");
                    table.Cell().PaddingVertical(3).Text(row.NachaFileName);
                },
                emptyMessage: "No hay datos para los filtros indicados.");
        });
    }
}

