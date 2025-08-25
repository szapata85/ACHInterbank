using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransaction : AuditableEntity
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public string Reference { get; set; } = null!;

    public string Type { get; set; } = null!; // e.g., "Credit", "Debit"

    public int SourceInstitutionId { get; set; }
    public FinancialInstitution? SourceInstitution { get; set; }

    public int DestinationInstitutionId { get; set; }
    public FinancialInstitution? DestinationInstitution { get; set; }

    public int AchCycleId { get; set; }
    public AchCycle? AchCycle { get; set; }
}
