using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransaction : AuditableEntity
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public string Reference { get; set; } = null!;

    // 🔹 Cambiado a enum
    public TransactionTypeEnum Type { get; set; }

    public int SourceInstitutionId { get; set; }
    public FinancialInstitution? SourceInstitution { get; set; }

    public int DestinationInstitutionId { get; set; }
    public FinancialInstitution? DestinationInstitution { get; set; }

    public int AchCycleId { get; set; }
    public AchCycle? AchCycle { get; set; }

    public ICollection<AchTransactionAddenda>? Addendas { get; set; }
}

