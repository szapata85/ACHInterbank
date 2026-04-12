using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Interfaces;

public interface IAchAuditHistoryReportService
{
    Task<AchAuditReportResponseDto> GetAuditAsync(AchAuditReportFilter filter, CancellationToken ct = default);
    Task<AchHistoryReportResponseDto> GetHistoryAsync(AchHistoryReportFilter filter, CancellationToken ct = default);
}
