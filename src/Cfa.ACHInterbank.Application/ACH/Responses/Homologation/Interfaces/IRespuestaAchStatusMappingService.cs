using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;

public interface IRespuestaAchStatusMappingService
{
    Task<HomologarRespuestaAchResult> HomologarAsync(
        HomologarRespuestaAchRequest request,
        CancellationToken cancellationToken = default);
}
