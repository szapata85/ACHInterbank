using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;

namespace Cfa.ACHInterbank.Api.Mappers.AchResponses;

public sealed class NotificarRespuestaAchApiMapper
{
    public NotificarRespuestaAchCommand MapRequest(NotificarRespuestaAchRequest request)
        => new(request.NotificationAttemptId, request.CorrelationId);

    public NotificarRespuestaAchResponse MapResponse(NotificarRespuestaAchResult result)
        => new(result.Procesada, result.Encontrada, result.YaProcesada, result.ExisteError, result.ErrorTecnico, result.EstadoNotificacion?.ToString(), result.EstadoProcesamiento?.ToString(), result.CodigoError, result.DescripcionError, result.ErrorTecnicoDetalle, result.Motivo);
}
