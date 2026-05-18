namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AccountingReviewReportRequest
{
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? ClearingHouseCode { get; init; }
    public string? CycleId { get; init; }
    public string? CycleName { get; init; }
    public string? FileId { get; init; }
    public string? FileName { get; init; }
    public int? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? CauseCode { get; init; }
    public bool IncludeOutbound { get; init; } = true;
    public bool IncludeIncoming { get; init; } = true;
    public bool IncludeReturns { get; init; } = true;
    public bool IncludeReturnOfReturn { get; init; } = true;
    public bool IncludeOrphans { get; init; } = true;
    public bool IncludeManualAuditOnly { get; init; } = true;
    public bool IncludeNetting { get; init; } = true;
    public bool IncludeLiquidity { get; init; } = true;
    public bool IncludeCudEvidence { get; init; } = true;
    public string RequestedBy { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}

public sealed class AccountingReviewReportResult
{
    public Guid ReportId { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public string GeneratedBy { get; init; } = string.Empty;
    public AccountingReviewScope Scope { get; init; } = new();
    public AccountingReviewReportSummary Summary { get; init; } = new();
    public IReadOnlyList<AccountingReviewReportRow> Rows { get; init; } = [];
    public IReadOnlyList<AccountingReviewDifference> Differences { get; init; } = [];
    public IReadOnlyList<AccountingReviewEvidenceReference> Evidence { get; init; } = [];
    public AccountingReviewExportMetadata ExportMetadata { get; init; } = new();
    public AccountingReviewBoundaryFlags BoundaryFlags { get; init; } = AccountingReviewBoundaryFlags.Default;
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class AccountingReviewReportRow
{
    public AccountingReviewRowType RowType { get; init; }
    public int? TransactionId { get; init; }
    public string? ExternalReference { get; init; }
    public string? FileName { get; init; }
    public string? FileHash { get; init; }
    public string? ClearingHouseCode { get; init; }
    public string? CycleName { get; init; }
    public DateTime? OperationalDate { get; init; }
    public decimal Amount { get; init; }
    public string? Direction { get; init; }
    public string? Status { get; init; }
    public string? CauseCode { get; init; }
    public bool IsAppliedOperationally { get; init; }
    public bool IsManualAuditOnly { get; init; }
    public bool IsOrphan { get; init; }
    public bool IsReturnOfReturn { get; init; }
    public bool IsRejected { get; init; }
    public bool IsCudEvidence { get; init; }
    public string? ThirdPartyReference { get; init; }
    public string? EvidenceReferenceId { get; init; }
    public string? Observation { get; init; }
    public AccountingReviewReconciliationStatus ReconciliationStatus { get; init; } = AccountingReviewReconciliationStatus.Pending;
}

public sealed class AccountingReviewReportSummary
{
    public int TotalRows { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalOutboundAmount { get; init; }
    public decimal TotalIncomingAmount { get; init; }
    public decimal TotalReturnAmount { get; init; }
    public decimal TotalRejectedAmount { get; init; }
    public decimal TotalDifferenceAmount { get; init; }
    public int OrphanCount { get; init; }
    public int ManualAuditOnlyCount { get; init; }
    public int ReturnOfReturnCount { get; init; }
    public int CudEvidenceCount { get; init; }
    public int DifferenceCount { get; init; }
    public bool HasDifferences { get; init; }
    public bool HasPendingEvidence { get; init; }
    public bool HasManualReviewItems { get; init; }
}

public sealed class AccountingReviewDifference
{
    public AccountingReviewDifferenceType DifferenceType { get; init; }
    public AccountingReviewDifferenceSeverity Severity { get; init; } = AccountingReviewDifferenceSeverity.Info;
    public int? TransactionId { get; init; }
    public string? FileName { get; init; }
    public string? ClearingHouseCode { get; init; }
    public string? CycleName { get; init; }
    public string? ExpectedValue { get; init; }
    public string? ActualValue { get; init; }
    public decimal DifferenceAmount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? RecommendedAction { get; init; }
    public string? EvidenceReferenceId { get; init; }
}

public sealed class AccountingReviewEvidenceReference
{
    public AccountingReviewEvidenceType EvidenceType { get; init; }
    public string ReferenceId { get; init; } = string.Empty;
    public string? FileName { get; init; }
    public string? FileHash { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsExternalEvidence { get; init; }
}

public sealed class AccountingReviewScope
{
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? ClearingHouseCode { get; init; }
    public string? CycleId { get; init; }
    public string? CycleName { get; init; }
    public string? FileId { get; init; }
    public string? FileName { get; init; }
    public int? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? CauseCode { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}

public sealed class AccountingReviewExportMetadata
{
    public bool SupportsPdf { get; init; } = true;
    public bool SupportsExcel { get; init; }
    public bool SupportsCsv { get; init; }
    public string? ExportFileName { get; init; }
    public DateTimeOffset? ExportedAt { get; init; }
    public string? ExportedBy { get; init; }
}

public sealed class AccountingReviewBoundaryFlags
{
    public bool IsAccountingPosting { get; init; }
    public bool IsOfficialLedger { get; init; }
    public bool IsJournalEntry { get; init; }
    public bool IsOperationalReport { get; init; }
    public bool IsThirdPartyReview { get; init; }
    public bool IsReconciliationSupport { get; init; }
    public bool CreatesAccountingEntry { get; init; }
    public bool RequiresAccountingApi { get; init; }

    public static AccountingReviewBoundaryFlags Default => new()
    {
        IsAccountingPosting = false,
        IsOfficialLedger = false,
        IsJournalEntry = false,
        IsOperationalReport = true,
        IsThirdPartyReview = true,
        IsReconciliationSupport = true,
        CreatesAccountingEntry = false,
        RequiresAccountingApi = false
    };
}

public enum AccountingReviewRowType
{
    OutboundTransaction = 1,
    IncomingReturn = 2,
    OutboundReturn = 3,
    ReturnOfReturn = 4,
    Orphan = 5,
    ManualAuditOnly = 6,
    Rejection = 7,
    Netting = 8,
    Liquidity = 9,
    CudEvidence = 10
}

public enum AccountingReviewDifferenceType
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
    ManualReviewPending = 11
}

public enum AccountingReviewDifferenceSeverity { Info = 1, Warning = 2, Critical = 3 }

public enum AccountingReviewEvidenceType
{
    Report = 1,
    Pdf = 2,
    NachaFile = 3,
    Traceability = 4,
    IncomingProcessingEvent = 5,
    StateEvent = 6,
    CudOperationalEvidence = 7,
    ThirdPartyReport = 8,
    ManualReview = 9
}

public enum AccountingReviewReconciliationStatus
{
    Pending = 1,
    Matched = 2,
    Difference = 3,
    ManualReview = 4,
    EvidencePending = 5
}
