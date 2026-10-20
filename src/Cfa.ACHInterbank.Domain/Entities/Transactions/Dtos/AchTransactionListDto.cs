using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

/// <summary>
/// Proyección resumida utilizada por el listado operativo de transacciones.
/// </summary>
public class AchTransactionListDto
{
    /// <summary>Identificador interno de la transacción.</summary>
    public int Id { get; set; }

    /// <summary>Monto registrado para la transacción.</summary>
    public decimal Amount { get; set; }

    /// <summary>Identificador operativo/idempotencia canónico de la transacción.</summary>
    public string TransactionExternalId { get; set; } = string.Empty;

    /// <summary>Referencia legada o transicional conservada para compatibilidad.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Tipo de transacción registrado.</summary>
    public TransactionTypeEnum Type { get; set; }

    /// <summary>Número de trazabilidad asociado a la transacción.</summary>
    public string TraceNumber { get; set; } = string.Empty;

    /// <summary>Fecha efectiva registrada para la transacción.</summary>
    public DateTime EffectiveEntryDate { get; set; }

    /// <summary>Fecha y hora de creación persistida por ACHInterbank.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Número de la cuenta de origen.</summary>
    public string SourceAccountNumber { get; set; } = string.Empty;

    /// <summary>Número de la cuenta de destino.</summary>
    public string DestinationAccountNumber { get; set; } = string.Empty;

    /// <summary>Nombre de la institución de origen asociada.</summary>
    public string SourceInstitutionName { get; set; } = string.Empty;

    /// <summary>Nombre de la institución de destino asociada.</summary>
    public string DestinationInstitutionName { get; set; } = string.Empty;

    /// <summary>Indica si la transacción corresponde a una prenotificación.</summary>
    public bool IsPrenotification { get; set; }

    /// <summary>Código transaccional persistido para la instrucción.</summary>
    public string TransactionCode { get; set; } = string.Empty;

    /// <summary>Identificador interno del lote al que pertenece la transacción.</summary>
    public int AchBatchId { get; set; }

    /// <summary>Número secuencial del lote asociado.</summary>
    public int BatchSequenceNumber { get; set; }

    /// <summary>Nombre del originador almacenado en el lote asociado.</summary>
    public string BatchCompanyName { get; set; } = string.Empty;

    /// <summary>Fecha efectiva almacenada en el lote asociado.</summary>
    public DateTime BatchEffectiveEntryDate { get; set; }

    /// <summary>Identificador del ciclo ACH asociado.</summary>
    public string AchCycleId { get; set; } = string.Empty;

    /// <summary>Nombre del ciclo ACH asociado.</summary>
    public string AchCycleName { get; set; } = string.Empty;

    /// <summary>Nombre de la cámara compensadora asociada al ciclo.</summary>
    public string ClearingHouseName { get; set; } = string.Empty;
}
