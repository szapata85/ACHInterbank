using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;

public sealed record HomologarRespuestaAchRequest(
    string CodigoCamaraCompensacion,
    TipoRespuestaAch TipoRespuesta,
    string CodigoEstadoExterno,
    string? CodigoCausalExterna,
    DateTime FechaReferencia);
