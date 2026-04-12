namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AchReconciliationReportFilter
{
    public DateTime? Date { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? AchCycleId { get; init; }
}

public sealed class AchReconciliationTotalsDto
{
    public int SentCount { get; init; }
    public decimal SentAmount { get; init; }
    public int ReceivedCount { get; init; }
    public decimal ReceivedAmount { get; init; }
    public int ReturnedCount { get; init; }
    public decimal ReturnedAmount { get; init; }
}

public sealed class AchReconciliationDifferencesDto
{
    public int SentVsReceivedCountDiff { get; init; }
    public decimal SentVsReceivedAmountDiff { get; init; }
    public int SentVsReturnedCountDiff { get; init; }
    public decimal SentVsReturnedAmountDiff { get; init; }
    public int ReceivedVsReturnedCountDiff { get; init; }
    public decimal ReceivedVsReturnedAmountDiff { get; init; }
}

public sealed class AchReconciliationInconsistencyDto
{
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int AffectedCount { get; init; }
}

public sealed class AchReconciliationReportResponseDto
{
    public AchReconciliationTotalsDto Totals { get; init; } = new();
    public AchReconciliationDifferencesDto Differences { get; init; } = new();
    public IReadOnlyList<AchReconciliationInconsistencyDto> Inconsistencies { get; init; } = [];
}
