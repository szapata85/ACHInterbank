namespace Cfa.ACHInterbank.Application.ACH.Responses.Operations;

public sealed record AchResponseMappingCommand(
    int ClearingHouseId,
    string ResponseType,
    string ExternalCode,
    string? ExternalCause,
    int InternalStatusId,
    int ExternalServiceStatusId,
    string InternalStatusName,
    string? NormalizedCause,
    string? NormalizedDescription,
    bool RequiresCause,
    bool AllowsNotification,
    int Priority,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    Guid? ExpectedVersion,
    string Reason);

public sealed record AchResponseMappingModel(
    int Id, int ClearingHouseId, string ClearingHouseCode, string ResponseType, string ExternalCode,
    string? ExternalCause, int InternalStatusId, int ExternalServiceStatusId, string InternalStatusName,
    string? NormalizedCause, string? NormalizedDescription, bool RequiresCause, bool AllowsNotification,
    int Priority, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsActive, Guid Version);

public sealed record AchResponseAuditModel(
    long Id, string EntityType, string EntityId, string Action, string? PreviousState, string? NewState,
    string Actor, string Reason, string CorrelationId, DateTime OccurredAtUtc, string? SanitizedMetadata);

public sealed record AchResponseOrphanModel(
    Guid Id, Guid AchResponseId, int ClearingHouseId, string ResponseType, string ExternalIdentifiers,
    string ExternalCode, DateTime ReceivedAtUtc, DateTime OperationalDate, string CorrelationId,
    string OrphanReason, string? CandidateReferences, string ResolutionStatus, string? ResolvedReference,
    DateTime? ResolvedAtUtc, Guid Version);

public sealed record AchResponseReprocessModel(
    long Id, Guid AchResponseId, int AttemptNumber, string Status, string RequestedBy, string Reason,
    string CorrelationId, DateTime RequestedAtUtc, DateTime? CompletedAtUtc, string? Result, Guid CommandId);

public sealed record AchResponseReconciliationCaseModel(
    Guid Id, int ClearingHouseId, Guid? AchResponseId, string ExceptionType, string Status, string Reference,
    string? Details, DateTime DetectedAtUtc, string? Resolution, string? ResolutionReason,
    string? ResolvedBy, DateTime? ResolvedAtUtc, string CorrelationId, Guid Version);

public sealed record ManualResolutionCommand(
    Guid ExpectedVersion, string Reason, string? FunctionalReference, bool Reject, string CorrelationId);

public sealed record ReprocessCommand(Guid CommandId, Guid ExpectedVersion, string Reason, string CorrelationId);

public sealed record ReconciliationResolutionCommand(
    Guid ExpectedVersion, string Resolution, string Reason, string CorrelationId);

public class AchResponseOperationException : Exception
{
    public AchResponseOperationException(string message) : base(message) { }
}

public sealed class AchResponseNotFoundException : AchResponseOperationException
{
    public AchResponseNotFoundException(string message) : base(message) { }
}

public sealed class AchResponseConflictException : AchResponseOperationException
{
    public AchResponseConflictException(string message, Guid? currentVersion = null) : base(message)
        => CurrentVersion = currentVersion;
    public Guid? CurrentVersion { get; }
}
