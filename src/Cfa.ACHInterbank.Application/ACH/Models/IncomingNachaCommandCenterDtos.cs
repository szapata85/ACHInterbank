using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Resultado paginado usado por las consultas del command center inbound.</summary>
/// <typeparam name="T">Tipo de elemento contenido en la página.</typeparam>
/// <param name="Items">Elementos de la página solicitada.</param>
/// <param name="Page">Número de página efectivo aplicado por el servicio.</param>
/// <param name="PageSize">Tamaño de página efectivo aplicado por el servicio.</param>
/// <param name="TotalItems">Cantidad total de elementos que cumplen los filtros antes de paginar.</param>
public sealed record IncomingNachaPageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems);

/// <summary>Parámetros de consulta para las ingestas NACHA-M entrantes.</summary>
public sealed class IncomingNachaIngestionQuery
{
    /// <summary>Número de página solicitado. El servicio aplica un mínimo efectivo de 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página solicitado. El servicio lo limita al rango de 1 a 200.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Estado de ingesta opcional usado como filtro exacto.</summary>
    public IncomingNachaIngestionStatus? IngestionStatus { get; set; }

    /// <summary>Estado de parseo opcional usado como filtro exacto.</summary>
    public IncomingNachaParsingStatus? ParsingStatus { get; set; }

    /// <summary>Texto opcional buscado dentro del identificador de correlación.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Texto opcional buscado dentro del nombre de archivo.</summary>
    public string? FileName { get; set; }

    /// <summary>Identificador opcional de la cámara resuelta para la ingesta.</summary>
    public int? ClearingHouseId { get; set; }

    /// <summary>Identificador opcional del ciclo ACH resuelto.</summary>
    public string? AchCycleId { get; set; }

    /// <summary>Fecha operacional opcional; el servicio compara únicamente la fecha.</summary>
    public DateTime? OperationalDate { get; set; }

    /// <summary>Límite inferior opcional, inclusivo, para UploadedAtUtc.</summary>
    public DateTime? UploadedFromUtc { get; set; }

    /// <summary>Límite superior opcional, inclusivo, para UploadedAtUtc.</summary>
    public DateTime? UploadedToUtc { get; set; }

    /// <summary>Código de resultado opcional que debe existir en alguna ejecución asociada.</summary>
    public string? ResultCode { get; set; }

    /// <summary>Resultado funcional opcional que debe existir en alguna ejecución asociada.</summary>
    public IncomingNachaBusinessOutcome? BusinessOutcome { get; set; }

    /// <summary>Cuando se informa, filtra por presencia o ausencia de fallas técnicas en ejecuciones asociadas.</summary>
    public bool? HasTechnicalErrors { get; set; }

    /// <summary>Cuando se informa, filtra por presencia o ausencia de etapas fallidas/rechazadas o resultados con incidencia.</summary>
    public bool? HasIssues { get; set; }

    /// <summary>Criterio de ordenamiento. fileName, operationalDate y cycle tienen tratamiento explícito; cualquier otro valor usa UploadedAtUtc.</summary>
    public string SortBy { get; set; } = "uploadedAtUtc";

    /// <summary>Indica si el ordenamiento efectivo se aplica de forma descendente.</summary>
    public bool SortDescending { get; set; } = true;
}

/// <summary>Vista resumida de una ingesta NACHA-M entrante.</summary>
/// <param name="Id">Identificador de la ingesta.</param>
/// <param name="FileName">Nombre de archivo registrado para la ingesta.</param>
/// <param name="CorrelationId">Identificador de correlación persistido.</param>
/// <param name="IngestionStatus">Estado actual de la ingesta.</param>
/// <param name="CycleResolutionStatus">Estado de resolución de cámara/ciclo.</param>
/// <param name="ParsingStatus">Estado de parseo persistido.</param>
/// <param name="ResolvedClearingHouseId">Identificador de cámara resuelto, cuando existe.</param>
/// <param name="ResolvedAchCycleId">Identificador de ciclo ACH resuelto, cuando existe.</param>
/// <param name="OperationalDate">Fecha operacional resuelta, cuando existe.</param>
/// <param name="IsReprocess">Indica si la ingesta fue registrada como reproceso.</param>
/// <param name="UploadedAtUtc">Fecha UTC de carga registrada.</param>
/// <param name="QueueItems">Cantidad de ítems de cola asociados.</param>
/// <param name="ProcessingEvents">Cantidad de eventos de procesamiento asociados.</param>
public sealed record IncomingNachaIngestionListItemDto(
    Guid Id,
    string FileName,
    string CorrelationId,
    IncomingNachaIngestionStatus IngestionStatus,
    IncomingNachaCycleResolutionStatus CycleResolutionStatus,
    IncomingNachaParsingStatus ParsingStatus,
    int? ResolvedClearingHouseId,
    string? ResolvedAchCycleId,
    DateTime? OperationalDate,
    bool IsReprocess,
    DateTime UploadedAtUtc,
    int QueueItems,
    int ProcessingEvents)
{
    /// <summary>Descripción operativa del estado de ingesta.</summary>
    public string IngestionStatusText { get; init; } = string.Empty;

    /// <summary>Código textual de la etapa actual.</summary>
    public string StageCode { get; init; } = string.Empty;

    /// <summary>Descripción operativa de la etapa actual.</summary>
    public string StageText { get; init; } = string.Empty;

    /// <summary>Nombre de la cámara resuelta o texto de fallback cuando no existe catálogo.</summary>
    public string ClearingHouseName { get; init; } = string.Empty;

    /// <summary>Usuario de carga o Sistema cuando no existe valor persistido.</summary>
    public string UploadedBy { get; init; } = string.Empty;

    /// <summary>Total de lotes reportado por el último resultado de procesamiento.</summary>
    public int TotalBatches { get; init; }

    /// <summary>Total de transacciones reportado por el último resultado de procesamiento.</summary>
    public int TotalTransactions { get; init; }

    /// <summary>Total débito consolidado desde controles de archivo.</summary>
    public decimal TotalDebit { get; init; }

    /// <summary>Total crédito consolidado desde controles de archivo.</summary>
    public decimal TotalCredit { get; init; }

    /// <summary>Descripción operativa calculada a partir de la cola y la etapa de la ingesta.</summary>
    public string ProcessingStatusText { get; init; } = string.Empty;

    /// <summary>Descripción del resultado global calculada a partir de ejecuciones, fallas técnicas y etapa.</summary>
    public string OverallResultText { get; init; } = string.Empty;

    /// <summary>Fecha UTC más antigua de creación de un ítem de cola asociado, cuando existe.</summary>
    public DateTime? ScheduledAtUtc { get; init; }

    /// <summary>Indica si alguna ejecución asociada está marcada como falla técnica.</summary>
    public bool HasTechnicalErrors { get; init; }

    /// <summary>Indica si la ingesta presenta una etapa fallida/rechazada o ejecuciones con incidencia.</summary>
    public bool HasIssues { get; init; }
}

/// <summary>Detalle operativo de una ingesta NACHA-M entrante.</summary>
/// <param name="Id">Identificador de la ingesta.</param>
/// <param name="FileName">Nombre de archivo registrado.</param>
/// <param name="CorrelationId">Identificador de correlación persistido.</param>
/// <param name="IngestionStatus">Estado actual de la ingesta.</param>
/// <param name="CycleResolutionStatus">Estado de resolución de cámara/ciclo.</param>
/// <param name="ParsingStatus">Estado de parseo persistido.</param>
/// <param name="DetectedClearingHouseId">Cámara detectada durante admisión, cuando existe.</param>
/// <param name="ResolvedClearingHouseId">Cámara finalmente resuelta, cuando existe.</param>
/// <param name="ResolvedAchCycleId">Ciclo ACH resuelto, cuando existe.</param>
/// <param name="OperationalDate">Fecha operacional resuelta, cuando existe.</param>
/// <param name="Notes">Notas persistidas de la ingesta.</param>
/// <param name="IsReprocess">Indica si corresponde a un reproceso.</param>
/// <param name="ParentIngestionId">Ingesta origen del reproceso, cuando existe.</param>
/// <param name="Queue">Ítems de cola asociados devueltos por la consulta de detalle.</param>
/// <param name="Events">Eventos de procesamiento más recientes asociados.</param>
public sealed record IncomingNachaIngestionDetailDto(
    Guid Id,
    string FileName,
    string CorrelationId,
    IncomingNachaIngestionStatus IngestionStatus,
    IncomingNachaCycleResolutionStatus CycleResolutionStatus,
    IncomingNachaParsingStatus ParsingStatus,
    int? DetectedClearingHouseId,
    int? ResolvedClearingHouseId,
    string? ResolvedAchCycleId,
    DateTime? OperationalDate,
    string Notes,
    bool IsReprocess,
    Guid? ParentIngestionId,
    IReadOnlyList<IncomingNachaQueueListItemDto> Queue,
    IReadOnlyList<IncomingNachaProcessingEventDto> Events)
{
    /// <summary>Resumen de conteos y totales derivados de la ingesta.</summary>
    public IncomingNachaFileSummaryDto Summary { get; init; } = IncomingNachaFileSummaryDto.Empty;

    /// <summary>Incidencia de admisión persistida, cuando existe.</summary>
    public IncomingNachaAdmissionIssue? AdmissionIssue { get; init; }

    /// <summary>Descripción operativa del estado de ingesta.</summary>
    public string IngestionStatusText { get; init; } = string.Empty;

    /// <summary>Código textual de la etapa actual.</summary>
    public string StageCode { get; init; } = string.Empty;

    /// <summary>Descripción operativa de la etapa actual.</summary>
    public string StageText { get; init; } = string.Empty;

    /// <summary>Nombre de la cámara resuelta o texto de fallback.</summary>
    public string ClearingHouseName { get; init; } = string.Empty;

    /// <summary>Usuario de carga o Sistema cuando no existe valor persistido.</summary>
    public string UploadedBy { get; init; } = string.Empty;

    /// <summary>Fecha UTC de carga registrada.</summary>
    public DateTime UploadedAtUtc { get; init; }

    /// <summary>Fecha UTC de recepción registrada, cuando existe.</summary>
    public DateTime? ReceivedAtUtc { get; init; }

    /// <summary>Descripción del resultado global calculada por el servicio.</summary>
    public string OverallResultText { get; init; } = string.Empty;

    /// <summary>Cantidad de ítems de cola que no están Confirmed ni FailedFinal.</summary>
    public int PendingTransactions { get; init; }
}

/// <summary>Resumen agregado del contenido y resultados de una ingesta.</summary>
/// <param name="TotalBatches">Total de lotes reportado por el último procesamiento.</param>
/// <param name="TotalTransactions">Total de transacciones reportado por el último procesamiento.</param>
/// <param name="TotalAddendas">Total de adendas reportado por el último procesamiento.</param>
/// <param name="TotalDebit">Total débito consolidado desde controles de archivo.</param>
/// <param name="TotalCredit">Total crédito consolidado desde controles de archivo.</param>
/// <param name="SuccessfulTransactions">Cantidad de ejecuciones con resultado Successful.</param>
/// <param name="RejectedTransactions">Cantidad de ejecuciones con resultado Rejected.</param>
/// <param name="ReturnedTransactions">Cantidad de ejecuciones con resultado Returned.</param>
/// <param name="TechnicalFailures">Cantidad de ejecuciones marcadas como falla técnica.</param>
public sealed record IncomingNachaFileSummaryDto(
    int TotalBatches,
    int TotalTransactions,
    int TotalAddendas,
    decimal TotalDebit,
    decimal TotalCredit,
    int SuccessfulTransactions,
    int RejectedTransactions,
    int ReturnedTransactions,
    int TechnicalFailures)
{
    public static IncomingNachaFileSummaryDto Empty { get; } = new(0, 0, 0, 0m, 0m, 0, 0, 0, 0);
}

/// <summary>Resultado de una validación de admisión asociada a una ingesta entrante.</summary>
/// <param name="Code">Código estable usado por el servicio para identificar el resultado de admisión.</param>
/// <param name="Title">Título funcional del resultado de validación.</param>
/// <param name="Message">Explicación registrada o derivada para el resultado.</param>
/// <param name="ExpectedValue">Valor esperado cuando la validación compara un dato concreto; puede ser nulo.</param>
/// <param name="FoundValue">Valor encontrado cuando la validación compara un dato concreto; puede ser nulo.</param>
/// <param name="SuggestedAction">Acción sugerida por el servicio para continuar o remediar el caso.</param>
/// <param name="ErrorType">Clasificación textual del tipo de resultado.</param>
/// <param name="Severity">Severidad textual del resultado.</param>
/// <param name="IsSuccessful">Indica si el resultado representa una admisión satisfactoria.</param>
public sealed record IncomingNachaValidationDto(
    string Code,
    string Title,
    string Message,
    string? ExpectedValue,
    string? FoundValue,
    string SuggestedAction,
    string ErrorType,
    string Severity,
    bool IsSuccessful)
{
    /// <summary>Fecha UTC asociada al resultado cuando está disponible.</summary>
    public DateTime? OccurredAtUtc { get; init; }
}

/// <summary>Parámetros de consulta para la cola de despacho inbound.</summary>
public sealed class IncomingNachaQueueQuery
{
    /// <summary>Número de página solicitado. El servicio aplica un mínimo efectivo de 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página solicitado. El servicio lo limita al rango de 1 a 200.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Identificador opcional de la ingesta asociada.</summary>
    public Guid? IngestionId { get; set; }

    /// <summary>Estado de cola opcional usado como filtro exacto.</summary>
    public IncomingNachaDispatchQueueStatus? QueueStatus { get; set; }

    /// <summary>Identificador opcional de cámara usado como filtro exacto.</summary>
    public int? ClearingHouseId { get; set; }

    /// <summary>Identificador opcional de ciclo ACH usado como filtro exacto.</summary>
    public string? AchCycleId { get; set; }
}

/// <summary>Vista operativa de un ítem de dispatch queue inbound.</summary>
/// <param name="Id">Identificador del ítem de cola.</param>
/// <param name="IngestionId">Identificador de la ingesta asociada.</param>
/// <param name="AchTransactionId">Identificador de la transacción ACH asociada.</param>
/// <param name="AchCycleId">Identificador de ciclo ACH registrado en la cola.</param>
/// <param name="ClearingHouseId">Identificador de cámara registrado en la cola.</param>
/// <param name="QueueStatus">Estado actual de la cola.</param>
/// <param name="Priority">Prioridad persistida del ítem.</param>
/// <param name="AttemptCount">Cantidad de intentos registrada.</param>
/// <param name="NextAttemptAtUtc">Fecha UTC del próximo intento, cuando existe.</param>
/// <param name="LastAttemptAtUtc">Fecha UTC del último intento, cuando existe.</param>
/// <param name="LastErrorCode">Último código de error persistido.</param>
/// <param name="LastErrorMessage">Último mensaje de error persistido.</param>
/// <param name="LastResponseCode">Último código de respuesta persistido.</param>
/// <param name="ConfirmedAtUtc">Fecha UTC de confirmación, cuando existe.</param>
/// <param name="CreatedAtUtc">Fecha UTC de creación del ítem de cola.</param>
/// <param name="AllowedActions">Acciones manuales que la máquina de estados permite para el estado actual.</param>
public sealed record IncomingNachaQueueListItemDto(
    Guid Id,
    Guid IngestionId,
    int AchTransactionId,
    string AchCycleId,
    int ClearingHouseId,
    IncomingNachaDispatchQueueStatus QueueStatus,
    int Priority,
    int AttemptCount,
    DateTime? NextAttemptAtUtc,
    DateTime? LastAttemptAtUtc,
    string LastErrorCode,
    string LastErrorMessage,
    string LastResponseCode,
    DateTime? ConfirmedAtUtc,
    DateTimeOffset CreatedAtUtc,
    IncomingNachaAllowedActionsDto AllowedActions)
{
    /// <summary>Identificador del detalle de transacción clasificado, cuando existe.</summary>
    public int? EntryDetailId { get; init; }

    /// <summary>Descripción operativa del estado actual de cola.</summary>
    public string QueueStatusText { get; init; } = string.Empty;

    /// <summary>Fecha UTC usada por la vista como programación inicial; corresponde a CreatedAt.</summary>
    public DateTime ScheduledAtUtc { get; init; }

    /// <summary>Máximo de intentos configurado en las opciones de resiliencia vigentes.</summary>
    public int MaxAttempts { get; init; }

    /// <summary>Nombre de operación de integración mostrado por la vista.</summary>
    public string SoapOperation { get; init; } = "Proc_Transacciones";
}

/// <summary>Detalle operativo de un ítem de cola inbound.</summary>
/// <param name="Queue">Estado y acciones permitidas del ítem de cola.</param>
/// <param name="Ingestion">Resumen de la ingesta asociada.</param>
/// <param name="Classification">Clasificación funcional asociada al ítem.</param>
/// <param name="Executions">Ejecuciones de integración asociadas, ordenadas desde la más reciente.</param>
/// <param name="Events">Eventos de procesamiento relacionados devueltos por la consulta.</param>
public sealed record IncomingNachaQueueDetailDto(
    IncomingNachaQueueListItemDto Queue,
    IncomingNachaIngestionListItemDto Ingestion,
    IncomingNachaEntryClassificationDto Classification,
    IReadOnlyList<IncomingNachaIntegrationExecutionDto> Executions,
    IReadOnlyList<IncomingNachaProcessingEventDto> Events);

/// <summary>Clasificación funcional persistida para un detalle de transacción inbound.</summary>
/// <param name="Id">Identificador de la clasificación.</param>
/// <param name="EntryDetailId">Identificador del detalle clasificado.</param>
/// <param name="AddendaRecordId">Identificador de adenda asociado, cuando aplica.</param>
/// <param name="FunctionalClass">Clase funcional asignada.</param>
/// <param name="EligibilityStatus">Estado de elegibilidad asignado.</param>
/// <param name="RequiresManualResolution">Indica si la clasificación requiere resolución manual.</param>
/// <param name="ReturnReasonCode">Código de causal asociado, cuando existe.</param>
/// <param name="PrenoteStatus">Estado de prenotificación derivado.</param>
/// <param name="BusinessMeaning">Descripción funcional persistida.</param>
public sealed record IncomingNachaEntryClassificationDto(
    Guid Id,
    int EntryDetailId,
    int? AddendaRecordId,
    IncomingNachaFunctionalClass FunctionalClass,
    IncomingNachaEligibilityStatus EligibilityStatus,
    bool RequiresManualResolution,
    string? ReturnReasonCode,
    IncomingNachaPrenoteStatus PrenoteStatus,
    string BusinessMeaning);

/// <summary>Ejecución de integración asociada a un ítem de cola inbound.</summary>
/// <param name="Id">Identificador de la ejecución.</param>
/// <param name="DispatchQueueId">Identificador del ítem de cola asociado.</param>
/// <param name="EntryDetailId">Identificador del detalle de transacción, cuando la ejecución está asociada a uno.</param>
/// <param name="AttemptNumber">Número de intento registrado.</param>
/// <param name="MethodName">Nombre de método de integración registrado.</param>
/// <param name="CorrelationId">Identificador de correlación de la ejecución.</param>
/// <param name="ProcessingStatus">Estado de procesamiento registrado.</param>
/// <param name="ProcessingStatusText">Descripción operativa del estado de procesamiento.</param>
/// <param name="BusinessOutcome">Resultado funcional registrado.</param>
/// <param name="BusinessOutcomeText">Descripción operativa del resultado funcional.</param>
/// <param name="AchReturnCodeId">Identificador de causal ACH asociado, cuando existe.</param>
/// <param name="ResultCode">Código de resultado registrado.</param>
/// <param name="ResultDescription">Descripción de resultado registrada.</param>
/// <param name="IsSuccess">Indica si la ejecución está marcada como exitosa.</param>
/// <param name="IsRetryable">Indica si la ejecución está marcada como reintentable.</param>
/// <param name="StartedAtUtc">Fecha UTC de inicio.</param>
/// <param name="FinishedAtUtc">Fecha UTC de finalización, cuando existe.</param>
public sealed record IncomingNachaIntegrationExecutionDto(
    Guid Id,
    Guid DispatchQueueId,
    int? EntryDetailId,
    int AttemptNumber,
    string MethodName,
    string CorrelationId,
    IncomingNachaIndividualProcessingStatus ProcessingStatus,
    string ProcessingStatusText,
    IncomingNachaBusinessOutcome BusinessOutcome,
    string BusinessOutcomeText,
    int? AchReturnCodeId,
    string ResultCode,
    string ResultDescription,
    bool IsSuccess,
    bool IsRetryable,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc)
{
    /// <summary>Nombre lógico expuesto para el endpoint de integración cuando existe endpoint registrado.</summary>
    public string LogicalEndpoint { get; init; } = string.Empty;

    /// <summary>Duración registrada de la ejecución en milisegundos.</summary>
    public long DurationMs { get; init; }

    /// <summary>Código textual del estado de transporte.</summary>
    public string TransportStatusCode { get; init; } = string.Empty;

    /// <summary>Descripción operativa del estado de transporte.</summary>
    public string TransportStatusText { get; init; } = string.Empty;

    /// <summary>Código de error técnico registrado.</summary>
    public string TechnicalErrorCode { get; init; } = string.Empty;

    /// <summary>Mensaje de error técnico registrado.</summary>
    public string TechnicalErrorMessage { get; init; } = string.Empty;

    /// <summary>Origen registrado para el resultado de integración.</summary>
    public string ResultSource { get; init; } = string.Empty;

    /// <summary>Identificador externo de transacción registrado.</summary>
    public string ExternalTransactionId { get; init; } = string.Empty;

    /// <summary>Fecha UTC de procesamiento registrada, cuando existe.</summary>
    public DateTime? ProcessedAtUtc { get; init; }
}

/// <summary>Parámetros de consulta para los lotes de una ingesta entrante.</summary>
public sealed class IncomingNachaBatchQuery
{
    /// <summary>Número de página solicitado. El servicio aplica un mínimo efectivo de 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página solicitado. El servicio lo limita al rango de 1 a 200.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Filtro opcional aplicado al nombre de compañía o al número de lote.</summary>
    public string? Search { get; set; }

    /// <summary>Criterio de ordenamiento. companyName ordena por compañía; cualquier otro valor usa el número de lote.</summary>
    public string SortBy { get; set; } = "batchNumber";

    /// <summary>Indica si el ordenamiento efectivo se aplica de forma descendente.</summary>
    public bool SortDescending { get; set; }
}

/// <summary>Lote persistido perteneciente a una ingesta NACHA-M entrante.</summary>
/// <param name="Id">Identificador interno del encabezado de lote.</param>
/// <param name="BatchNumber">Número de lote persistido.</param>
/// <param name="CompanyName">Nombre de compañía persistido; se entrega vacío cuando no existe valor.</param>
/// <param name="ServiceClassCode">Código de clase de servicio persistido; se entrega vacío cuando no existe valor.</param>
/// <param name="StandardEntryClassCode">Código de clase de entrada estándar persistido; se entrega vacío cuando no existe valor.</param>
/// <param name="EffectiveEntryDate">Fecha efectiva almacenada en el encabezado de lote, cuando existe.</param>
/// <param name="TotalTransactions">Cantidad de detalles de transacción asociados al lote.</param>
/// <param name="TotalAmount">Suma de los montos de los detalles asociados al lote.</param>
/// <param name="TotalDebit">Total débito del control de lote, o cero cuando el control no está disponible.</param>
/// <param name="TotalCredit">Total crédito del control de lote, o cero cuando el control no está disponible.</param>
public sealed record IncomingNachaBatchDto(
    int Id,
    int BatchNumber,
    string CompanyName,
    string ServiceClassCode,
    string StandardEntryClassCode,
    string? EffectiveEntryDate,
    int TotalTransactions,
    decimal TotalAmount,
    decimal TotalDebit,
    decimal TotalCredit)
{
    /// <summary>Descripción de entrada de compañía persistida para el lote; se entrega vacía cuando no existe valor.</summary>
    public string CompanyEntryDescription { get; init; } = string.Empty;
}

/// <summary>Parámetros de consulta para las transacciones de una ingesta entrante.</summary>
public sealed class IncomingNachaTransactionQuery
{
    /// <summary>Número de página solicitado. El servicio aplica un mínimo efectivo de 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página solicitado. El servicio lo limita al rango de 1 a 200.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Identificador opcional del lote al que debe pertenecer la transacción.</summary>
    public int? BatchId { get; set; }

    /// <summary>Texto opcional buscado en el número de secuencia o en el nombre del receptor persistido.</summary>
    public string? Search { get; set; }

    /// <summary>Código de resultado opcional que debe aparecer en una ejecución de integración asociada.</summary>
    public string? ResultCode { get; set; }

    /// <summary>Resultado funcional opcional que debe aparecer en una ejecución de integración asociada.</summary>
    public IncomingNachaBusinessOutcome? BusinessOutcome { get; set; }

    /// <summary>Estado de procesamiento opcional que debe aparecer en una ejecución de integración asociada.</summary>
    public IncomingNachaIndividualProcessingStatus? ProcessingStatus { get; set; }

    /// <summary>Cuando se informa, filtra por presencia o ausencia de registros de adenda asociados.</summary>
    public bool? HasAddenda { get; set; }

    /// <summary>Cuando se informa, filtra por presencia o ausencia de una ejecución marcada como falla técnica.</summary>
    public bool? HasTechnicalError { get; set; }

    /// <summary>Código de transacción opcional usado como filtro exacto.</summary>
    public string? TransactionCode { get; set; }

    /// <summary>Criterio de ordenamiento. amount y batchNumber tienen tratamiento explícito; cualquier otro valor ordena por número de secuencia.</summary>
    public string SortBy { get; set; } = "traceNumber";

    /// <summary>Indica si el ordenamiento efectivo se aplica de forma descendente.</summary>
    public bool SortDescending { get; set; }
}

/// <summary>Vista operativa de una transacción persistida dentro de una ingesta entrante.</summary>
/// <param name="Id">Identificador interno del detalle de transacción.</param>
/// <param name="BatchId">Identificador del lote asociado; el servicio entrega cero cuando no existe vínculo.</param>
/// <param name="BatchNumber">Número de lote persistido en el detalle.</param>
/// <param name="TraceNumber">Número de secuencia persistido que la vista expone como trazabilidad; se entrega vacío cuando no existe.</param>
/// <param name="TransactionCode">Código de transacción persistido; se entrega vacío cuando no existe.</param>
/// <param name="Amount">Monto persistido del detalle; se entrega cero cuando no existe valor.</param>
/// <param name="AddendaCount">Cantidad de adendas asociadas al detalle.</param>
/// <param name="ClassificationCode">Código textual de la clasificación funcional encontrada o Pending cuando aún no existe.</param>
/// <param name="ClassificationText">Descripción operativa derivada de la clasificación funcional encontrada.</param>
/// <param name="DispatchStatusCode">Código textual del estado de cola más reciente encontrado o Pending cuando no existe cola.</param>
/// <param name="DispatchStatusText">Descripción operativa derivada del estado de despacho.</param>
/// <param name="AttemptCount">Cantidad de intentos registrada en el item de cola asociado; cero cuando no existe cola.</param>
/// <param name="ProcessingStatus">Estado de procesamiento de la ejecución seleccionada, cuando existe.</param>
/// <param name="ProcessingStatusText">Descripción operativa derivada del estado de procesamiento.</param>
/// <param name="BusinessOutcome">Resultado funcional de la ejecución seleccionada, cuando existe.</param>
/// <param name="BusinessOutcomeText">Descripción operativa derivada del resultado funcional.</param>
/// <param name="ResultCode">Código de resultado de la ejecución seleccionada; se entrega vacío cuando no existe.</param>
/// <param name="ResultDescription">Descripción de resultado de la ejecución seleccionada; se entrega vacía cuando no existe.</param>
/// <param name="ProcessedAtUtc">Fecha UTC de procesamiento de la ejecución seleccionada, cuando existe.</param>
/// <param name="CorrelationId">Identificador de correlación de la ejecución; si no existe ejecución, el servicio usa el identificador de ingesta sin separadores.</param>
public sealed record IncomingNachaTransactionDto(
    int Id,
    int BatchId,
    int BatchNumber,
    string TraceNumber,
    string TransactionCode,
    decimal Amount,
    int AddendaCount,
    string ClassificationCode,
    string ClassificationText,
    string DispatchStatusCode,
    string DispatchStatusText,
    int AttemptCount,
    IncomingNachaIndividualProcessingStatus? ProcessingStatus,
    string ProcessingStatusText,
    IncomingNachaBusinessOutcome? BusinessOutcome,
    string BusinessOutcomeText,
    string ResultCode,
    string ResultDescription,
    DateTime? ProcessedAtUtc,
    string CorrelationId)
{
    /// <summary>Identificador del item de dispatch queue asociado, cuando existe.</summary>
    public Guid? DispatchQueueId { get; init; }

    /// <summary>Identificador de cámara registrado en el item de cola; cero cuando no existe cola asociada.</summary>
    public int ClearingHouseId { get; init; }

    /// <summary>Fecha operacional registrada en el item de cola, cuando existe.</summary>
    public DateTime? OperationalDate { get; init; }

    /// <summary>Identificador de ciclo registrado en el item de cola; se entrega vacío cuando no existe cola.</summary>
    public string AchCycleId { get; init; } = string.Empty;

    /// <summary>Fecha UTC de creación del item de cola, expuesta como programación, cuando existe.</summary>
    public DateTime? ScheduledAtUtc { get; init; }

    /// <summary>Fecha UTC de inicio de la ejecución seleccionada, cuando existe.</summary>
    public DateTime? StartedAtUtc { get; init; }

    /// <summary>Fecha UTC de finalización de la ejecución seleccionada, cuando existe.</summary>
    public DateTime? FinishedAtUtc { get; init; }

    /// <summary>Fecha UTC del próximo intento registrada en la cola, cuando existe.</summary>
    public DateTime? NextRetryAtUtc { get; init; }

    /// <summary>Máximo de intentos configurado actualmente en las opciones de resiliencia usadas por el servicio.</summary>
    public int MaxAttempts { get; init; }

    /// <summary>Método de integración de la ejecución seleccionada o Proc_Transacciones cuando existe cola sin ejecución.</summary>
    public string SoapOperation { get; init; } = string.Empty;

    /// <summary>Identificador externo de la ejecución seleccionada; se entrega vacío cuando no existe.</summary>
    public string ExternalTransactionId { get; init; } = string.Empty;

    /// <summary>Identificador del catálogo de causal ACH asociado a la ejecución seleccionada, cuando existe.</summary>
    public int? AchReturnCodeId { get; init; }

    /// <summary>Código de error técnico de la ejecución seleccionada; se entrega vacío cuando no existe.</summary>
    public string TechnicalErrorCode { get; init; } = string.Empty;

    /// <summary>Mensaje de error técnico de la ejecución seleccionada; se entrega vacío cuando no existe.</summary>
    public string TechnicalErrorMessage { get; init; } = string.Empty;

    /// <summary>Descripción resuelta para el código de transacción o texto de fallback cuando el catálogo no aporta una descripción.</summary>
    public string TransactionCodeDescription { get; init; } = string.Empty;

    /// <summary>Número de cuenta expuesto con el enmascaramiento aplicado por el servicio.</summary>
    public string AccountNumberMasked { get; init; } = string.Empty;

    /// <summary>Institución originadora expuesta con el enmascaramiento aplicado por el servicio.</summary>
    public string OriginInstitution { get; init; } = string.Empty;

    /// <summary>Institución receptora expuesta con el enmascaramiento aplicado por el servicio.</summary>
    public string DestinationInstitution { get; init; } = string.Empty;

    /// <summary>Nombre del receptor expuesto con el enmascaramiento aplicado por el servicio.</summary>
    public string RecipientNameMasked { get; init; } = string.Empty;

    /// <summary>Fecha efectiva almacenada en el lote asociado; se entrega vacía cuando no está disponible.</summary>
    public string EffectiveEntryDate { get; init; } = string.Empty;
}

/// <summary>Adenda persistida asociada a un detalle de transacción de la ingesta consultada.</summary>
/// <param name="Id">Identificador interno del registro de adenda.</param>
/// <param name="TypeCode">Código de tipo de registro de adenda persistido; se entrega vacío cuando no existe.</param>
/// <param name="Sequence">Secuencia de adenda persistida; se entrega vacía cuando no existe.</param>
/// <param name="ReturnReasonCode">Código de causal persistido en la adenda; se entrega vacío cuando no existe.</param>
/// <param name="OriginalTraceNumber">Número de trazabilidad original expuesto con el enmascaramiento aplicado por el servicio.</param>
/// <param name="PaymentInformation">Información de pago expuesta con el enmascaramiento aplicado por el servicio.</param>
public sealed record IncomingNachaAddendaDto(
    int Id,
    string TypeCode,
    string Sequence,
    string ReturnReasonCode,
    string OriginalTraceNumber,
    string PaymentInformation);

/// <summary>Parámetros de consulta para devoluciones entrantes pendientes de relación.</summary>
public sealed class IncomingNachaOrphanQuery
{
    /// <summary>Número de página solicitado. El servicio aplica un mínimo efectivo de 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página solicitado. El servicio lo limita al rango de 1 a 100.</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>Texto opcional buscado en archivo, secuencia, trazabilidad original o código de causal.</summary>
    public string? Search { get; set; }
}

/// <summary>Vista operativa de una devolución entrante sin relación inequívoca o ya resuelta.</summary>
/// <param name="Id">Identificador del vínculo de transacción entrante.</param>
/// <param name="IngestionId">Identificador de la ingesta asociada.</param>
/// <param name="FileName">Nombre de archivo de la ingesta.</param>
/// <param name="ReceivedAtUtc">Fecha UTC de recepción o, si no está disponible, fecha del vínculo.</param>
/// <param name="ClearingHouseId">Identificador de cámara resuelto, cuando existe.</param>
/// <param name="AchCycleId">Identificador de ciclo ACH resuelto o cadena vacía.</param>
/// <param name="OperationalDate">Fecha operacional resuelta, cuando existe.</param>
/// <param name="EntryDetailId">Identificador del detalle asociado; cero cuando el vínculo no lo conserva.</param>
/// <param name="AddendaRecordId">Identificador de adenda asociado, cuando existe.</param>
/// <param name="Amount">Monto del detalle asociado o cero cuando no existe.</param>
/// <param name="TraceNumber">Número de secuencia del detalle o cadena vacía.</param>
/// <param name="OriginalTraceNumber">Referencia de trazabilidad original derivada de clasificación o adenda.</param>
/// <param name="ReturnReasonCode">Código de causal derivado de clasificación o adenda.</param>
/// <param name="ReturnReasonDescription">Descripción de causal activa encontrada para la cámara, o cadena vacía.</param>
/// <param name="AccountNumberMasked">Cuenta receptora con el enmascaramiento aplicado por el servicio.</param>
/// <param name="RecipientNameMasked">Nombre del receptor con el enmascaramiento aplicado por el servicio.</param>
/// <param name="OriginInstitution">Institución originadora con el enmascaramiento aplicado por el servicio.</param>
/// <param name="DestinationInstitution">Institución receptora con el enmascaramiento aplicado por el servicio.</param>
/// <param name="LinkType">Tipo de vínculo persistido.</param>
/// <param name="CandidateTransactionIds">Identificadores de candidatos recuperados de la evidencia persistida.</param>
/// <param name="ResolvedAchTransactionId">Transacción ACH resuelta, cuando existe.</param>
/// <param name="ResolutionStatus">Resuelta cuando el vínculo es final; Sin relación en caso contrario.</param>
/// <param name="ResolvedBy">Usuario que resolvió el vínculo cuando es final.</param>
/// <param name="ResolvedAtUtc">Fecha UTC de resolución cuando el vínculo es final.</param>
public sealed record IncomingNachaOrphanDto(
    Guid Id,
    Guid IngestionId,
    string FileName,
    DateTime ReceivedAtUtc,
    int? ClearingHouseId,
    string AchCycleId,
    DateTime? OperationalDate,
    int EntryDetailId,
    int? AddendaRecordId,
    decimal Amount,
    string TraceNumber,
    string OriginalTraceNumber,
    string ReturnReasonCode,
    string ReturnReasonDescription,
    string AccountNumberMasked,
    string RecipientNameMasked,
    string OriginInstitution,
    string DestinationInstitution,
    IncomingNachaLinkType LinkType,
    IReadOnlyList<int> CandidateTransactionIds,
    int? ResolvedAchTransactionId,
    string ResolutionStatus,
    string ResolvedBy,
    DateTime? ResolvedAtUtc);

/// <summary>Transacción candidata evaluada para una devolución entrante pendiente.</summary>
/// <param name="AchTransactionId">Identificador de la transacción ACH candidata.</param>
/// <param name="TraceNumber">Número de trazabilidad de la transacción.</param>
/// <param name="Amount">Monto de la transacción candidata.</param>
/// <param name="EffectiveEntryDate">Fecha efectiva de la transacción.</param>
/// <param name="State">Estado actual de la transacción como texto.</param>
/// <param name="AccountNumberMasked">Cuenta destino con el enmascaramiento aplicado por el servicio.</param>
/// <param name="OriginInstitution">Institución originadora con el enmascaramiento aplicado por el servicio.</param>
/// <param name="DestinationInstitution">Institución receptora con el enmascaramiento aplicado por el servicio.</param>
/// <param name="IsCompatible">Indica si la política de compatibilidad no encontró incompatibilidades.</param>
/// <param name="IncompatibilityReasons">Motivos de incompatibilidad calculados; vacío cuando la candidata es compatible.</param>
public sealed record IncomingNachaOrphanCandidateDto(
    int AchTransactionId,
    string TraceNumber,
    decimal Amount,
    DateTime EffectiveEntryDate,
    string State,
    string AccountNumberMasked,
    string OriginInstitution,
    string DestinationInstitution,
    bool IsCompatible,
    IReadOnlyList<string> IncompatibilityReasons);

/// <summary>Solicitud para relacionar manualmente una devolución entrante con una transacción ACH.</summary>
public sealed class IncomingNachaOrphanResolveRequestDto
{
    /// <summary>Identificador de la transacción ACH seleccionada como destino de la relación.</summary>
    public int AchTransactionId { get; set; }

    /// <summary>Justificación de la resolución. El servicio exige al menos 8 caracteres.</summary>
    public string Justification { get; set; } = string.Empty;

    /// <summary>Comentario adicional almacenado como evidencia de la resolución.</summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>Identificador de correlación que se conserva en la evidencia de resolución.</summary>
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>Evento de procesamiento asociado a una ingesta inbound.</summary>
/// <param name="Id">Identificador del evento.</param>
/// <param name="EventType">Tipo de evento persistido.</param>
/// <param name="EventStatus">Estado textual persistido del evento.</param>
/// <param name="Message">Mensaje persistido del evento.</param>
/// <param name="OccurredAtUtc">Fecha UTC del evento.</param>
/// <param name="RaisedBy">Actor registrado como originador del evento.</param>
/// <param name="AchTransactionId">Transacción ACH asociada, cuando existe.</param>
public sealed record IncomingNachaProcessingEventDto(
    Guid Id,
    string EventType,
    string EventStatus,
    string Message,
    DateTime OccurredAtUtc,
    string RaisedBy,
    int? AchTransactionId);

/// <summary>Solicitud común para acciones manuales sobre un ítem de dispatch queue.</summary>
public sealed class IncomingNachaManualActionRequest
{
    /// <summary>Justificación de la acción. El servicio exige al menos 8 caracteres.</summary>
    public string Justification { get; set; } = string.Empty;

    /// <summary>Clave de idempotencia recibida dentro del cuerpo de la solicitud; es obligatoria para la acción manual.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Prioridad opcional; retry, unblock y requeue la limitan a 1..999. Mark-failed-final no la utiliza.</summary>
    public int? Priority { get; set; }
}

/// <summary>Resultado de una acción manual sobre un ítem de dispatch queue.</summary>
/// <param name="QueueId">Identificador del ítem de cola afectado.</param>
/// <param name="Action">Código textual de la acción aplicada.</param>
/// <param name="PreviousStatus">Estado anterior del ítem.</param>
/// <param name="CurrentStatus">Estado resultante del ítem.</param>
/// <param name="IsIdempotentReplay">Indica si la misma solicitud ya había sido aplicada y no produjo efectos adicionales.</param>
/// <param name="Message">Mensaje generado por la transición o por el replay idempotente.</param>
public sealed record IncomingNachaManualActionResultDto(
    Guid QueueId,
    string Action,
    IncomingNachaDispatchQueueStatus PreviousStatus,
    IncomingNachaDispatchQueueStatus CurrentStatus,
    bool IsIdempotentReplay,
    string Message)
{
    /// <summary>Descripción operativa de la acción.</summary>
    public string ActionText { get; init; } = string.Empty;
}

/// <summary>Resumen de observabilidad calculado para una ventana horaria del command center inbound.</summary>
/// <param name="GeneratedAtUtc">Fecha UTC en la que se calculó el resumen.</param>
/// <param name="WindowHours">Cantidad efectiva de horas aplicada; el servicio la limita a 1..168.</param>
/// <param name="PipelineHealth">Indicadores agregados de salud de ingestas y cola.</param>
/// <param name="IngestionsByStatus">Conteos de ingestas agrupados por estado.</param>
/// <param name="QueueByStatus">Conteos de ítems de cola agrupados por estado.</param>
/// <param name="ByClearingHouseCycle">Indicadores de cola agrupados por cámara y ciclo.</param>
/// <param name="TopErrors">Hasta diez códigos de error de cola con mayor frecuencia.</param>
/// <param name="Timeline">Serie horaria de eventos dentro de la ventana efectiva.</param>
public sealed record IncomingNachaObservabilitySummaryDto(
    DateTime GeneratedAtUtc,
    int WindowHours,
    IncomingNachaPipelineHealthDto PipelineHealth,
    IReadOnlyList<IncomingNachaKpiCountDto> IngestionsByStatus,
    IReadOnlyList<IncomingNachaKpiCountDto> QueueByStatus,
    IReadOnlyList<IncomingNachaClearingCycleKpiDto> ByClearingHouseCycle,
    IReadOnlyList<IncomingNachaTopErrorDto> TopErrors,
    IReadOnlyList<IncomingNachaTimelinePointDto> Timeline);

/// <summary>Indicadores agregados de salud del pipeline inbound.</summary>
/// <param name="TotalIngestions">Cantidad de ingestas dentro de la ventana.</param>
/// <param name="TotalQueueItems">Cantidad de ítems de cola dentro de la ventana.</param>
/// <param name="BacklogItems">Ítems que no están Confirmed ni FailedFinal.</param>
/// <param name="BlockedItems">Ítems en estado Blocked.</param>
/// <param name="RetryPendingItems">Ítems en estado RetryPending.</param>
/// <param name="WaitingWindowItems">Ítems en estado WaitingWindow.</param>
/// <param name="FailedFinalItems">Ítems en estado FailedFinal.</param>
/// <param name="ConfirmedItems">Ítems en estado Confirmed.</param>
/// <param name="AverageQueueAgeMinutes">Edad promedio de los ítems de cola, en minutos.</param>
/// <param name="OldestQueueAgeMinutes">Mayor edad observada en cola, en minutos.</param>
public sealed record IncomingNachaPipelineHealthDto(
    int TotalIngestions,
    int TotalQueueItems,
    int BacklogItems,
    int BlockedItems,
    int RetryPendingItems,
    int WaitingWindowItems,
    int FailedFinalItems,
    int ConfirmedItems,
    double AverageQueueAgeMinutes,
    double OldestQueueAgeMinutes);

/// <summary>Conteo agregado identificado por una clave textual.</summary>
/// <param name="Key">Clave del grupo contabilizado.</param>
/// <param name="Count">Cantidad de elementos del grupo.</param>
public sealed record IncomingNachaKpiCountDto(string Key, int Count);

/// <summary>Conteos de cola agregados por cámara y ciclo.</summary>
/// <param name="ClearingHouseId">Identificador de cámara del grupo.</param>
/// <param name="AchCycleId">Identificador de ciclo ACH del grupo.</param>
/// <param name="TotalItems">Cantidad total de ítems del grupo.</param>
/// <param name="BlockedItems">Cantidad en estado Blocked.</param>
/// <param name="RetryPendingItems">Cantidad en estado RetryPending.</param>
/// <param name="WaitingWindowItems">Cantidad en estado WaitingWindow.</param>
/// <param name="FailedFinalItems">Cantidad en estado FailedFinal.</param>
/// <param name="ConfirmedItems">Cantidad en estado Confirmed.</param>
public sealed record IncomingNachaClearingCycleKpiDto(
    int ClearingHouseId,
    string AchCycleId,
    int TotalItems,
    int BlockedItems,
    int RetryPendingItems,
    int WaitingWindowItems,
    int FailedFinalItems,
    int ConfirmedItems);

/// <summary>Error de cola agregado por código.</summary>
/// <param name="ErrorCode">Código de error no vacío registrado en la cola.</param>
/// <param name="Count">Cantidad de ítems con el código dentro de la ventana.</param>
/// <param name="LastSeenAtUtc">Última fecha de intento registrada para el grupo, cuando existe.</param>
public sealed record IncomingNachaTopErrorDto(
    string ErrorCode,
    int Count,
    DateTime? LastSeenAtUtc);

/// <summary>Punto horario de la serie de eventos del command center inbound.</summary>
/// <param name="BucketAtUtc">Inicio UTC de la hora representada.</param>
/// <param name="TotalEvents">Cantidad total de eventos en la hora.</param>
/// <param name="ManualApplied">Eventos con estado Applied.</param>
/// <param name="ManualRejected">Eventos con estado Rejected.</param>
/// <param name="RetryPendingTransitions">Eventos cuyo mensaje contiene RetryPending.</param>
/// <param name="FailedFinalTransitions">Eventos cuyo mensaje contiene FailedFinal.</param>
public sealed record IncomingNachaTimelinePointDto(
    DateTime BucketAtUtc,
    int TotalEvents,
    int ManualApplied,
    int ManualRejected,
    int RetryPendingTransitions,
    int FailedFinalTransitions);
