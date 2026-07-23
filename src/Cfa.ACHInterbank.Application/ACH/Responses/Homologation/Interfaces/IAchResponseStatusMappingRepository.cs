using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Queries.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;

public interface IAchResponseStatusMappingRepository
{
    Task<int?> ResolveClearingHouseIdAsync(string codigoCamaraCompensacion, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AchResponseStatusMappingModel>> FindCandidatesAsync(
        string codigoCamaraCompensacion,
        TipoRespuestaAch tipoRespuesta,
        string codigoEstadoExterno,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AchResponseStatusMappingListItemModel>> ListAsync(string? codigoCamaraCompensacion = null, TipoRespuestaAch? tipoRespuesta = null, bool? activo = null, CancellationToken cancellationToken = default);
}
