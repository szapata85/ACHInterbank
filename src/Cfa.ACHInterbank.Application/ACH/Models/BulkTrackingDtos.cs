using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Estado consolidado y contadores operativos de un lote de ingestión masiva.
/// </summary>
public sealed class BulkBatchStatusDto
{
    /// <summary>Identificador único del lote.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Referencia operativa efectiva del lote.</summary>
    public string BatchReference { get; set; } = string.Empty;

    /// <summary>Estado consolidado actual del lote.</summary>
    public BulkIngestionBatchStatusEnum Status { get; set; }

    /// <summary>Cantidad total de registros detectados en el archivo.</summary>
    public int TotalRecords { get; set; }

    /// <summary>Cantidad de registros que superaron la validación estructural inicial.</summary>
    public int TotalValid { get; set; }

    /// <summary>Cantidad de registros clasificados como errores estructurales.</summary>
    public int TotalInvalid { get; set; }

    /// <summary>Cantidad de registros contabilizados como procesados por el lote.</summary>
    public int TotalProcessed { get; set; }

    /// <summary>Cantidad de registros procesados correctamente.</summary>
    public int TotalSucceeded { get; set; }

    /// <summary>Conteo de fallos acumulado en el lote según el procesamiento persistido.</summary>
    public int TotalFailed { get; set; }

    /// <summary>Porcentaje de avance calculado a partir del estado y los contadores actuales del lote.</summary>
    public decimal ProgressPercent { get; set; }

    /// <summary>Fecha y hora UTC en la que se registró la carga.</summary>
    public DateTime UploadedAtUtc { get; set; }

    /// <summary>Fecha y hora UTC de inicio del procesamiento cuando éste ya comenzó.</summary>
    public DateTime? ProcessingStartedAtUtc { get; set; }

    /// <summary>Fecha y hora UTC de finalización registrada para el procesamiento cuando existe.</summary>
    public DateTime? ProcessingFinishedAtUtc { get; set; }

    /// <summary>Cantidad de reintentos que han iniciado procesamiento para el lote.</summary>
    public int RetryCount { get; set; }

    /// <summary>Identificador del último job asociado al lote cuando existe.</summary>
    public string? LastJobId { get; set; }

    /// <summary>Último mensaje operativo persistido respecto del job o del estado del lote.</summary>
    public string LastJobMessage { get; set; } = string.Empty;

    /// <summary>Resumen persistido de errores relevantes del lote.</summary>
    public IReadOnlyList<string> ErrorSummary { get; set; } = [];
}

/// <summary>
/// Vista de seguimiento de un registro individual perteneciente a un lote de ingestión.
/// </summary>
public sealed class BulkBatchItemDto
{
    /// <summary>Identificador persistido del ítem dentro del lote.</summary>
    public long ItemId { get; set; }

    /// <summary>Índice del registro según la posición registrada durante el parseo del archivo.</summary>
    public int ItemIndex { get; set; }

    /// <summary>Referencia operativa del ítem derivada del identificador o referencia disponible en la carga.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Estado actual del ítem dentro del flujo de ingestión y procesamiento.</summary>
    public BulkIngestionItemStatusEnum Status { get; set; }

    /// <summary>Mensaje operativo o de error asociado al estado actual del ítem.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Identificador de la transacción creada cuando el ítem se procesó correctamente; en otro caso puede ser nulo.</summary>
    public int? TransactionId { get; set; }
}

/// <summary>
/// Página de ítems de un lote después de aplicar paginación y filtro opcional por estado.
/// </summary>
public sealed class BulkBatchItemsPageDto
{
    /// <summary>Página efectiva devuelta después de normalizar el valor solicitado a un mínimo de 1.</summary>
    public int Page { get; set; }

    /// <summary>Tamaño de página efectivo después de limitarlo al rango de 1 a 500.</summary>
    public int PageSize { get; set; }

    /// <summary>Cantidad total de ítems que cumplen el filtro de estado antes de aplicar paginación.</summary>
    public int Total { get; set; }

    /// <summary>Ítems de la página actual, ordenados por ItemIndex.</summary>
    public IReadOnlyList<BulkBatchItemDto> Items { get; set; } = [];
}

/// <summary>
/// Registro de un intento inicial o de reintento asociado al procesamiento de un lote.
/// </summary>
public sealed class BulkBatchAttemptDto
{
    /// <summary>Identificador persistido del intento.</summary>
    public long AttemptId { get; set; }

    /// <summary>Número secuencial del intento dentro del lote.</summary>
    public int AttemptNumber { get; set; }

    /// <summary>Origen textual del intento, por ejemplo Initial o Retry.</summary>
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>Alcance textual del intento, por ejemplo Full o FailedOnly.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Identidad registrada como solicitante del intento.</summary>
    public string TriggeredBy { get; set; } = string.Empty;

    /// <summary>Fecha y hora UTC en la que se creó el intento.</summary>
    public DateTime TriggeredAtUtc { get; set; }

    /// <summary>Estado textual actual o final del intento.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Identificador del job asociado al intento cuando fue despachado.</summary>
    public string? JobId { get; set; }

    /// <summary>Fecha y hora UTC de inicio del intento cuando comenzó a procesarse.</summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>Fecha y hora UTC de finalización del intento cuando terminó.</summary>
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>Cantidad de ítems procesados contabilizados para este intento.</summary>
    public int TotalProcessed { get; set; }

    /// <summary>Cantidad de ítems exitosos contabilizados para este intento.</summary>
    public int TotalSucceeded { get; set; }

    /// <summary>Cantidad de ítems fallidos contabilizados para este intento.</summary>
    public int TotalFailed { get; set; }

    /// <summary>Mensaje final o de seguimiento persistido para el intento.</summary>
    public string ResultMessage { get; set; } = string.Empty;
}

/// <summary>
/// Resumen de procesamiento que combina el estado consolidado del lote y su historial de intentos.
/// </summary>
public sealed class BulkBatchProcessingSummaryDto
{
    /// <summary>Identificador del lote resumido.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Estado consolidado y contadores actuales del lote.</summary>
    public BulkBatchStatusDto Status { get; set; } = new();

    /// <summary>Historial de intentos ordenado del número de intento más reciente al más antiguo.</summary>
    public IReadOnlyList<BulkBatchAttemptDto> Attempts { get; set; } = [];
}

/// <summary>
/// Solicitud de reintento para un lote de ingestión masiva.
/// </summary>
public sealed class RetryBatchRequest
{
    /// <summary>Alcance del reintento. FailedOnly selecciona ítems ProcessingError; Full selecciona todos excepto StructuralError. El valor por defecto es FailedOnly.</summary>
    public BulkIngestionRetryScopeEnum Scope { get; set; } = BulkIngestionRetryScopeEnum.FailedOnly;
}

/// <summary>
/// Resultado inmediato de la creación y despacho de un nuevo intento de procesamiento.
/// </summary>
public sealed class RetryBatchResponse
{
    /// <summary>Identificador del lote reintentado.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Identificador persistido del nuevo intento.</summary>
    public long AttemptId { get; set; }

    /// <summary>Número secuencial asignado al nuevo intento.</summary>
    public int AttemptNumber { get; set; }

    /// <summary>Identificador del job despachado para procesar el reintento.</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>Estado del lote al responder; el servicio lo establece en Retrying antes de despachar el procesamiento.</summary>
    public BulkIngestionBatchStatusEnum Status { get; set; }
}
