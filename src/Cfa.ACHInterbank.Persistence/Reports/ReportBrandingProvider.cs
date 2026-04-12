using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Reports;

[Scoped]
public sealed class ReportBrandingProvider : IReportBrandingProvider
{
    private readonly AchDbContext _context;

    public ReportBrandingProvider(AchDbContext context)
    {
        _context = context;
    }

    public async Task<PdfBrandingOptions> GetAsync(CancellationToken ct = default)
    {
        var branding = await _context.BrandingSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (branding is null)
        {
            return new PdfBrandingOptions();
        }

        return new PdfBrandingOptions
        {
            CompanyName = "ACH Interbank",
            LogoDataUri = !string.IsNullOrWhiteSpace(branding.PrivateLogo) ? branding.PrivateLogo : branding.PublicLogo,
            AccentColorHex = branding.ButtonColor,
            FooterText = "Confidencial"
        };
    }
}
