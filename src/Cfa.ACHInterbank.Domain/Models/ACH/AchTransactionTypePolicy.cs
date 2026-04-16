using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransactionTypePolicy : AuditableEntity
{
    public int Id { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int PriorityOrder { get; set; }
    public bool IsMonetary { get; set; }
    public bool RequiresPrenotification { get; set; }
    public bool CanBeReturned { get; set; }
    public bool CanBeReturnedAgain { get; set; }
    public bool IsActive { get; set; } = true;
}
