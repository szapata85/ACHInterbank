using Cfa.ACHInterbank.Application.ACH.Responses.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;

public sealed record HomologarRespuestaAchRequest(
    string CodigoCamaraCompensacion,
    TipoRespuestaAch TipoRespuesta,
    string CodigoEstadoExterno,
    string? CodigoCausalExterna,
    DateTime FechaReferencia);
