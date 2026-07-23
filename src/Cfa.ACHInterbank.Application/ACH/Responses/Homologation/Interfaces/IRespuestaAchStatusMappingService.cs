using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;

public interface IRespuestaAchStatusMappingService
{
    Task<int?> ResolveClearingHouseIdAsync(string codigoCamaraCompensacion, CancellationToken cancellationToken = default);

    Task<HomologarRespuestaAchResult> HomologarAsync(
        HomologarRespuestaAchRequest request,
        CancellationToken cancellationToken = default);
}
