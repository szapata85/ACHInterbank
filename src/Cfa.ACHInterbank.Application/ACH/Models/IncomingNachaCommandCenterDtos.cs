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
    string MethodName,
    string CorrelationId,
    string ResponseCode,
    string ResponseMessage,
    bool IsSuccess,
    bool IsRetryable,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc);

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
