namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class PdfBrandingOptions
{
    public string CompanyName { get; init; } = "ACH Interbank";
    public string? LogoDataUri { get; init; }
    public string? AccentColorHex { get; init; }
    public string FooterText { get; init; } = "Confidencial";
}
