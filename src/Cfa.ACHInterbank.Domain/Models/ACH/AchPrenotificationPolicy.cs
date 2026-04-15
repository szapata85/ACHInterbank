using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchPrenotificationPolicy : AuditableEntity
{
    public int Id { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool RequiresAddenda { get; set; }
    public bool BlocksMonetaryTransactionIfMissing { get; set; }
    public bool IsActive { get; set; } = true;
}
