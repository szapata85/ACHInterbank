using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Persistence.Reports.Models;

internal sealed class AchCycleReportDocumentModel
{
    public required AchCycleReportFilter Filter { get; init; }
    public required IReadOnlyList<AchCycleReportRowDto> Rows { get; init; }
    public required AchCycleReportTotalsDto Totals { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}
