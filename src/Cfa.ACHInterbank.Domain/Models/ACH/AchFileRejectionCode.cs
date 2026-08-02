using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchFileRejectionCode : AuditableEntity
{
    public int Id { get; set; }
    public int? ClearingHouseId { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Fatal";
    public string AppliesToStage { get; set; } = "Validation";
    public bool IsRetryable { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = new(2024, 1, 1);
    public DateTime? EffectiveTo { get; set; }
    public string RegulatorySource { get; set; } = string.Empty;
}
