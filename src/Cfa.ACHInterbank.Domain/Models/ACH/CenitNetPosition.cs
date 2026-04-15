using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CenitNetPosition : AuditableEntity
{
    public long Id { get; set; }
    public long CenitNettingExecutionId { get; set; }
    public CenitNettingExecution CenitNettingExecution { get; set; } = null!;

    public int FinancialInstitutionId { get; set; }
    public FinancialInstitution FinancialInstitution { get; set; } = null!;

    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal NetAmount { get; set; }

    public decimal AvailableLiquidity { get; set; }
    public bool HasInsufficientFunds { get; set; }
}
