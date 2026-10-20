using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>Intento append-only de prenotificación asociado a un contexto de validación.</summary>
public class PrenotificationValidationAttempt : AuditableEntity
{
    public long Id { get; set; }
    public long ThirdPartyAccountValidationId { get; set; }
    public ThirdPartyAccountValidation ThirdPartyAccountValidation { get; set; } = null!;

    public int AttemptNumber { get; set; }
    public int? AchTransactionId { get; set; }
    public AchTransaction? AchTransaction { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;
    public PrenotificationValidationAttemptStatus Status { get; set; } = PrenotificationValidationAttemptStatus.Created;
    public DateTimeOffset SentAtUtc { get; set; }
    public DateTimeOffset? ResponseDeadlineAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public PrenotificationValidationResolution Resolution { get; set; }

    public string? ReturnCode { get; set; }
    public string? ReturnDetail { get; set; }
    public string? EvidenceReference { get; set; }
    public string? CorrelationReference { get; set; }
    public string? SourceCycleId { get; set; }
    public string? SourceFileReference { get; set; }
}
