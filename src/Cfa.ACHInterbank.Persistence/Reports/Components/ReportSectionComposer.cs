using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Components;

internal static class ReportSectionComposer
{
    public static void ComposeTitleAndMetadata(IContainer container, string title, IReadOnlyDictionary<string, string> metadata)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text(title);

            if (metadata.Count == 0)
            {
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                foreach (var item in metadata)
                {
                    table.Cell().PaddingRight(10).Text($"{item.Key}: {item.Value}");
                }
            });
        });
    }

    public static void ComposeFiltersBlock(IContainer container, IReadOnlyDictionary<string, string> filters)
    {
        container.Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Background(Colors.Grey.Lighten4)
            .Padding(8)
            .Column(column =>
            {
                column.Spacing(4);
                column.Item().Text("Filtros aplicados");

                if (filters.Count == 0)
                {
                    column.Item().Text("Sin filtros.");
                    return;
                }

                foreach (var item in filters)
                {
                    column.Item().Text($"{item.Key}: {item.Value}");
                }
            });
    }

    public static void ComposeDataTable<TRow>(
        IContainer container,
        IReadOnlyList<string> headers,
        Action<TableColumnsDefinitionDescriptor> defineColumns,
        IReadOnlyCollection<TRow> rows,
        Action<TableDescriptor, TRow> composeRow,
        string emptyMessage)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(defineColumns);

            table.Header(header =>
            {
                foreach (var headerText in headers)
                {
                    header.Cell()
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten1)
                        .PaddingVertical(4)
                        .Text(headerText);
                }
            });

            if (rows.Count == 0)
            {
                table.Cell().ColumnSpan((uint)headers.Count).Padding(6).Text(emptyMessage);
                return;
            }

            foreach (var row in rows)
            {
                composeRow(table, row);
            }
        });
    }
}
