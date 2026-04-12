using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Interfaces;

public interface IAchNachaCycleReportService
{
    Task<AchNachaFileReportResponseDto> GetNachaFilesAsync(AchNachaFileReportFilter filter, CancellationToken ct = default);
    Task<AchCycleReportResponseDto> GetCyclesAsync(AchCycleReportFilter filter, CancellationToken ct = default);
}
