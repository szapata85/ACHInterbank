using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CenitNettingExecution : AuditableEntity
{
    public long Id { get; set; }
    public long CenitCycleExecutionId { get; set; }
    public CenitCycleExecution CenitCycleExecution { get; set; } = null!;

    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }

    public ICollection<CenitNetPosition> NetPositions { get; set; } = new List<CenitNetPosition>();
    public ICollection<CenitNettingDetail> Details { get; set; } = new List<CenitNettingDetail>();
}
