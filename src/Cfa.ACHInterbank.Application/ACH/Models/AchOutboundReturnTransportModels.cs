using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchOutboundReturnArtifact(
    string FileName,
    byte[] Content,
    int RecordCount,
    int ReturnCount,
    string CycleId,
    int ClearingHouseId,
    IReadOnlyList<int> TransactionIds,
    string ContentSha256);

public sealed record AchOutboundReturnTransportRequest(
    int AchFileExportId,
    int ClearingHouseId,
    string FileName,
    byte[] Content,
    string ContentSha256,
    string IdempotencyKey);

public sealed record AchOutboundReturnTransportResult(
    bool Succeeded,
    bool Retryable,
    string ResultCode,
    string ResultSummary,
    string? ExternalReference,
    DateTime OccurredAtUtc);

public sealed record AchOutboundReturnDispatchRequest(
    string FileName,
    string IdempotencyKey,
    string Actor);

public sealed record AchOutboundReturnGenerateDispatchRequest(
    GenerateReturnsFileRequest Generation,
    string IdempotencyKey,
    string Actor);

public sealed record AchOutboundReturnDispatchResult(
    int AchFileExportId,
    string FileName,
    AchFileExportLifecycleStatus LifecycleStatus,
    bool Succeeded,
    bool Retryable,
    bool WasDuplicate,
    string ResultCode,
    string ResultSummary,
    string? ExternalReference,
    int AttemptNumber);

public sealed record AchOutboundReturnResultRequest(
    string ExternalEventId,
    string FileName,
    string TransmissionReference,
    AchOutboundReturnOutcome Outcome,
    string ResultCode,
    DateTime OccurredAtUtc,
    string? ResultSummary = null);

public sealed record AchOutboundReturnResultProcessingResult(
    Guid ResultId,
    bool WasDuplicate,
    AchResponseCorrelationStatus CorrelationStatus,
    int? AchFileExportId,
    AchFileExportLifecycleStatus? LifecycleStatus,
    bool Applied,
    bool RequiresManualReview,
    string ResultCode);
