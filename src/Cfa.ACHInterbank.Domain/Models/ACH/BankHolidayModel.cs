using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BankHolidayModel : AuditableEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly? CommemorativeDate { get; set; }
    public string Description { get; set; } = default!;
    public string CountryCode { get; set; } = "CO";
    public string? RuleCode { get; set; }
    public BankHolidayRuleKind? RuleKind { get; set; }
    public bool IsSystemGenerated { get; set; }
    public string? LegalOrigin { get; set; }
    public int? EffectiveFromYear { get; set; }
}

public enum BankHolidayRuleKind
{
    Fixed = 1,
    Emiliani = 2,
    Easter = 3,
    EasterEmiliani = 4,
    ChiquinquiraEmiliani = 5
}
