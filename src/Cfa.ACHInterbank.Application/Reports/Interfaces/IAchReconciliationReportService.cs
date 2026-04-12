using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Interfaces;

public interface IAchReconciliationReportService
{
    Task<AchReconciliationReportResponseDto> GetReconciliationAsync(AchReconciliationReportFilter filter, CancellationToken ct = default);
}
