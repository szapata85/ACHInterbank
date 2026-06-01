namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchReconciliationDashboardReadModel
{
    public required string ProductiveStatus { get; init; }
    public int TotalResponses { get; init; }
    public int TotalDifferentialResponses { get; init; }
    public int TotalReturns { get; init; }
    public int TotalRejections { get; init; }
    public int TotalPrenotifications { get; init; }
    public int TotalRor { get; init; }
    public int TotalReconciled { get; init; }
    public int TotalPending { get; init; }
    public int TotalInconsistent { get; init; }
    public int TotalManualReviewRequired { get; init; }
    public int TotalNonMonetary { get; init; }
    public int TotalMonetaryCandidates { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
    public required string DataSource { get; init; }
    public bool IsPartialData { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record AchReconciliationItemReadModel
{
    public required string ReconciliationId { get; init; }
    public required string CorrelationId { get; init; }
    public string? FileId { get; init; }
    public required string FileName { get; init; }
    public required string ClearingHouseCode { get; init; }
    public required string FlowType { get; init; }
    public required string ResponseType { get; init; }
    public string? ResponseCode { get; init; }
    public string? ResponseDescription { get; init; }
    public string? ReasonCode { get; init; }
    public string? ReasonDescription { get; init; }
    public required string TraceNumberMasked { get; init; }
    public required string OriginalTraceNumberMasked { get; init; }
    public int? EntryId { get; init; }
    public int? TransactionId { get; init; }
    public required string InternalStatus { get; init; }
    public required string ReconciliationStatus { get; init; }
    public bool RequiresManualReview { get; init; }
    public bool IsReturnFile { get; init; }
    public bool IsRor { get; init; }
    public bool IsPrenotification { get; init; }
    public bool IsNonMonetary { get; init; }
    public bool IsMonetaryCandidate { get; init; }
    public required string SoapOperationCandidate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public required string DataSource { get; init; }
    public bool IsPersisted { get; init; }
    public bool IsDerived { get; init; }
    public string? Warning { get; init; }
}

public sealed record AchReconciliationDetailReadModel
{
    public required AchReconciliationItemReadModel Item { get; init; }
    public AchReconciliationNachaHeaderSummary? NachaHeaderSummary { get; init; }
    public AchReconciliationBatchSummary? BatchSummary { get; init; }
    public AchReconciliationEntrySummary? EntrySummary { get; init; }
    public AchReconciliationAddendaSummary? AddendaSummary { get; init; }
    public AchReconciliationControlSummary? ControlSummary { get; init; }
    public AchReconciliationInternalTransactionSummary? InternalTransactionSummary { get; init; }
    public IReadOnlyList<AchReconciliationHistoryEvent> ResponseHistory { get; init; } = [];
    public IReadOnlyList<AchReconciliationHistoryEvent> AuditEvents { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public bool NoSensitiveData { get; init; } = true;
}

public sealed record AchReconciliationNachaHeaderSummary(string? HeaderId, string ClearingHouseCode, string FileName, string FlowType, string CorrelationId);
public sealed record AchReconciliationBatchSummary(int? BatchId, string? ServiceClassCode, string? CompanyEntryDescription, int? BatchNumber);
public sealed record AchReconciliationEntrySummary(int? EntryId, string? TransactionCode, string TraceNumberMasked, string AccountNumberMasked, decimal? Amount);
public sealed record AchReconciliationAddendaSummary(int? AddendaId, string? ReturnReasonCode, string OriginalTraceNumberMasked, string NewTraceNumberMasked);
public sealed record AchReconciliationControlSummary(int BatchControlCount, int FileControlCount, int EntryAddendaCount, decimal TotalDebitAmount, decimal TotalCreditAmount);
public sealed record AchReconciliationInternalTransactionSummary(int? TransactionId, string? ExternalIdMasked, string? ReferenceMasked, string? State, bool IsPrenotification);
public sealed record AchReconciliationHistoryEvent(string EventType, string Status, string Message, DateTimeOffset Timestamp, string DataSource);
