using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Queries.Models;

public sealed record AchResponseSearchQuery(
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

public sealed record AchResponseDashboardQuery(
    DateTime? FechaDesde,
    DateTime? FechaHasta,
    TipoRespuestaAch? TipoRespuesta);

public sealed record AchResponseDashboardModel(
    int TotalRespuestas,
    int Recibidas,
    int Homologadas,
    int Notificadas,
    int NoHomologadas,
    int RevisionManual,
    int PendientesReintento,
    int ErroresFuncionales,
    int Duplicadas);

public sealed record AchResponseListItemModel(
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

public sealed record AchResponseNotificationAttemptModel(
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

public sealed record AchResponseDetailModel(
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
    IReadOnlyList<AchResponseNotificationAttemptModel> NotificationAttempts);

public sealed record AchResponseStatusMappingListItemModel(
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

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
