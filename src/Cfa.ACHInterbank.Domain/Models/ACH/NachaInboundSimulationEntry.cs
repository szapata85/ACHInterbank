using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaInboundSimulationEntry : AuditableEntity
{
    public int Id { get; set; }
    public int NachaInboundSimulationId { get; set; }
    public NachaInboundSimulation Simulation { get; set; } = null!;
    public string Reference { get; set; } = string.Empty;
    public int? TransactionId { get; set; }
    public string? PrenotificationReference { get; set; }
    public string AccountNumberMasked { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Nature { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string ExpectedStatusAfterUpload { get; set; } = string.Empty;
    public string? ReasonCode { get; set; }
    public bool IsSynthetic { get; set; } = true;
}
