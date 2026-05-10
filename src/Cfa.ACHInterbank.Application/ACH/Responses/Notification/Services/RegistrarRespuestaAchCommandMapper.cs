using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Notification.Services;

public sealed class RegistrarRespuestaAchCommandMapper : IRegistrarRespuestaAchCommandMapper
{
    public RegistrarRespuestaAchCommand Map(AchResponse response, AchResponseNotificationAttempt attempt)
        => new(
            TipoRespuesta: response.TipoRespuesta,
            IdTransaccion: attempt.IdTransaccion,
            IdCanal: attempt.IdCanal,
            NombreCanal: attempt.NombreCanal,
            IdEstado: attempt.IdEstado,
            Causal: attempt.Causal,
            IdTransaccionServicioExterno: attempt.IdTransaccionServicioExterno,
            DescripcionCausal: attempt.DescripcionCausal,
            CodigoCamaraCompensacion: response.CodigoCamaraCompensacion,
            CodigoEntidadOrigen: response.CodigoEntidadOrigen,
            CodigoEntidadDestino: response.CodigoEntidadDestino,
            CorrelationId: response.CorrelationId);
}
