namespace Cfa.ACHInterbank.Api.Contracts.AchResponses;

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

public sealed record NotificarRespuestaAchRequest(long NotificationAttemptId, string? CorrelationId);

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

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

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
    IReadOnlyList<AchResponseNotificationAttemptResponse> NotificationAttempts);

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
    DateTime? FechaFinVigencia);

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
