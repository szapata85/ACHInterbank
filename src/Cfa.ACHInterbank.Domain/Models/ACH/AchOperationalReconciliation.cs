namespace Cfa.ACHInterbank.Domain.Models.ACH;

public enum AchOperationalReconciliationStatus
{
    PendingExternalEvidence = 1,
    Balanced = 2,
    Differences = 3
}

public enum AchOperationalReconciliationDifferenceCategory
{
    ReceivedApplicationInvariant = 1,
    ExternalSentCount = 2,
    ExternalSentAmount = 3,
    ExternalReceivedCount = 4,
    ExternalReceivedAmount = 5,
    ExternalNetPosition = 6
}

public sealed class AchOperationalReconciliationSnapshot
{
    public Guid Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public DateOnly OperationalDate { get; set; }
    public string AchCycleId { get; set; } = string.Empty;
    public AchCycle AchCycle { get; set; } = null!;
    public int Revision { get; set; }
    public string SourceFingerprint { get; set; } = string.Empty;
    public AchOperationalReconciliationStatus Status { get; set; }

    public int SentCount { get; set; }
    public decimal SentAmount { get; set; }
    public int ReceivedCount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public int AppliedCount { get; set; }
    public decimal AppliedAmount { get; set; }
    public int ParticipantReturnCount { get; set; }
    public decimal ParticipantReturnAmount { get; set; }
    public int OperatorReturnCount { get; set; }
    public decimal OperatorReturnAmount { get; set; }
    public decimal? InternalExpectedNetPosition { get; set; }

    public string? ExternalEvidenceReference { get; set; }
    public int? ExternalSentCount { get; set; }
    public decimal? ExternalSentAmount { get; set; }
    public int? ExternalReceivedCount { get; set; }
    public decimal? ExternalReceivedAmount { get; set; }
    public decimal? ExternalNetPosition { get; set; }
    public DateTimeOffset? ExternalEvidenceRecordedAt { get; set; }

    public DateTimeOffset CalculatedAt { get; set; }
    public string CalculatedBy { get; set; } = "system";
    public Guid Version { get; set; }
    public ICollection<AchOperationalReconciliationDifference> Differences { get; set; } = new List<AchOperationalReconciliationDifference>();
}

public sealed class AchOperationalReconciliationDifference
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public AchOperationalReconciliationSnapshot Snapshot { get; set; } = null!;
    public AchOperationalReconciliationDifferenceCategory Category { get; set; }
    public decimal? InternalValue { get; set; }
    public decimal? ExternalValue { get; set; }
    public decimal? Delta { get; set; }
    public string EvidenceSource { get; set; } = string.Empty;
    public DateTimeOffset DetectedAt { get; set; }
}
