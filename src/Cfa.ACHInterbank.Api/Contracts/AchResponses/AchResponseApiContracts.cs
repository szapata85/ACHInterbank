namespace Cfa.ACHInterbank.Api.Contracts.AchResponses;

/// <summary>Solicitud pública para procesar una respuesta ACH recibida.</summary>
/// <param name="TipoRespuesta">Tipo de respuesta. El validador admite Prenota o Transaccion sin distinguir mayúsculas y minúsculas.</param>
/// <param name="IdTransaccion">Identificador de la transacción informado en la respuesta; es obligatorio.</param>
/// <param name="CodigoCamaraCompensacion">Código de la cámara compensadora asociado a la respuesta; es obligatorio.</param>
/// <param name="CodigoEntidadOrigen">Código opcional de la entidad de origen.</param>
/// <param name="CodigoEntidadDestino">Código opcional de la entidad de destino.</param>
/// <param name="CodigoEstadoExterno">Código de estado recibido desde el origen externo; es obligatorio.</param>
/// <param name="CodigoCausalExterna">Código de causal externa cuando fue informado.</param>
/// <param name="DescripcionCausalExterna">Descripción opcional de la causal externa.</param>
/// <param name="IdCanal">Identificador del canal; el validador requiere un valor mayor que cero.</param>
/// <param name="NombreCanal">Nombre del canal; es obligatorio.</param>
/// <param name="IdTransaccionServicioExterno">Identificador de transacción del servicio externo; el validador requiere un valor mayor que cero.</param>
/// <param name="FechaRecepcion">Fecha de recepción opcional; si se informa no puede ser DateTime.MinValue.</param>
/// <param name="CorrelationId">Identificador de correlación opcional transportado dentro del contrato de procesamiento.</param>
public sealed record ProcesarRespuestaAchRequest(
    string TipoRespuesta,
    string IdTransaccion,
    string CodigoCamaraCompensacion,
    string? CodigoEntidadOrigen,
    string? CodigoEntidadDestino,
    string CodigoEstadoExterno,
    string? CodigoCausalExterna,
    string? DescripcionCausalExterna,
    int IdCanal,
    string NombreCanal,
    int IdTransaccionServicioExterno,
    DateTime? FechaRecepcion,
    string? CorrelationId);

/// <summary>Resultado público del procesamiento de una respuesta ACH.</summary>
/// <param name="AchResponseId">Identificador persistido de la respuesta cuando el procesamiento produjo uno.</param>
/// <param name="Procesada">Indica si el caso de uso considera procesada la respuesta; determina 200 frente a 422 en el controller.</param>
/// <param name="Duplicada">Indica si el procesamiento detectó una recepción duplicada.</param>
/// <param name="ExisteHomologacion">Indica si se encontró una homologación aplicable.</param>
/// <param name="PermiteNotificacion">Indica si el resultado habilita el flujo de notificación.</param>
/// <param name="IntentoPendienteCreado">Indica si se creó un intento de notificación pendiente.</param>
/// <param name="EstadoProcesamiento">Nombre del estado de procesamiento devuelto por el caso de uso.</param>
/// <param name="Motivo">Motivo funcional opcional asociado al resultado.</param>
/// <param name="HashIdempotencia">Hash de idempotencia calculado por el procesamiento cuando está disponible.</param>
public sealed record ProcesarRespuestaAchResponse(
    Guid? AchResponseId,
    bool Procesada,
    bool Duplicada,
    bool ExisteHomologacion,
    bool PermiteNotificacion,
    bool IntentoPendienteCreado,
    string EstadoProcesamiento,
    string? Motivo,
    string? HashIdempotencia);

/// <summary>Solicitud pública para procesar un intento de notificación ya existente.</summary>
/// <param name="NotificationAttemptId">Identificador del intento de notificación; debe ser mayor que cero.</param>
/// <param name="CorrelationId">Identificador de correlación opcional transportado en el body.</param>
public sealed record NotificarRespuestaAchRequest(long NotificationAttemptId, string? CorrelationId);

/// <summary>Resultado público del procesamiento de un intento de notificación ACH.</summary>
/// <param name="Procesada">Indica si el caso de uso procesó el intento.</param>
/// <param name="Encontrada">Indica si el intento solicitado fue encontrado; el controller responde 404 cuando es false.</param>
/// <param name="YaProcesada">Indica si el intento ya se encontraba procesado.</param>
/// <param name="ExisteError">Indica si el resultado registra un error funcional.</param>
/// <param name="ErrorTecnico">Indica si el resultado registra un error técnico.</param>
/// <param name="EstadoNotificacion">Estado de notificación resultante cuando está disponible.</param>
/// <param name="EstadoProcesamiento">Estado de procesamiento de la respuesta asociado al intento cuando está disponible.</param>
/// <param name="CodigoError">Código funcional de error cuando existe.</param>
/// <param name="DescripcionError">Descripción funcional del error cuando existe.</param>
/// <param name="ErrorTecnicoDetalle">Detalle técnico registrado por el caso de uso cuando existe.</param>
/// <param name="Motivo">Motivo adicional del resultado cuando fue informado.</param>
public sealed record NotificarRespuestaAchResponse(
    bool Procesada,
    bool Encontrada,
    bool YaProcesada,
    bool ExisteError,
    bool ErrorTecnico,
    string? EstadoNotificacion,
    string? EstadoProcesamiento,
    string? CodigoError,
    string? DescripcionError,
    string? ErrorTecnicoDetalle,
    string? Motivo);

/// <summary>Filtros y paginación de la consulta de respuestas ACH.</summary>
/// <param name="FechaDesde">Fecha mínima de recepción incluida en la consulta.</param>
/// <param name="FechaHasta">Fecha máxima de recepción incluida en la consulta.</param>
/// <param name="TipoRespuesta">Filtro textual opcional por tipo de respuesta persistido.</param>
/// <param name="IdTransaccion">Filtro opcional por identificador de transacción.</param>
/// <param name="CodigoCamaraCompensacion">Filtro opcional por código de cámara.</param>
/// <param name="CodigoEntidadOrigen">Filtro opcional por entidad de origen.</param>
/// <param name="CodigoEntidadDestino">Filtro opcional por entidad de destino.</param>
/// <param name="CodigoEstadoExterno">Filtro opcional por código de estado externo.</param>
/// <param name="EstadoProcesamiento">Filtro textual opcional por estado de procesamiento persistido.</param>
/// <param name="CorrelationId">Filtro opcional por identificador de correlación.</param>
/// <param name="PageNumber">Número de página solicitado; valores menores o iguales que cero se normalizan a 1.</param>
/// <param name="PageSize">Tamaño de página solicitado; valores menores o iguales que cero usan 10 y valores mayores a 100 se limitan a 100.</param>
public sealed record AchResponseSearchRequest(
    DateTime? FechaDesde,
    DateTime? FechaHasta,
    string? TipoRespuesta,
    string? IdTransaccion,
    string? CodigoCamaraCompensacion,
    string? CodigoEntidadOrigen,
    string? CodigoEntidadDestino,
    string? CodigoEstadoExterno,
    string? EstadoProcesamiento,
    string? CorrelationId,
    int PageNumber,
    int PageSize);

/// <summary>Filtros de la consulta agregada de respuestas ACH.</summary>
/// <param name="FechaDesde">Fecha mínima de recepción incluida en el resumen.</param>
/// <param name="FechaHasta">Fecha máxima de recepción incluida en el resumen.</param>
/// <param name="TipoRespuesta">Tipo opcional; el controller acepta Prenota o Transaccion sin distinguir mayúsculas y minúsculas.</param>
public sealed record AchResponseDashboardRequest(
    DateTime? FechaDesde,
    DateTime? FechaHasta,
    string? TipoRespuesta);

/// <summary>Contadores agregados de respuestas ACH para los filtros solicitados.</summary>
/// <param name="TotalRespuestas">Cantidad total de respuestas incluidas.</param>
/// <param name="Recibidas">Cantidad en estado Recibida.</param>
/// <param name="Homologadas">Cantidad en estado Homologada.</param>
/// <param name="Notificadas">Cantidad en estado Notificada.</param>
/// <param name="NoHomologadas">Cantidad en estado NoHomologada.</param>
/// <param name="RevisionManual">Cantidad en estado RequiereRevisionManual.</param>
/// <param name="PendientesReintento">Cantidad en estado PendienteReintento.</param>
/// <param name="ErroresFuncionales">Cantidad en estado ErrorFuncional.</param>
/// <param name="Duplicadas">Cantidad en estado Duplicada.</param>
public sealed record AchResponseDashboardResponse(
    int TotalRespuestas,
    int Recibidas,
    int Homologadas,
    int Notificadas,
    int NoHomologadas,
    int RevisionManual,
    int PendientesReintento,
    int ErroresFuncionales,
    int Duplicadas);

/// <summary>Elemento de la lista paginada de respuestas ACH.</summary>
/// <param name="Id">Identificador persistido de la respuesta.</param>
/// <param name="TipoRespuesta">Tipo de respuesta persistido.</param>
/// <param name="IdTransaccion">Identificador de la transacción asociado.</param>
/// <param name="CodigoCamaraCompensacion">Código de cámara asociado.</param>
/// <param name="CodigoEntidadOrigen">Código de entidad de origen cuando fue informado.</param>
/// <param name="CodigoEntidadDestino">Código de entidad de destino cuando fue informado.</param>
/// <param name="CodigoEstadoExterno">Código de estado externo recibido.</param>
/// <param name="CodigoCausalExterna">Código de causal externa cuando fue informado.</param>
/// <param name="EstadoInternoNombre">Nombre del estado interno homologado cuando existe.</param>
/// <param name="EstadoProcesamiento">Estado actual de procesamiento de la respuesta.</param>
/// <param name="PermiteNotificacion">Indica si la respuesta permite notificación según el resultado persistido.</param>
/// <param name="CorrelationId">Identificador de correlación persistido cuando existe.</param>
/// <param name="FechaRecepcion">Fecha de recepción persistida.</param>
/// <param name="FechaCreacion">Fecha de creación persistida.</param>
public sealed record AchResponseListItemResponse(
    Guid Id,
    string TipoRespuesta,
    string IdTransaccion,
    string CodigoCamaraCompensacion,
    string? CodigoEntidadOrigen,
    string? CodigoEntidadDestino,
    string CodigoEstadoExterno,
    string? CodigoCausalExterna,
    string? EstadoInternoNombre,
    string EstadoProcesamiento,
    bool PermiteNotificacion,
    string? CorrelationId,
    DateTime FechaRecepcion,
    DateTime FechaCreacion);

/// <summary>Página de resultados de una consulta pública de respuestas ACH.</summary>
/// <typeparam name="T">Tipo de elemento incluido en la página.</typeparam>
/// <param name="Items">Elementos de la página actual.</param>
/// <param name="PageNumber">Número de página efectivo después de la normalización del repositorio.</param>
/// <param name="PageSize">Tamaño de página efectivo después de la normalización del repositorio.</param>
/// <param name="TotalCount">Cantidad total de elementos que cumplen los filtros.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    /// <summary>Cantidad total de páginas calculada a partir de TotalCount y PageSize; devuelve cero cuando PageSize no es positivo.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>Detalle público persistido de una respuesta ACH.</summary>
/// <param name="Id">Identificador persistido de la respuesta.</param>
/// <param name="TipoRespuesta">Tipo de respuesta persistido.</param>
/// <param name="IdTransaccion">Identificador de la transacción asociado.</param>
/// <param name="CodigoCamaraCompensacion">Código de cámara asociado.</param>
/// <param name="CodigoEntidadOrigen">Código de entidad de origen cuando fue informado.</param>
/// <param name="CodigoEntidadDestino">Código de entidad de destino cuando fue informado.</param>
/// <param name="CodigoEstadoExterno">Código de estado externo recibido.</param>
/// <param name="CodigoCausalExterna">Código de causal externa cuando fue informado.</param>
/// <param name="IdEstadoInterno">Identificador del estado interno homologado cuando existe.</param>
/// <param name="IdEstadoServicioExterno">Identificador del estado del servicio externo homologado cuando existe.</param>
/// <param name="EstadoInternoNombre">Nombre del estado interno homologado cuando existe.</param>
/// <param name="CausalNormalizada">Causal normalizada cuando existe homologación.</param>
/// <param name="DescripcionCausal">Descripción de causal normalizada cuando existe.</param>
/// <param name="IdTransaccionServicioExterno">Identificador de transacción del servicio externo.</param>
/// <param name="HashIdempotencia">Hash de idempotencia persistido para la respuesta.</param>
/// <param name="EstadoProcesamiento">Estado actual de procesamiento.</param>
/// <param name="MotivoNoHomologacion">Motivo de no homologación cuando fue registrado.</param>
/// <param name="PermiteNotificacion">Indica si la respuesta permite notificación.</param>
/// <param name="CorrelationId">Identificador de correlación persistido cuando existe.</param>
/// <param name="FechaRecepcion">Fecha de recepción persistida.</param>
/// <param name="FechaCreacion">Fecha de creación persistida.</param>
/// <param name="FechaActualizacion">Fecha de última actualización cuando existe.</param>
/// <param name="NotificationAttempts">Intentos de notificación asociados, ordenados por número de intento.</param>
/// <param name="ClearingHouseId">Identificador interno de cámara asociado al registro.</param>
/// <param name="AppliedMappingId">Identificador del mapping aplicado cuando existe.</param>
/// <param name="DuplicateReceiptCount">Cantidad persistida de recepciones duplicadas.</param>
/// <param name="Version">Versión de concurrencia persistida de la respuesta.</param>
public sealed record AchResponseDetailResponse(
    Guid Id,
    string TipoRespuesta,
    string IdTransaccion,
    string CodigoCamaraCompensacion,
    string? CodigoEntidadOrigen,
    string? CodigoEntidadDestino,
    string CodigoEstadoExterno,
    string? CodigoCausalExterna,
    int? IdEstadoInterno,
    int? IdEstadoServicioExterno,
    string? EstadoInternoNombre,
    string? CausalNormalizada,
    string? DescripcionCausal,
    int IdTransaccionServicioExterno,
    string HashIdempotencia,
    string EstadoProcesamiento,
    string? MotivoNoHomologacion,
    bool PermiteNotificacion,
    string? CorrelationId,
    DateTime FechaRecepcion,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion,
    IReadOnlyList<AchResponseNotificationAttemptResponse> NotificationAttempts,
    int ClearingHouseId,
    int? AppliedMappingId,
    int DuplicateReceiptCount,
    Guid Version);

/// <summary>Intento de notificación expuesto por las consultas de respuestas ACH.</summary>
/// <param name="Id">Identificador persistido del intento.</param>
/// <param name="AchResponseId">Identificador de la respuesta ACH asociada.</param>
/// <param name="NumeroIntento">Número secuencial del intento dentro de la respuesta.</param>
/// <param name="EstadoNotificacion">Estado persistido del intento de notificación.</param>
/// <param name="IdCanal">Identificador del canal usado por el intento.</param>
/// <param name="NombreCanal">Nombre del canal usado por el intento.</param>
/// <param name="IdTransaccion">Identificador de transacción informado al canal.</param>
/// <param name="IdEstado">Identificador de estado informado al canal.</param>
/// <param name="Causal">Causal enviada al canal cuando existe.</param>
/// <param name="IdTransaccionServicioExterno">Identificador de transacción del servicio externo.</param>
/// <param name="DescripcionCausal">Descripción de causal enviada cuando existe.</param>
/// <param name="ExisteError">Indicador de error funcional informado por el intento cuando existe.</param>
/// <param name="CodigoError">Código de error funcional cuando existe.</param>
/// <param name="DescripcionError">Descripción de error funcional cuando existe.</param>
/// <param name="ErrorTecnico">Detalle técnico persistido cuando existe.</param>
/// <param name="FechaCreacion">Fecha de creación persistida del intento.</param>
/// <param name="FechaEnvio">Fecha de envío cuando el intento fue enviado.</param>
public sealed record AchResponseNotificationAttemptResponse(
    long Id,
    Guid AchResponseId,
    int NumeroIntento,
    string EstadoNotificacion,
    int IdCanal,
    string NombreCanal,
    string IdTransaccion,
    int IdEstado,
    string? Causal,
    int IdTransaccionServicioExterno,
    string? DescripcionCausal,
    bool? ExisteError,
    string? CodigoError,
    string? DescripcionError,
    string? ErrorTecnico,
    DateTime FechaCreacion,
    DateTime? FechaEnvio);

/// <summary>Mapping de estado de respuesta ACH expuesto por el catálogo de mappings.</summary>
/// <param name="Id">Identificador persistido del mapping.</param>
/// <param name="CodigoCamaraCompensacion">Código de cámara asociado.</param>
/// <param name="TipoRespuesta">Tipo de respuesta al que aplica el mapping.</param>
/// <param name="CodigoEstadoExterno">Código de estado externo mapeado.</param>
/// <param name="CodigoCausalExterna">Código de causal externa cuando forma parte del mapping.</param>
/// <param name="IdEstadoInterno">Identificador del estado interno configurado.</param>
/// <param name="IdEstadoServicioExterno">Identificador del estado de servicio externo configurado.</param>
/// <param name="EstadoInternoNombre">Nombre del estado interno configurado.</param>
/// <param name="CausalNormalizada">Causal normalizada configurada cuando existe.</param>
/// <param name="DescripcionCausalNormalizada">Descripción normalizada configurada cuando existe.</param>
/// <param name="RequiereCausal">Indica si el mapping requiere causal externa.</param>
/// <param name="PermiteNotificacion">Indica si el mapping permite notificación.</param>
/// <param name="Activo">Indica si el mapping está activo.</param>
/// <param name="FechaInicioVigencia">Fecha de inicio de vigencia configurada.</param>
/// <param name="FechaFinVigencia">Fecha de fin de vigencia cuando existe.</param>
/// <param name="ClearingHouseId">Identificador interno de cámara asociado.</param>
/// <param name="Priority">Prioridad configurada del mapping.</param>
/// <param name="Version">Versión de concurrencia persistida del mapping.</param>
public sealed record AchResponseStatusMappingResponse(
    int Id,
    string CodigoCamaraCompensacion,
    string TipoRespuesta,
    string CodigoEstadoExterno,
    string? CodigoCausalExterna,
    int IdEstadoInterno,
    int IdEstadoServicioExterno,
    string EstadoInternoNombre,
    string? CausalNormalizada,
    string? DescripcionCausalNormalizada,
    bool RequiereCausal,
    bool PermiteNotificacion,
    bool Activo,
    DateTime FechaInicioVigencia,
    DateTime? FechaFinVigencia,
    int ClearingHouseId,
    int Priority,
    Guid Version);

public sealed record AchResponseStatusMappingRequest(
    string CodigoCamaraCompensacion,
    string TipoRespuesta,
    string CodigoEstadoExterno,
    string? CodigoCausalExterna,
    int IdEstadoInterno,
    int IdEstadoServicioExterno,
    string EstadoInternoNombre,
    string? CausalNormalizada,
    string? DescripcionCausalNormalizada,
    bool RequiereCausal,
    bool PermiteNotificacion,
    bool Activo,
    DateTime FechaInicioVigencia,
    DateTime? FechaFinVigencia);
