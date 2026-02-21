using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Base;

public abstract class BaseReportDocument<TModel> : IDocument
{
    protected BaseReportDocument(TModel model)
    {
        Model = model;
    }

    protected TModel Model { get; }

    protected virtual string CompanyName => "ACH Interbank";

    protected virtual string Title => "Reporte";

    protected virtual DateTime GeneratedAtUtc => DateTime.UtcNow;

    public virtual DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(20);
            page.Size(PageSizes.A4.Landscape());

            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Element(ComposeCorporateHeader);
            page.Content().PaddingVertical(10).Element(ComposeBody);
            page.Footer().Element(ComposeCorporateFooter);
        });
    }

    protected abstract void ComposeBody(IContainer container);

    protected virtual void ComposeCorporateHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().Text(CompanyName).SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
            column.Item().Text(Title).SemiBold().FontSize(16);
            column.Item().Text($"Generado UTC: {GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}").FontSize(9).FontColor(Colors.Grey.Darken2);
        });
    }

    protected virtual void ComposeCorporateFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"{CompanyName} · Confidencial").FontSize(8).FontColor(Colors.Grey.Darken1);
            row.ConstantItem(120).AlignRight().Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            }).FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }
}
