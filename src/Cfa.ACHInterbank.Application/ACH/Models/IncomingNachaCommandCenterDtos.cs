using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record IncomingNachaPageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems);

public sealed class IncomingNachaIngestionQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public IncomingNachaIngestionStatus? IngestionStatus { get; set; }
    public IncomingNachaParsingStatus? ParsingStatus { get; set; }
    public string? CorrelationId { get; set; }
    public string? FileName { get; set; }
}

public sealed record IncomingNachaIngestionListItemDto(
    Guid Id,
    string FileName,
    string CorrelationId,
    IncomingNachaIngestionStatus IngestionStatus,
    IncomingNachaCycleResolutionStatus CycleResolutionStatus,
    IncomingNachaParsingStatus ParsingStatus,
    int? ResolvedClearingHouseId,
    string? ResolvedAchCycleId,
    DateTime? OperationalDate,
    bool IsReprocess,
    DateTime UploadedAtUtc,
    int QueueItems,
    int ProcessingEvents);

public sealed record IncomingNachaIngestionDetailDto(
    Guid Id,
    string FileName,
    string CorrelationId,
    IncomingNachaIngestionStatus IngestionStatus,
    IncomingNachaCycleResolutionStatus CycleResolutionStatus,
    IncomingNachaParsingStatus ParsingStatus,
    int? DetectedClearingHouseId,
    int? ResolvedClearingHouseId,
    string? ResolvedAchCycleId,
    DateTime? OperationalDate,
    string Notes,
    bool IsReprocess,
    Guid? ParentIngestionId,
    IReadOnlyList<IncomingNachaQueueListItemDto> Queue,
    IReadOnlyList<IncomingNachaProcessingEventDto> Events);

public sealed class IncomingNachaQueueQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public Guid? IngestionId { get; set; }
    public IncomingNachaDispatchQueueStatus? QueueStatus { get; set; }
    public int? ClearingHouseId { get; set; }
    public string? AchCycleId { get; set; }
}

public sealed record IncomingNachaQueueListItemDto(
    Guid Id,
    Guid IngestionId,
    int AchTransactionId,
    string AchCycleId,
    int ClearingHouseId,
    IncomingNachaDispatchQueueStatus QueueStatus,
    int Priority,
    int AttemptCount,
    DateTime? NextAttemptAtUtc,
    DateTime? LastAttemptAtUtc,
    string LastErrorCode,
    string LastErrorMessage,
    string LastResponseCode,
    DateTime? ConfirmedAtUtc,
    DateTimeOffset CreatedAtUtc,
    IncomingNachaAllowedActionsDto AllowedActions);

public sealed record IncomingNachaQueueDetailDto(
    IncomingNachaQueueListItemDto Queue,
    IncomingNachaIngestionListItemDto Ingestion,
    IncomingNachaEntryClassificationDto Classification,
    IReadOnlyList<IncomingNachaIntegrationExecutionDto> Executions,
    IReadOnlyList<IncomingNachaProcessingEventDto> Events);

public sealed record IncomingNachaEntryClassificationDto(
    Guid Id,
    int EntryDetailId,
    int? AddendaRecordId,
    IncomingNachaFunctionalClass FunctionalClass,
    IncomingNachaEligibilityStatus EligibilityStatus,
    bool RequiresManualResolution,
    string? ReturnReasonCode,
    IncomingNachaPrenoteStatus PrenoteStatus,
    string BusinessMeaning);

public sealed record IncomingNachaIntegrationExecutionDto(
    Guid Id,
    Guid DispatchQueueId,
    int? EntryDetailId,
    int AttemptNumber,
    string MethodName,
    string CorrelationId,
    IncomingNachaIndividualProcessingStatus ProcessingStatus,
    string ProcessingStatusText,
    IncomingNachaBusinessOutcome BusinessOutcome,
    string BusinessOutcomeText,
    int? AchReturnCodeId,
    string ResultCode,
    string ResultDescription,
    bool IsSuccess,
    bool IsRetryable,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc);

public sealed class IncomingNachaBatchQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "batchNumber";
    public bool SortDescending { get; set; }
}

public sealed record IncomingNachaBatchDto(
    int Id,
    int BatchNumber,
    string CompanyName,
    string ServiceClassCode,
    string StandardEntryClassCode,
    string? EffectiveEntryDate,
    int TotalTransactions,
    decimal TotalAmount,
    decimal TotalDebit,
    decimal TotalCredit);

public sealed class IncomingNachaTransactionQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int? BatchId { get; set; }
    public string? Search { get; set; }
    public string? ResultCode { get; set; }
    public IncomingNachaBusinessOutcome? BusinessOutcome { get; set; }
    public IncomingNachaIndividualProcessingStatus? ProcessingStatus { get; set; }
    public string SortBy { get; set; } = "traceNumber";
    public bool SortDescending { get; set; }
}

public sealed record IncomingNachaTransactionDto(
    int Id,
    int BatchId,
    int BatchNumber,
    string TraceNumber,
    string TransactionCode,
    decimal Amount,
    int AddendaCount,
    string ClassificationCode,
    string ClassificationText,
    string DispatchStatusCode,
    string DispatchStatusText,
    int AttemptCount,
    IncomingNachaIndividualProcessingStatus? ProcessingStatus,
    string ProcessingStatusText,
    IncomingNachaBusinessOutcome? BusinessOutcome,
    string BusinessOutcomeText,
    string ResultCode,
    string ResultDescription,
    DateTime? ProcessedAtUtc,
    string CorrelationId);

public sealed record IncomingNachaAddendaDto(
    int Id,
    string TypeCode,
    string Sequence,
    string ReturnReasonCode,
    string OriginalTraceNumber,
    string PaymentInformation);

public sealed record IncomingNachaProcessingEventDto(
    Guid Id,
    string EventType,
    string EventStatus,
    string Message,
    DateTime OccurredAtUtc,
    string RaisedBy,
    int? AchTransactionId);

public sealed class IncomingNachaManualActionRequest
{
    public string Justification { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int? Priority { get; set; }
}

public sealed record IncomingNachaManualActionResultDto(
    Guid QueueId,
    string Action,
    IncomingNachaDispatchQueueStatus PreviousStatus,
    IncomingNachaDispatchQueueStatus CurrentStatus,
    bool IsIdempotentReplay,
    string Message);

public sealed record IncomingNachaObservabilitySummaryDto(
    DateTime GeneratedAtUtc,
    int WindowHours,
    IncomingNachaPipelineHealthDto PipelineHealth,
    IReadOnlyList<IncomingNachaKpiCountDto> IngestionsByStatus,
    IReadOnlyList<IncomingNachaKpiCountDto> QueueByStatus,
    IReadOnlyList<IncomingNachaClearingCycleKpiDto> ByClearingHouseCycle,
    IReadOnlyList<IncomingNachaTopErrorDto> TopErrors,
    IReadOnlyList<IncomingNachaTimelinePointDto> Timeline);

public sealed record IncomingNachaPipelineHealthDto(
    int TotalIngestions,
    int TotalQueueItems,
    int BacklogItems,
    int BlockedItems,
    int RetryPendingItems,
    int WaitingWindowItems,
    int FailedFinalItems,
    int ConfirmedItems,
    double AverageQueueAgeMinutes,
    double OldestQueueAgeMinutes);

public sealed record IncomingNachaKpiCountDto(string Key, int Count);

public sealed record IncomingNachaClearingCycleKpiDto(
    int ClearingHouseId,
    string AchCycleId,
    int TotalItems,
    int BlockedItems,
    int RetryPendingItems,
    int WaitingWindowItems,
    int FailedFinalItems,
    int ConfirmedItems);

public sealed record IncomingNachaTopErrorDto(
    string ErrorCode,
    int Count,
    DateTime? LastSeenAtUtc);

public sealed record IncomingNachaTimelinePointDto(
    DateTime BucketAtUtc,
    int TotalEvents,
    int ManualApplied,
    int ManualRejected,
    int RetryPendingTransitions,
    int FailedFinalTransitions);
