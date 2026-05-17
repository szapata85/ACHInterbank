using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchReturnOfReturnPolicy : AuditableEntity
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public string OriginalReturnCode { get; set; } = string.Empty;
    public string Direction { get; set; } = AchReturnDirection.Any;
    public string FlowType { get; set; } = AchReturnFlowType.ReturnOfReturn;
    public string AllowedNewReturnCodesCsv { get; set; } = string.Empty;
    public int MaxDays { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string RequiredOriginalState { get; set; } = string.Empty;
    public bool IsUniquePerTransaction { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
