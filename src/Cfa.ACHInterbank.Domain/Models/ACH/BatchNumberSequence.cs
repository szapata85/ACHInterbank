using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BatchNumberSequence : AuditableEntity
{
    public int Id { get; set; }
    public string ClearingHouseId { get; set; } = string.Empty;
    public string OriginatingDfi { get; set; } = string.Empty;
    public DateOnly ProcessingDate { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public int LastAssignedValue { get; set; }
}
