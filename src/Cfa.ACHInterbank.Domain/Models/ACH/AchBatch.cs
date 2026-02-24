using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchBatch : AuditableEntity
{
    public int Id { get; set; }

    public string? AchCycleId { get; set; }
    public AchCycle? AchCycle { get; set; }

    public string ServiceClassCode { get; set; } = "220";
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentification { get; set; } = string.Empty;
    public string CompanyEntryDescription { get; set; } = "PAGOS";
    public int CompanyEntryDescriptionId { get; set; }
    public string OriginOrOdfi { get; set; } = string.Empty;

    public DateTime EffectiveEntryDate { get; set; }

    public int BatchSequenceNumber { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalCreditAmount { get; set; }

    public ICollection<AchTransaction> Transactions { get; set; } = new List<AchTransaction>();
}
