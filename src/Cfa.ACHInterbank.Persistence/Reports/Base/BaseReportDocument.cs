using Cfa.ACHInterbank.Application.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports.Base;

public abstract class BaseReportDocument<TModel> : IDocument
{
    private readonly PdfBrandingOptions _branding;

    protected BaseReportDocument(TModel model, PdfBrandingOptions? branding = null)
    {
        Model = model;
        _branding = branding ?? new PdfBrandingOptions();
    }

    protected TModel Model { get; }

    protected virtual string CompanyName => _branding.CompanyName;

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
            column.Spacing(4);

            if (TryReadLogoBytes(_branding.LogoDataUri, out var logoBytes))
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(120).Height(36).Image(logoBytes);
                    row.RelativeItem().Column(meta =>
                    {
                        meta.Item().Text(CompanyName).SemiBold();
                        meta.Item().Text(Title);
                        meta.Item().Text($"Generado UTC: {GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}");
                    });
                });
            }
            else
            {
                column.Item().Text(CompanyName).SemiBold();
                column.Item().Text(Title);
                column.Item().Text($"Generado UTC: {GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}");
            }

            column.Item().LineHorizontal(1).LineColor(ResolveColor(_branding.AccentColorHex));
        });
    }

    protected virtual void ComposeCorporateFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"{CompanyName} · {_branding.FooterText}");
            row.ConstantItem(120).AlignRight().Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        });
    }

    private static bool TryReadLogoBytes(string? logoDataUri, out byte[] bytes)
    {
        bytes = [];

        if (string.IsNullOrWhiteSpace(logoDataUri))
        {
            return false;
        }

        var marker = "base64,";
        var markerIndex = logoDataUri.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (markerIndex < 0)
        {
            return false;
        }

        var encoded = logoDataUri[(markerIndex + marker.Length)..].Trim();

        if (encoded.Length == 0)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(encoded);
            return bytes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveColor(string? hex)
    {
        return string.IsNullOrWhiteSpace(hex) ? Colors.Blue.Medium : hex.Trim();
    }
}
