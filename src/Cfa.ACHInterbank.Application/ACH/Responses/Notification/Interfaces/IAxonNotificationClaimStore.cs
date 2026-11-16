using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;

/// <summary>Durable SQL fencing for SOAP Axon; no remote call within DB transactions.</summary>
public interface IAxonNotificationClaimStore
{
    Task<bool> TryFinishPreflightAsync(long attemptId, Guid responseId,
        AxonNotificationCompletion completion, DateTime nowUtc, CancellationToken ct);
    Task<bool> TryClaimAsync(long attemptId, string token, DateTime nowUtc, CancellationToken ct);
    Task<bool> MarkTransportIntentAsync(long attemptId, string token, DateTime nowUtc, CancellationToken ct);
    Task<bool> TryFinalizeAsync(long attemptId, Guid responseId, string token,
        AxonNotificationCompletion completion, DateTime nowUtc, CancellationToken ct);
    Task RecordLateEvidenceAsync(long attemptId, Guid responseId, string token,
        string evidenceCode, string? responsePayload, CancellationToken ct);
    Task<int> QuarantineExpiredAsync(DateTime nowUtc, CancellationToken ct);
}

public sealed record AxonNotificationCompletion(
    AchResponseNotificationStatus AttemptStatus,
    AchResponseProcessingStatus ResponseStatus,
    bool? HasError, string? ErrorCode, string? ErrorDescription, string? TechnicalError,
    string? RequestPayload, string? ResponsePayload);
