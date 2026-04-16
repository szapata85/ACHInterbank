using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CenitNettingDetail : AuditableEntity
{
    public long Id { get; set; }
    public long CenitNettingExecutionId { get; set; }
    public CenitNettingExecution CenitNettingExecution { get; set; } = null!;

    public int AchTransactionId { get; set; }
    public AchTransaction AchTransaction { get; set; } = null!;

    public int SourceInstitutionId { get; set; }
    public int DestinationInstitutionId { get; set; }
    public int AchBatchId { get; set; }
    public DateTime ValueDate { get; set; }
    public int ClearingHouseId { get; set; }
    public string ClearingHouseCode { get; set; } = string.Empty;
    public string SourceFileReference { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public bool IncludedInSettlement { get; set; }
    public string DecisionReason { get; set; } = string.Empty;
}
