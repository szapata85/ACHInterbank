using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>Representa el estado persistido de una transacción ACH expuesto por el contrato actual de transacciones.</summary>
public class AchTransaction : AuditableEntity
{
    /// <summary>Identificador interno de la transacción.</summary>
    public int Id { get; set; }

    /// <summary>Monto persistido de la transacción.</summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Identificador operativo/idempotencia canónico de la instrucción.
    /// </summary>
    public string TransactionExternalId { get; set; } = string.Empty;
    /// <summary>
    /// Referencia histórica legada o transicional; no es el identificador operativo principal cuando existe TransactionExternalId.
    /// </summary>
    public string Reference { get; set; } = null!;
    /// <summary>Tipo de transacción persistido.</summary>
    public TransactionTypeEnum Type { get; set; }

    /// <summary>Código transaccional resuelto por el flujo de registro.</summary>
    public string TransactionCode { get; set; } = string.Empty;
    /// <summary>Código de clase de servicio NACHA-M persistido para la transacción.</summary>
    public string ServiceClassCode { get; set; } = "200";
    /// <summary>Identificador de la Company Entry Description seleccionada al registrar la transacción.</summary>
    public int CompanyEntryDescriptionId { get; set; }
    /// <summary>Nombre del originador persistido para la transacción.</summary>
    public string CompanyName { get; set; } = string.Empty;
    /// <summary>Identificación del originador persistida para la transacción.</summary>
    public string CompanyIdentification { get; set; } = string.Empty;

    /// <summary>Código DFI originador persistido para la transacción.</summary>
    public string OriginatingDFI { get; set; } = string.Empty;
    /// <summary>Código DFI receptor persistido para la transacción.</summary>
    public string ReceivingDFI { get; set; } = string.Empty;

    /// <summary>Número de trazabilidad asociado a la transacción.</summary>
    public string TraceNumber { get; set; } = string.Empty;
    /// <summary>Secuencia numérica asignada al generar el número de trazabilidad de la transacción.</summary>
    public int TraceSequenceNumber { get; set; }

    /// <summary>Fecha efectiva asignada a la transacción.</summary>
    public DateTime EffectiveEntryDate { get; set; }
    /// <summary>Indica si el registro de detalle declara presencia de addenda.</summary>
    public bool AddendaRecordIndicator { get; set; }
    /// <summary>Indica si la transacción corresponde a una prenotificación.</summary>
    public bool IsPrenotification { get; set; }

    /// <summary>
    /// Clasificación histórica fijada al crear la transacción. Los registros anteriores a
    /// Fase 1A permanecen Unknown hasta que exista evidencia inequívoca para clasificarlos.
    /// </summary>
    public AchTransactionDirection Direction { get; set; } = AchTransactionDirection.Unknown;
    /// <summary>Origen de la transacción resultante de la clasificación persistida.</summary>
    public AchTransactionOrigin Origin { get; set; } = AchTransactionOrigin.Unknown;
    /// <summary>Ruta de integración monetaria resultante de la clasificación persistida.</summary>
    public AchMonetaryIntegrationRoute MonetaryIntegrationRoute { get; set; } = AchMonetaryIntegrationRoute.ManualReview;
    /// <summary>Estado del resultado de clasificación persistido para la transacción.</summary>
    public AchTransactionClassificationStatus ClassificationStatus { get; set; } = AchTransactionClassificationStatus.Unknown;
    /// <summary>Indica, cuando está registrado, si la institución de origen estaba marcada como predeterminada al clasificar la transacción.</summary>
    public bool? SourceInstitutionWasDefaultAtCreation { get; set; }
    /// <summary>Fecha y hora UTC registrada por la clasificación de la transacción, cuando existe.</summary>
    public DateTime? ClassifiedAtUtc { get; set; }
    /// <summary>Versión de la política de clasificación persistida para la transacción.</summary>
    public int ClassificationVersion { get; set; }

    /// <summary>Estado actual persistido de la transacción.</summary>
    public AchTransferStateEnum State { get; set; } = AchTransferStateEnum.Pending;
    /// <summary>Fecha y hora UTC del cambio que estableció el estado actual persistido.</summary>
    public DateTime StateChangedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Fecha y hora límite SLA persistida para la transacción, cuando existe.</summary>
    public DateTime? SlaDeadlineAtUtc { get; set; }
    /// <summary>Código de respuesta de Contrapartidas persistido cuando ese flujo informa un resultado.</summary>
    public string ContrapartidasResponseCode { get; set; } = string.Empty;
    /// <summary>Código de causal de devolución persistido cuando la transacción se marca con una devolución.</summary>
    public string ReturnReasonCode { get; set; } = string.Empty;
    /// <summary>Referencia de trazabilidad original persistida cuando el flujo de devolución o retorno la informa.</summary>
    public string OriginalTraceRef { get; set; } = string.Empty;
    /// <summary>Identificación del receptor cuando fue informada en el flujo de registro.</summary>
    public string RecipientIdNumber { get; set; } = string.Empty;
    /// <summary>Dato discrecional persistido para el registro de detalle NACHA-M.</summary>
    public string DiscretionaryData { get; set; } = string.Empty;

    /// <summary>Número de la cuenta de origen.</summary>
    public string SourceAccountNumber { get; set; } = null!;
    /// <summary>Número de la cuenta de destino.</summary>
    public string DestinationAccountNumber { get; set; } = null!;

    /// <summary>Identificador de la institución de origen asociada.</summary>
    public int SourceInstitutionId { get; set; }
    public FinancialInstitution SourceInstitution { get; set; } = null!;

    /// <summary>Identificador de la institución de destino asociada.</summary>
    public int DestinationInstitutionId { get; set; }
    public FinancialInstitution DestinationInstitution { get; set; } = null!;

    /// <summary>Identificador del ciclo ACH asociado.</summary>
    public string AchCycleId { get; set; } = null!;
    public AchCycle AchCycle { get; set; } = null!;

    /// <summary>Identificador interno del lote ACH asociado.</summary>
    public int AchBatchId { get; set; }
    public AchBatch AchBatch { get; set; } = null!;

    /// <summary>Addendas persistidas asociadas a la transacción.</summary>
    public ICollection<AchTransactionAddenda> Addendas { get; set; } = new List<AchTransactionAddenda>();
    public ICollection<AchTransactionStateEvent> StateEvents { get; set; } = new List<AchTransactionStateEvent>();
    public ContrapartidaDispatchItem? ContrapartidaDispatchItem { get; set; }
    public ICollection<AchFileExportTransaction> FileExportMemberships { get; set; } = new List<AchFileExportTransaction>();
}
