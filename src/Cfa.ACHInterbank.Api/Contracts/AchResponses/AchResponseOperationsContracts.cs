namespace Cfa.ACHInterbank.Api.Contracts.AchResponses;

public sealed record AchResponseMappingWriteRequest(
    int ClearingHouseId, string ResponseType, string ExternalCode, string? ExternalCause,
    int InternalStatusId, int ExternalServiceStatusId, string InternalStatusName,
    string? NormalizedCause, string? NormalizedDescription, bool RequiresCause, bool AllowsNotification,
    int Priority, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsActive,
    Guid? ExpectedVersion, string Reason);

public sealed record VersionedReasonRequest(Guid ExpectedVersion, string Reason, string? CorrelationId = null);
public sealed record CreateOrphanRequest(string Reason, string? CandidateReferences, string? CorrelationId = null);
public sealed record ResolveOrphanRequest(Guid ExpectedVersion, string Reason, string? FunctionalReference,
    bool Reject, string? CorrelationId = null);
public sealed record ReprocessResponseRequest(Guid CommandId, Guid ExpectedVersion, string Reason, string? CorrelationId = null);
public sealed record ResolveReconciliationRequest(Guid ExpectedVersion, string Resolution, string Reason,
    string? CorrelationId = null);
