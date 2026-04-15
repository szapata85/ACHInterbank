using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CenitCycleQueue : AuditableEntity
{
    public long Id { get; set; }
    public int AchTransactionId { get; set; }
    public AchTransaction AchTransaction { get; set; } = null!;

    public string TargetAchCycleId { get; set; } = string.Empty;
    public AchCycle TargetAchCycle { get; set; } = null!;

    public string? OriginalAchCycleId { get; set; }
    public string QueueReason { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DequeuedAtUtc { get; set; }

    public long? CenitCycleExecutionId { get; set; }
    public CenitCycleExecution? CenitCycleExecution { get; set; }
}
