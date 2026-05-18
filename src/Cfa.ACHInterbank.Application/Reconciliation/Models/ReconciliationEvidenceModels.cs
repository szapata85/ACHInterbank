namespace Cfa.ACHInterbank.Application.Reconciliation.Models;

public sealed class ReconciliationEvidenceRequest
{
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? ClearingHouseCode { get; init; }
    public string? CycleId { get; init; }
    public string? CycleName { get; init; }
    public string? FileId { get; init; }
    public string? FileName { get; init; }
    public string? FileHash { get; init; }
    public int? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? CauseCode { get; init; }
    public ReconciliationEvidenceType? EvidenceType { get; init; }
    public bool IncludeCudEvidence { get; init; } = true;
    public bool IncludeThirdPartyReports { get; init; } = true;
    public bool IncludeOrphans { get; init; } = true;
    public bool IncludeManualAuditOnly { get; init; } = true;
    public bool IncludeReturnOfReturn { get; init; } = true;
    public bool IncludeRejections { get; init; } = true;
    public bool IncludeNetting { get; init; } = true;
    public bool IncludeLiquidity { get; init; } = true;
    public string RequestedBy { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}

public sealed class ReconciliationEvidenceResult
{
    public Guid EvidenceSetId { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public string GeneratedBy { get; init; } = string.Empty;
    public ReconciliationEvidenceScope Scope { get; init; } = new();
    public IReadOnlyList<ReconciliationEvidenceItem> Items { get; init; } = [];
    public IReadOnlyList<ReconciliationEvidenceAttachment> Attachments { get; init; } = [];
    public IReadOnlyList<ReconciliationEvidenceDifferenceLink> DifferenceLinks { get; init; } = [];
    public IReadOnlyList<ReconciliationEvidenceReview> Reviews { get; init; } = [];
    public ReconciliationEvidenceBoundaryFlags BoundaryFlags { get; init; } = ReconciliationEvidenceBoundaryFlags.Default;
    public ReconciliationEvidenceIdempotencyKey IdempotencyKey { get; init; } = new();
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class ReconciliationEvidenceScope
{
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? ClearingHouseCode { get; init; }
    public string? CycleId { get; init; }
    public string? CycleName { get; init; }
    public string? FileId { get; init; }
    public string? FileName { get; init; }
    public string? FileHash { get; init; }
    public int? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? CauseCode { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}

public sealed class ReconciliationEvidenceItem
{
    public string EvidenceItemId { get; init; } = string.Empty;
    public ReconciliationEvidenceType EvidenceType { get; init; }
    public ReconciliationEvidenceSource Source { get; init; }
    public int? TransactionId { get; init; }
    public string? ExternalReference { get; init; }
    public string? ThirdPartyReference { get; init; }
    public string? FileName { get; init; }
    public string? FileHash { get; init; }
    public string? ClearingHouseCode { get; init; }
    public string? CycleName { get; init; }
    public DateTime? OperationalDate { get; init; }
    public decimal Amount { get; init; }
    public string? Status { get; init; }
    public string? CauseCode { get; init; }
    public string? Description { get; init; }
    public bool IsExternalEvidence { get; init; }
    public bool IsManualAuditOnly { get; init; }
    public bool IsOrphan { get; init; }
    public bool IsReturnOfReturn { get; init; }
    public bool IsRejected { get; init; }
    public bool IsCudEvidence { get; init; }
    public bool IsNettingEvidence { get; init; }
    public bool IsLiquidityEvidence { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

public sealed class ReconciliationEvidenceAttachment
{
    public string AttachmentId { get; init; } = string.Empty;
    public string EvidenceItemId { get; init; } = string.Empty;
    public ReconciliationEvidenceAttachmentType AttachmentType { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string? FileHash { get; init; }
    public string? ContentType { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsExternalAttachment { get; init; }
    public bool ContainsSensitiveData { get; init; }
}

public sealed class ReconciliationEvidenceDifferenceLink
{
    public string DifferenceLinkId { get; init; } = string.Empty;
    public string EvidenceItemId { get; init; } = string.Empty;
    public ReconciliationEvidenceDifferenceType DifferenceType { get; init; }
    public ReconciliationEvidenceSeverity Severity { get; init; }
    public string? ExpectedValue { get; init; }
    public string? ActualValue { get; init; }
    public decimal DifferenceAmount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? RecommendedAction { get; init; }
}

public sealed class ReconciliationEvidenceReview
{
    public string ReviewId { get; init; } = string.Empty;
    public string EvidenceItemId { get; init; } = string.Empty;
    public ReconciliationEvidenceReviewStatus ReviewStatus { get; init; }
    public string? ReviewedBy { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public string? Comment { get; init; }
    public ReconciliationEvidenceEscalationLevel EscalationLevel { get; init; } = ReconciliationEvidenceEscalationLevel.None;
    public string? ApprovalReference { get; init; }
}

public sealed class ReconciliationEvidenceBoundaryFlags
{
    public bool IsAccountingPosting { get; init; }
    public bool IsOfficialLedger { get; init; }
    public bool IsJournalEntry { get; init; }
    public bool CreatesAccountingEntry { get; init; }
    public bool RequiresAccountingApi { get; init; }
    public bool IsOperationalEvidence { get; init; }
    public bool IsThirdPartyReviewEvidence { get; init; }
    public bool IsReconciliationSupport { get; init; }

    public static ReconciliationEvidenceBoundaryFlags Default => new()
    {
        IsAccountingPosting = false,
        IsOfficialLedger = false,
        IsJournalEntry = false,
        CreatesAccountingEntry = false,
        RequiresAccountingApi = false,
        IsOperationalEvidence = true,
        IsThirdPartyReviewEvidence = true,
        IsReconciliationSupport = true
    };
}

public sealed class ReconciliationEvidenceIdempotencyKey
{
    public string Key { get; init; } = string.Empty;
    public string ScopeHash { get; init; } = string.Empty;
    public string EvidenceType { get; init; } = string.Empty;
    public string SourceReference { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }
}

public enum ReconciliationEvidenceType
{
    OperationalReport = 1,
    PdfExport = 2,
    NachaFile = 3,
    Traceability = 4,
    StateEvent = 5,
    IncomingProcessingEvent = 6,
    ThirdPartyReport = 7,
    CudOperationalEvidence = 8,
    ManualReview = 9,
    ReturnOfReturn = 10,
    Orphan = 11,
    Rejection = 12,
    Netting = 13,
    Liquidity = 14
}

public enum ReconciliationEvidenceSource { Internal = 1, ExternalThirdParty = 2, Manual = 3, GeneratedReport = 4 }

public enum ReconciliationEvidenceAttachmentType { Pdf = 1, Csv = 2, Excel = 3, Image = 4, Text = 5, Json = 6, ThirdPartyFile = 7, CudSupport = 8 }

public enum ReconciliationEvidenceDifferenceType
{
    Amount = 1,
    Count = 2,
    Status = 3,
    CauseCode = 4,
    Cycle = 5,
    File = 6,
    MissingInThirdParty = 7,
    MissingInAch = 8,
    CudEvidenceMissing = 9,
    OrphanPending = 10,
    ManualReviewPending = 11,
    RorMismatch = 12,
    NettingMismatch = 13,
    LiquidityMismatch = 14
}

public enum ReconciliationEvidenceSeverity { Info = 1, Warning = 2, Critical = 3 }

public enum ReconciliationEvidenceReviewStatus { Pending = 1, Reviewed = 2, Accepted = 3, Rejected = 4, Escalated = 5, RequiresMoreEvidence = 6 }

public enum ReconciliationEvidenceEscalationLevel { None = 0, Level1 = 1, Level2 = 2, Level3 = 3, Level4 = 4 }
