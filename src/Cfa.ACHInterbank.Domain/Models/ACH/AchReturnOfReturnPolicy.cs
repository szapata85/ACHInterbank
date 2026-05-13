using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchReturnOfReturnPolicy : AuditableEntity
{
    public int Id { get; set; }
    public string OriginalReturnCode { get; set; } = string.Empty;
    public string AllowedNewReturnCodesCsv { get; set; } = string.Empty;
    public int MaxDays { get; set; }
    public string RequiredOriginalState { get; set; } = string.Empty;
    public bool IsUniquePerTransaction { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
