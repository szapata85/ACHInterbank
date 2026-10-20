using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Queries.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Api.Mappers.AchResponses;

public sealed class AchResponseQueryApiMapper
{
    public AchResponseSearchQuery MapSearchRequest(AchResponseSearchRequest request)
        => new(request.FechaDesde, request.FechaHasta, request.TipoRespuesta, request.IdTransaccion, request.CodigoCamaraCompensacion, request.CodigoEntidadOrigen, request.CodigoEntidadDestino, request.CodigoEstadoExterno, request.EstadoProcesamiento, request.CorrelationId, request.PageNumber, request.PageSize);

    public PagedResponse<AchResponseListItemResponse> MapPagedResult(PagedResult<AchResponseListItemModel> result)
        => new(result.Items.Select(MapListItem).ToList(), result.PageNumber, result.PageSize, result.TotalCount);

    public AchResponseDashboardQuery MapDashboardRequest(AchResponseDashboardRequest request)
        => new(request.FechaDesde, request.FechaHasta, ParseTipoRespuestaOrNull(request.TipoRespuesta));

    public AchResponseDashboardResponse MapDashboard(AchResponseDashboardModel model)
        => new(
            model.TotalRespuestas,
            model.Recibidas,
            model.Homologadas,
            model.Notificadas,
            model.NoHomologadas,
            model.RevisionManual,
            model.PendientesReintento,
            model.ErroresFuncionales,
            model.Duplicadas);

    public AchResponseDetailResponse MapDetail(AchResponseDetailModel model)
        => new(model.Id, model.TipoRespuesta, model.IdTransaccion, model.CodigoCamaraCompensacion, model.CodigoEntidadOrigen, model.CodigoEntidadDestino, model.CodigoEstadoExterno, model.CodigoCausalExterna, model.IdEstadoInterno, model.IdEstadoServicioExterno, model.EstadoInternoNombre, model.CausalNormalizada, model.DescripcionCausal, model.IdTransaccionServicioExterno, model.HashIdempotencia, model.EstadoProcesamiento, model.MotivoNoHomologacion, model.PermiteNotificacion, model.CorrelationId, model.FechaRecepcion, model.FechaCreacion, model.FechaActualizacion, model.NotificationAttempts.Select(MapAttempt).ToList(), model.ClearingHouseId, model.AppliedMappingId, model.DuplicateReceiptCount, model.Version);

    public AchResponseNotificationAttemptResponse MapAttempt(AchResponseNotificationAttemptModel model)
        => new(model.Id, model.AchResponseId, model.NumeroIntento, model.EstadoNotificacion, model.IdCanal, model.NombreCanal, model.IdTransaccion, model.IdEstado, model.Causal, model.IdTransaccionServicioExterno, model.DescripcionCausal, model.ExisteError, model.CodigoError, model.DescripcionError, model.ErrorTecnico, model.FechaCreacion, model.FechaEnvio);

    public AchResponseStatusMappingResponse MapStatusMapping(AchResponseStatusMappingListItemModel model)
        => new(model.Id, model.CodigoCamaraCompensacion, model.TipoRespuesta, model.CodigoEstadoExterno, model.CodigoCausalExterna, model.IdEstadoInterno, model.IdEstadoServicioExterno, model.EstadoInternoNombre, model.CausalNormalizada, model.DescripcionCausalNormalizada, model.RequiereCausal, model.PermiteNotificacion, model.Activo, model.FechaInicioVigencia, model.FechaFinVigencia, model.ClearingHouseId, model.Priority, model.Version);

    public TipoRespuestaAch? ParseTipoRespuestaOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToLowerInvariant() switch
        {
            "prenota" => TipoRespuestaAch.Prenota,
            "transaccion" => TipoRespuestaAch.Transaccion,
            _ => null
        };
    }

    private static AchResponseListItemResponse MapListItem(AchResponseListItemModel model)
        => new(model.Id, model.TipoRespuesta, model.IdTransaccion, model.CodigoCamaraCompensacion, model.CodigoEntidadOrigen, model.CodigoEntidadDestino, model.CodigoEstadoExterno, model.CodigoCausalExterna, model.EstadoInternoNombre, model.EstadoProcesamiento, model.PermiteNotificacion, model.CorrelationId, model.FechaRecepcion, model.FechaCreacion);
}
