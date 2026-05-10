using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
namespace Cfa.ACHInterbank.Application.ACH.Responses.Models;

public sealed record RegistrarRespuestaAchCommand(
    TipoRespuestaAch TipoRespuesta,
    string IdTransaccion,
    int IdCanal,
    string NombreCanal,
    int IdEstado,
    string? Causal,
    int IdTransaccionServicioExterno,
    string? DescripcionCausal,
    string? CodigoCamaraCompensacion,
    string? CodigoEntidadOrigen,
    string? CodigoEntidadDestino,
    string? CorrelationId);
