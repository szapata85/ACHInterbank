using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransaction : AuditableEntity
{
    public int Id { get; set; }

    public decimal Amount { get; set; }
    public string Reference { get; set; } = null!;
    public TransactionTypeEnum Type { get; set; }

    public string TransactionCode { get; set; } = string.Empty;
    public string ServiceClassCode { get; set; } = "200";
    public string CompanyEntryDescription { get; set; } = "PAGOS";
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentification { get; set; } = string.Empty;

    public string OriginatingDFI { get; set; } = string.Empty;
    public string ReceivingDFI { get; set; } = string.Empty;

    public string TraceNumber { get; set; } = string.Empty;
    public int TraceSequenceNumber { get; set; }

    public DateTime EffectiveEntryDate { get; set; }
    public bool AddendaRecordIndicator { get; set; }

    public string SourceAccountNumber { get; set; } = null!;
    public string DestinationAccountNumber { get; set; } = null!;

    public int SourceInstitutionId { get; set; }
    public FinancialInstitution SourceInstitution { get; set; } = null!;

    public int DestinationInstitutionId { get; set; }
    public FinancialInstitution DestinationInstitution { get; set; } = null!;

    public int AchCycleId { get; set; }
    public AchCycle AchCycle { get; set; } = null!;

    public int AchBatchId { get; set; }
    public AchBatch AchBatch { get; set; } = null!;

    public ICollection<AchTransactionAddenda> Addendas { get; set; } = new List<AchTransactionAddenda>();
}



