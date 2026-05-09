using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

public sealed record ProcesarRespuestaAchCommand(
    TipoRespuestaAch TipoRespuesta,
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
