using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CenitCycleExecution : AuditableEntity
{
    public long Id { get; set; }
    public string AchCycleId { get; set; } = string.Empty;
    public AchCycle AchCycle { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public string Summary { get; set; } = string.Empty;

    public CenitNettingExecution? NettingExecution { get; set; }
    public ICollection<CenitCycleQueue> QueueItems { get; set; } = new List<CenitCycleQueue>();
    public ICollection<LiquidityOptimizationDecision> OptimizationDecisions { get; set; } = new List<LiquidityOptimizationDecision>();
    public ICollection<ReturnOfReturnFlow> ReturnOfReturnFlows { get; set; } = new List<ReturnOfReturnFlow>();
}
