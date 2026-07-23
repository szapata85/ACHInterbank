using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public sealed class AchResponseAudit
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid? AchResponseId { get; set; }
    public AchResponse? AchResponse { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousState { get; set; }
    public string? NewState { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? SanitizedMetadata { get; set; }
}

public sealed class AchResponseOrphan
{
    public Guid Id { get; set; }
    public Guid AchResponseId { get; set; }
    public AchResponse AchResponse { get; set; } = null!;
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public string ResponseType { get; set; } = string.Empty;
    public string ExternalIdentifiers { get; set; } = string.Empty;
    public string ExternalCode { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime OperationalDate { get; set; }
    public string CanonicalPayloadHash { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string OrphanReason { get; set; } = string.Empty;
    public string? CandidateReferences { get; set; }
    public string ResolutionStatus { get; set; } = "Pending";
    public string? ResolvedReference { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionReason { get; set; }
    public Guid Version { get; set; }
}

public sealed class AchResponseReprocessAttempt
{
    public long Id { get; set; }
    public Guid AchResponseId { get; set; }
    public AchResponse AchResponse { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Result { get; set; }
    public Guid CommandId { get; set; }
}

public sealed class AchResponseReconciliationCase
{
    public Guid Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public Guid? AchResponseId { get; set; }
    public AchResponse? AchResponse { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Reference { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime DetectedAtUtc { get; set; }
    public string? Resolution { get; set; }
    public string? ResolutionReason { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid Version { get; set; }
}
