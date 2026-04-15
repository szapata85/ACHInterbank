using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchReturnPolicy : AuditableEntity
{
    public int Id { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string AllowedReturnCodesCsv { get; set; } = string.Empty;
    public int MaxDays { get; set; }
    public string RequiredOriginalTransactionState { get; set; } = string.Empty;
    public bool AllowsReturnOfReturn { get; set; }
    public bool RequiresAddenda { get; set; }
    public bool IsActive { get; set; } = true;
}
