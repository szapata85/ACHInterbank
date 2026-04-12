using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Persistence.Reports.Models;

internal sealed class AchReconciliationReportDocumentModel
{
    public required AchReconciliationReportFilter Filter { get; init; }
    public required AchReconciliationTotalsDto Totals { get; init; }
    public required AchReconciliationDifferencesDto Differences { get; init; }
    public required IReadOnlyList<AchReconciliationInconsistencyDto> Inconsistencies { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}
