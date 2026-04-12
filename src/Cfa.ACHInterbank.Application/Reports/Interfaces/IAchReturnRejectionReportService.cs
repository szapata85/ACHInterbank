using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Interfaces;

public interface IAchReturnRejectionReportService
{
    Task<AchReturnRejectionReportResponseDto> GetReturnsAsync(AchReturnRejectionReportFilter filter, CancellationToken ct = default);
    Task<AchReturnRejectionReportResponseDto> GetRejectionsAsync(AchReturnRejectionReportFilter filter, CancellationToken ct = default);
}
