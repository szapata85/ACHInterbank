using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;

public interface IAchResponseStatusMappingRepository
{
    Task<IReadOnlyList<AchResponseStatusMappingModel>> FindCandidatesAsync(
        string codigoCamaraCompensacion,
        TipoRespuestaAch tipoRespuesta,
        string codigoEstadoExterno,
        CancellationToken cancellationToken = default);
}
