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
            column.Item().Text(title).SemiBold().FontSize(14);

            if (metadata.Count == 0)
            {
                return;
            }

            column.Item().Grid(grid =>
            {
                grid.Columns(2);

                foreach (var item in metadata)
                {
                    grid.Item().PaddingRight(10).Text(text =>
                    {
                        text.Span($"{item.Key}: ").SemiBold();
                        text.Span(item.Value);
                    }).FontSize(9).FontColor(Colors.Grey.Darken2);
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
                column.Item().Text("Filtros aplicados").SemiBold().FontSize(10);

                if (filters.Count == 0)
                {
                    column.Item().Text("Sin filtros.").FontSize(9).FontColor(Colors.Grey.Darken1);
                    return;
                }

                foreach (var item in filters)
                {
                    column.Item().Text(text =>
                    {
                        text.Span($"{item.Key}: ").SemiBold();
                        text.Span(item.Value);
                    }).FontSize(9);
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

            foreach (var header in headers)
            {
                table.Header().Cell()
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .PaddingVertical(4)
                    .Text(header)
                    .SemiBold()
                    .FontSize(9);
            }

            if (rows.Count == 0)
            {
                table.Cell().ColumnSpan(headers.Count).Padding(6).Text(emptyMessage).Italic().FontSize(9);
                return;
            }

            foreach (var row in rows)
            {
                composeRow(table, row);
            }
        });
    }
}
