using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

/// <summary>
/// Evento persistido que forma parte del historial de estados de una transacción.
/// </summary>
public class AchTraceabilityEventDto
{
    /// <summary>Identificador interno del evento de estado.</summary>
    public long Id { get; set; }

    /// <summary>Fecha y hora de creación persistida del evento.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Estado de la transacción antes del evento.</summary>
    public AchTransferStateEnum FromState { get; set; }

    /// <summary>Estado de la transacción después del evento.</summary>
    public AchTransferStateEnum ToState { get; set; }

    /// <summary>Fuente registrada para la transición de estado.</summary>
    public AchStateEventSourceEnum Source { get; set; }

    /// <summary>Código de razón persistido para el evento, cuando fue informado.</summary>
    public string? ReasonCode { get; set; }

    /// <summary>Payload JSON persistido con datos adicionales de la transición, cuando existe.</summary>
    public string? PayloadJson { get; set; }
}

/// <summary>
/// Vista detallada de trazabilidad de una transacción y de sus eventos de estado.
/// </summary>
public class AchTraceabilityDetailDto
{
    /// <summary>Identificador interno de la transacción.</summary>
    public int TransactionId { get; set; }

    /// <summary>Identificador operativo externo persistido para la transacción.</summary>
    public string TransactionExternalId { get; set; } = string.Empty;

    /// <summary>Referencia histórica persistida de la transacción.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Número de trazabilidad persistido de la transacción.</summary>
    public string TraceNumber { get; set; } = string.Empty;

    /// <summary>Referencia de trazabilidad original persistida, cuando aplica.</summary>
    public string OriginalTraceRef { get; set; } = string.Empty;

    /// <summary>Código transaccional persistido.</summary>
    public string TransactionCode { get; set; } = string.Empty;

    /// <summary>Monto persistido de la transacción.</summary>
    public decimal Amount { get; set; }

    /// <summary>Fecha efectiva persistida de la transacción.</summary>
    public DateTime EffectiveEntryDate { get; set; }

    /// <summary>Identificador del ciclo asociado a la transacción.</summary>
    public string AchCycleId { get; set; } = string.Empty;

    /// <summary>Nombre del ciclo asociado, o cadena vacía cuando no está disponible.</summary>
    public string AchCycleName { get; set; } = string.Empty;

    /// <summary>Nombre de la cámara asociada al ciclo, o cadena vacía cuando no está disponible.</summary>
    public string ClearingHouseName { get; set; } = string.Empty;

    /// <summary>Código de la cámara asociada al ciclo, o cadena vacía cuando no está disponible.</summary>
    public string ClearingHouseCode { get; set; } = string.Empty;

    /// <summary>Nombre del archivo de exportación de tipo NACHA más reciente del ciclo, o cadena vacía si no existe.</summary>
    public string CurrentNachaFileName { get; set; } = string.Empty;

    /// <summary>Fecha UTC de generación del archivo NACHA más reciente del ciclo, cuando existe.</summary>
    public DateTime? CurrentNachaGeneratedAtUtc { get; set; }

    /// <summary>Nombre del archivo de devolución más reciente asociado a la transacción, o cadena vacía si no existe.</summary>
    public string ReturnFileName { get; set; } = string.Empty;

    /// <summary>Identificador del ciclo de devolución del archivo más reciente asociado, o cadena vacía si no existe.</summary>
    public string ReturnCycleId { get; set; } = string.Empty;

    /// <summary>Identificador de la transacción original registrado en la devolución más reciente, cuando existe.</summary>
    public int? ReturnOriginalTransactionId { get; set; }

    /// <summary>Fecha UTC de generación del archivo de devolución más reciente asociado, cuando existe.</summary>
    public DateTime? ReturnGeneratedAtUtc { get; set; }

    /// <summary>Nombre de la institución de origen asociada, o cadena vacía cuando no está disponible.</summary>
    public string SourceInstitutionName { get; set; } = string.Empty;

    /// <summary>Nombre de la institución de destino asociada, o cadena vacía cuando no está disponible.</summary>
    public string DestinationInstitutionName { get; set; } = string.Empty;

    /// <summary>Estado actual persistido de la transacción.</summary>
    public AchTransferStateEnum State { get; set; }

    /// <summary>Fecha UTC del cambio que estableció el estado actual.</summary>
    public DateTime StateChangedAtUtc { get; set; }

    /// <summary>Fecha límite SLA persistida para la transacción, cuando existe.</summary>
    public DateTime? SlaDeadlineAtUtc { get; set; }

    /// <summary>Código de devolución persistido; si está vacío, la consulta usa el código de la devolución generada más reciente cuando existe.</summary>
    public string ReturnReasonCode { get; set; } = string.Empty;

    /// <summary>Eventos de cambio de estado ordenados por fecha de creación ascendente.</summary>
    public List<AchTraceabilityEventDto> Events { get; set; } = [];
}

/// <summary>
/// Fila consolidada usada por la consulta de reporte de trazabilidad.
/// </summary>
public class AchTraceabilityReportRowDto
{
    /// <summary>Identificador interno de la transacción.</summary>
    public int TransactionId { get; set; }

    /// <summary>Identificador operativo externo persistido para la transacción.</summary>
    public string TransactionExternalId { get; set; } = string.Empty;

    /// <summary>Referencia histórica persistida de la transacción.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Número de trazabilidad persistido de la transacción.</summary>
    public string TraceNumber { get; set; } = string.Empty;

    /// <summary>Código transaccional persistido.</summary>
    public string TransactionCode { get; set; } = string.Empty;

    /// <summary>Monto persistido de la transacción.</summary>
    public decimal Amount { get; set; }

    /// <summary>Identificador del ciclo asociado a la transacción.</summary>
    public string AchCycleId { get; set; } = string.Empty;

    /// <summary>Nombre del ciclo asociado a la transacción.</summary>
    public string AchCycleName { get; set; } = string.Empty;

    /// <summary>Nombre de la cámara asociada al ciclo.</summary>
    public string ClearingHouseName { get; set; } = string.Empty;

    /// <summary>Código de la cámara asociada al ciclo.</summary>
    public string ClearingHouseCode { get; set; } = string.Empty;

    /// <summary>Nombre del archivo de exportación de tipo NACHA más reciente del ciclo, o cadena vacía si no existe.</summary>
    public string CurrentNachaFileName { get; set; } = string.Empty;

    /// <summary>Fecha efectiva persistida de la transacción.</summary>
    public DateTime EffectiveEntryDate { get; set; }

    /// <summary>Estado actual persistido de la transacción.</summary>
    public AchTransferStateEnum State { get; set; }

    /// <summary>Fecha UTC del cambio que estableció el estado actual.</summary>
    public DateTime StateChangedAtUtc { get; set; }

    /// <summary>Código de devolución persistido para la transacción.</summary>
    public string ReturnReasonCode { get; set; } = string.Empty;

    /// <summary>Cantidad de eventos de estado asociados a la transacción.</summary>
    public int EventsCount { get; set; }

    /// <summary>Nombre de la institución de origen asociada.</summary>
    public string SourceInstitutionName { get; set; } = string.Empty;

    /// <summary>Nombre de la institución de destino asociada.</summary>
    public string DestinationInstitutionName { get; set; } = string.Empty;
}
