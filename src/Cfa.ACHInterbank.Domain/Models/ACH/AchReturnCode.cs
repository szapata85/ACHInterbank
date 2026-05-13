using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchReturnCode : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool AppliesToDebit { get; set; }
    public bool AppliesToCredit { get; set; }
    public bool AppliesToPrenotification { get; set; }
    public bool AppliesToReturn { get; set; }
    public bool RequiresAddenda { get; set; }
    public int? MaxDaysAllowed { get; set; }
    public bool IsActive { get; set; } = true;
    public string RegulatorySource { get; set; } = "CENIT";
}
