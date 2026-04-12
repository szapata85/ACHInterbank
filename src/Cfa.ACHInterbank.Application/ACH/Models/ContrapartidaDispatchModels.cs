using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ContrapartidaDispatchBatchCreateRequest(
    string AchCycleId,
    int ClearingHouseId,
    int? AchBatchId,
    ContrapartidaDispatchBatchTriggerTypeEnum TriggerType,
    string RequestedBy,
    string? JobId = null,
    string? RequestPayloadXml = null);

public sealed record ContrapartidaDispatchAttemptCreateRequest(
    long DispatchItemId,
    Guid? DispatchBatchId,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    ContrapartidaDispatchAttemptResultEnum Result,
    string CorrelationId,
    string TriggeredBy,
    bool RetryEligible,
    string? ExternalResponseCode = null,
    string? ExternalResponseMessage = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? RequestPayloadXml = null,
    string? ResponsePayloadXml = null);
