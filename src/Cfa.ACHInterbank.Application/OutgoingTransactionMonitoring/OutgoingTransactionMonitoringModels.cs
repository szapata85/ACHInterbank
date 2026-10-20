using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;

public sealed record OutgoingTransactionMonitoringQuery
{
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? CycleId { get; init; }
    public int? DestinationInstitutionId { get; init; }
    public string? TransactionExternalId { get; init; }
    public string? TraceNumber { get; init; }
    public string? ResponseCode { get; init; }
    public TransactionTypeEnum? TransactionType { get; init; }
    public string? ProcessStatus { get; init; }
    public string? InitialResult { get; init; }
    public string? SubsequentSituation { get; init; }
    public bool? HasReturn { get; init; }
    public bool? RequiresAttention { get; init; }
    public decimal? MinimumAmount { get; init; }
    public decimal? MaximumAmount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
}

public sealed record OutgoingMonitoringPagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    long TotalItems,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record OutgoingTransactionMonitoringListItem
{
    public int Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string TransactionExternalId { get; init; } = string.Empty;
    public string TraceNumber { get; init; } = string.Empty;
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string ClearingHouseDisplayName { get; init; } = string.Empty;
    public string CycleId { get; init; } = string.Empty;
    public string CycleDisplayName { get; init; } = string.Empty;
    public DateTime CycleProcessingDate { get; init; }
    public string NextExpectedStepDisplayName { get; init; } = string.Empty;
    public string DestinationInstitutionDisplayName { get; init; } = string.Empty;
    public string TransactionTypeCode { get; init; } = string.Empty;
    public string TransactionTypeDisplayName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string MaskedDestinationAccount { get; init; } = string.Empty;
    public string ProcessStatusCode { get; init; } = string.Empty;
    public string ProcessStatusDisplayName { get; init; } = string.Empty;
    public string InitialResultCode { get; init; } = string.Empty;
    public string InitialResultDisplayName { get; init; } = string.Empty;
    public string SubsequentSituationCode { get; init; } = string.Empty;
    public string SubsequentSituationDisplayName { get; init; } = string.Empty;
    public bool HasReturn { get; init; }
    public string? ReturnCode { get; init; }
    public string? ReturnDescription { get; init; }
    public string? FileName { get; init; }
    public int? FileVersion { get; init; }
    public string FileLifecycleStatusCode { get; init; } = "NotDetermined";
    public string FileLifecycleStatusDisplayName { get; init; } = "No determinado";
    public DateTimeOffset LastUpdatedAtUtc { get; init; }
    public bool RequiresAttention { get; init; }
    public string? AttentionReason { get; init; }
}

public sealed record OutgoingTransactionMonitoringDetail
{
    public required OutgoingTransactionMonitoringListItem Summary { get; init; }
    public required OutgoingTransactionClassificationDetail Classification { get; init; }
    public required OutgoingTransactionIntegrationDetail Integration { get; init; }
    public IReadOnlyList<OutgoingTransactionFileDetail> Files { get; init; } = [];
    public IReadOnlyList<OutgoingTransactionResponseDetail> Responses { get; init; } = [];
    public IReadOnlyList<OutgoingTransactionReturnDetail> Returns { get; init; } = [];
    public IReadOnlyList<OutgoingTransactionTimelineEvent> Timeline { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public OutgoingTransactionTechnicalDetail? TechnicalDetail { get; init; }
}

public sealed record OutgoingTransactionClassificationDetail(
    string DirectionDisplayName,
    string OriginDisplayName,
    string MonetaryRouteDisplayName,
    string ClassificationStatusDisplayName,
    DateTime? ClassifiedAtUtc,
    int ClassificationVersion);

public sealed record OutgoingTransactionIntegrationDetail(
    bool WasDispatched,
    int AttemptCount,
    string ResultDisplayName,
    string? ResponseCode,
    string? ResponseDescription,
    DateTime? LastAttemptAtUtc,
    DateTime? LastSuccessAtUtc);

public sealed record OutgoingTransactionFileDetail(
    int FileId,
    string FileName,
    string OperationDisplayName,
    int? Version,
    int FileSequence,
    DateTime IncludedAtUtc,
    DateTime GeneratedAtUtc,
    string ArtifactTypeDisplayName,
    string? ContentSha256,
    string LifecycleStatusCode,
    string LifecycleStatusDisplayName,
    bool HasTransmissionEvidence,
    string? TransmissionReference,
    DateTime? TransmittedAtUtc,
    bool HasAcknowledgementEvidence,
    DateTime? AcknowledgedAtUtc,
    string? AcknowledgementCode,
    IReadOnlyList<OutgoingTransactionTransportAttemptDetail> TransportAttempts,
    IReadOnlyList<OutgoingTransactionTransportResultDetail> TransportResults);

public sealed record OutgoingTransactionTransportAttemptDetail(
    int AttemptNumber,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string StatusCode,
    string StatusDisplayName,
    bool Retryable,
    string ResultCode,
    string ResultDescription,
    string? TransmissionReference);

public sealed record OutgoingTransactionTransportResultDetail(
    Guid Id,
    DateTime OccurredAtUtc,
    DateTime ReceivedAtUtc,
    DateTime? ProcessedAtUtc,
    string OutcomeCode,
    string OutcomeDisplayName,
    string ResultCode,
    string ResultDescription,
    string CorrelationStatusDisplayName,
    bool Applied,
    bool RequiresManualReview);

public sealed record OutgoingTransactionResponseDetail(
    Guid Id,
    DateTime ReceivedAtUtc,
    string ResponseTypeDisplayName,
    string ExternalStatusCode,
    string? CauseCode,
    string? CauseDescription,
    string CorrelationStatusDisplayName);

public sealed record OutgoingTransactionReturnDetail(
    DateTime OccurredAtUtc,
    string StateDisplayName,
    string? CauseCode,
    string? CauseDescription);

public sealed record OutgoingTransactionTimelineEvent(
    DateTime OccurredAtUtc,
    string StageCode,
    string StageDisplayName,
    string Title,
    string Description,
    string OutcomeCode,
    string OutcomeDisplayName,
    string Severity,
    string SourceType,
    bool IsTechnical);

public sealed record OutgoingTransactionTechnicalDetail(
    int TransactionId,
    string? LastIntegrationMethod,
    string? LastIntegrationMode,
    string? LastIntegrationCode,
    long? LastIntegrationDurationMs,
    string? LastCorrelationId);

public sealed record OutgoingTransactionMonitoringAudit(
    string UserId,
    string Operation,
    string EntityId,
    string CorrelationId,
    bool Authorized,
    string SanitizedCriteria);

public sealed class OutgoingTransactionMonitoringException : Exception
{
    public OutgoingTransactionMonitoringException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
