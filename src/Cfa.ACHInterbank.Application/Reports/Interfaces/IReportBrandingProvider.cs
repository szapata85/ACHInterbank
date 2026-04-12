using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Interfaces;

public interface IReportBrandingProvider
{
    Task<PdfBrandingOptions> GetAsync(CancellationToken ct = default);
}
