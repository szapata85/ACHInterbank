using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchOperationalReconciliationExternalEvidence
{
    public string? EvidenceReference { get; init; }
    public int? SentCount { get; init; }
    public decimal? SentAmount { get; init; }
    public int? ReceivedCount { get; init; }
    public decimal? ReceivedAmount { get; init; }
    public decimal? NetPosition { get; init; }
    public DateTimeOffset? RecordedAt { get; init; }

    public bool IsComplete => !string.IsNullOrWhiteSpace(EvidenceReference)
        && SentCount.HasValue
        && SentAmount.HasValue
        && ReceivedCount.HasValue
        && ReceivedAmount.HasValue
        && NetPosition.HasValue;
}

public sealed record AchOperationalReconciliationRequest(
    int ClearingHouseId,
    DateOnly OperationalDate,
    string AchCycleId,
    AchOperationalReconciliationExternalEvidence? ExternalEvidence = null,
    string CalculatedBy = "system");

public sealed record AchOperationalReconciliationResult(
    AchOperationalReconciliationSnapshot Snapshot,
    bool ReusedExistingRevision);
