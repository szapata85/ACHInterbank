using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransaction : AuditableEntity
{
    public int Id { get; set; }

    public decimal Amount { get; set; }
    /// <summary>
    /// Identificador operativo/idempotencia de la instrucción del cliente.
    /// Nuevo campo canónico para correlación técnica.
    /// </summary>
    public string TransactionExternalId { get; set; } = string.Empty;
    /// <summary>
    /// LEGACY: referencia histórica transicional.
    /// No usar como campo funcional principal de negocio.
    /// </summary>
    public string Reference { get; set; } = null!;
    public TransactionTypeEnum Type { get; set; }

    public string TransactionCode { get; set; } = string.Empty;
    public string ServiceClassCode { get; set; } = "200";
    public int CompanyEntryDescriptionId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentification { get; set; } = string.Empty;

    public string OriginatingDFI { get; set; } = string.Empty;
    public string ReceivingDFI { get; set; } = string.Empty;

    public string TraceNumber { get; set; } = string.Empty;
    public int TraceSequenceNumber { get; set; }

    public DateTime EffectiveEntryDate { get; set; }
    public bool AddendaRecordIndicator { get; set; }
    public bool IsPrenotification { get; set; }

    public AchTransferStateEnum State { get; set; } = AchTransferStateEnum.Pending;
    public DateTime StateChangedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SlaDeadlineAtUtc { get; set; }
    public string ContrapartidasResponseCode { get; set; } = string.Empty;
    public string ReturnReasonCode { get; set; } = string.Empty;
    public string OriginalTraceRef { get; set; } = string.Empty;
    public string RecipientIdNumber { get; set; } = string.Empty;
    public string DiscretionaryData { get; set; } = string.Empty;

    public string SourceAccountNumber { get; set; } = null!;
    public string DestinationAccountNumber { get; set; } = null!;

    public int SourceInstitutionId { get; set; }
    public FinancialInstitution SourceInstitution { get; set; } = null!;

    public int DestinationInstitutionId { get; set; }
    public FinancialInstitution DestinationInstitution { get; set; } = null!;

    public string AchCycleId { get; set; } = null!;
    public AchCycle AchCycle { get; set; } = null!;

    public int AchBatchId { get; set; }
    public AchBatch AchBatch { get; set; } = null!;

    public ICollection<AchTransactionAddenda> Addendas { get; set; } = new List<AchTransactionAddenda>();
    public ICollection<AchTransactionStateEvent> StateEvents { get; set; } = new List<AchTransactionStateEvent>();
    public ContrapartidaDispatchItem? ContrapartidaDispatchItem { get; set; }
}
