using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class PaymentRailCapabilityRegistryEntry : AuditableEntity
{
    public long Id { get; set; }
    public string RailCode { get; set; } = string.Empty;
    public string CapabilityCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public string ChangeSource { get; set; } = "Manual";
    public string ChangedBy { get; set; } = "system";
    public string? ChangeTicket { get; set; }
    public string? Notes { get; set; }
}
