using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ReturnOfReturnFlow : AuditableEntity
{
    public long Id { get; set; }

    public int SourceReturnTransactionId { get; set; }
    public AchTransaction SourceReturnTransaction { get; set; } = null!;

    public int ReturnOfReturnTransactionId { get; set; }
    public AchTransaction ReturnOfReturnTransaction { get; set; } = null!;

    public string ReasonCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Registered";
    public DateTime OrchestratedAtUtc { get; set; } = DateTime.UtcNow;

    public long? CenitCycleExecutionId { get; set; }
    public CenitCycleExecution? CenitCycleExecution { get; set; }
}
