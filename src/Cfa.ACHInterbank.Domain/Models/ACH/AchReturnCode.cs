using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchReturnCode : AuditableEntity
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FlowType { get; set; } = AchReturnFlowType.Any;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool AppliesToDebit { get; set; }
    public bool AppliesToCredit { get; set; }
    public bool AppliesToPrenotification { get; set; }
    public bool AppliesToReturn { get; set; }
    public bool RequiresAddenda { get; set; }
    public int? MaxDaysAllowed { get; set; }
    public bool IsActive { get; set; } = true;
    public string RegulatorySource { get; set; } = "CENIT";
}
