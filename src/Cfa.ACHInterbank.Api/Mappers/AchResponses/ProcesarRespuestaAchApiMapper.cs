using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Api.Mappers.AchResponses;

public sealed class ProcesarRespuestaAchApiMapper
{
    public ProcesarRespuestaAchCommand MapRequest(ProcesarRespuestaAchRequest request)
    {
        return new ProcesarRespuestaAchCommand(
            ParseTipoRespuesta(request.TipoRespuesta),
            request.IdTransaccion,
            request.CodigoCamaraCompensacion,
            request.CodigoEntidadOrigen,
            request.CodigoEntidadDestino,
            request.CodigoEstadoExterno,
            request.CodigoCausalExterna,
            request.DescripcionCausalExterna,
            request.IdCanal,
            request.NombreCanal,
            request.IdTransaccionServicioExterno,
            request.FechaRecepcion,
            request.CorrelationId);
    }

    public ProcesarRespuestaAchResponse MapResponse(ProcesarRespuestaAchResult result)
        => new(result.AchResponseId, result.Procesada, result.Duplicada, result.ExisteHomologacion, result.PermiteNotificacion, result.IntentoPendienteCreado, result.EstadoProcesamiento.ToString(), result.Motivo, result.HashIdempotencia);

    private static TipoRespuestaAch ParseTipoRespuesta(string tipoRespuesta)
        => tipoRespuesta?.Trim().ToLowerInvariant() switch
        {
            "prenota" => TipoRespuestaAch.Prenota,
            "transaccion" => TipoRespuestaAch.Transaccion,
            _ => throw new ArgumentException("TipoRespuesta inválido. Valores permitidos: Prenota, Transaccion.")
        };
}
