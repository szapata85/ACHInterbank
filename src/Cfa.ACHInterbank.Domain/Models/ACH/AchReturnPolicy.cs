using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchReturnPolicy : AuditableEntity
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public string TransactionType { get; set; } = string.Empty;
    public string Direction { get; set; } = AchReturnDirection.Any;
    public string FlowType { get; set; } = AchReturnFlowType.Return;
    public string AllowedReturnCodesCsv { get; set; } = string.Empty;
    public int MaxDays { get; set; }
    public int? MaxCycles { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string RequiredOriginalTransactionState { get; set; } = string.Empty;
    public bool AllowsReturnOfReturn { get; set; }
    public bool RequiresAddenda { get; set; }
    public bool IsActive { get; set; } = true;
}
