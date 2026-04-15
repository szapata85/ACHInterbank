using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class LiquidityOptimizationDecision : AuditableEntity
{
    public long Id { get; set; }
    public long CenitCycleExecutionId { get; set; }
    public CenitCycleExecution CenitCycleExecution { get; set; } = null!;

    public int AchTransactionId { get; set; }
    public AchTransaction AchTransaction { get; set; } = null!;

    public string DecisionType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string DecisionReason { get; set; } = string.Empty;
    public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;

    public string FromCycleId { get; set; } = string.Empty;
    public string? ToCycleId { get; set; }
}
