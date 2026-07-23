using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Services;

public sealed class RespuestaAchStatusMappingService : IRespuestaAchStatusMappingService
{
    private readonly IAchResponseStatusMappingRepository _repository;

    public RespuestaAchStatusMappingService(IAchResponseStatusMappingRepository repository) => _repository = repository;

    public Task<int?> ResolveClearingHouseIdAsync(string codigoCamaraCompensacion, CancellationToken cancellationToken = default)
        => _repository.ResolveClearingHouseIdAsync(Normalize(codigoCamaraCompensacion), cancellationToken);

    public async Task<HomologarRespuestaAchResult> HomologarAsync(
        HomologarRespuestaAchRequest request, CancellationToken cancellationToken = default)
    {
        var camara = Normalize(request.CodigoCamaraCompensacion);
        var estadoExterno = Normalize(request.CodigoEstadoExterno);
        var causalExterna = NormalizeNullable(request.CodigoCausalExterna);
        var candidates = await _repository.FindCandidatesAsync(camara, request.TipoRespuesta, estadoExterno, cancellationToken);
        var effective = candidates
            .Where(x => x.Activo)
            .Where(x => x.FechaInicioVigencia <= request.FechaReferencia)
            .Where(x => !x.FechaFinVigencia.HasValue || x.FechaFinVigencia.Value >= request.FechaReferencia)
            .ToList();

        if (effective.Count == 0)
            return HomologarRespuestaAchResult.NotFound("No existe homologación activa y vigente para la combinación consultada.");

        IReadOnlyList<AchResponseStatusMappingModel> matches;
        if (causalExterna is null)
        {
            matches = effective.Where(x => string.IsNullOrWhiteSpace(x.CodigoCausalExterna) && !x.RequiereCausal).ToList();
            if (matches.Count == 0 && effective.Any(x => x.RequiereCausal))
                return HomologarRespuestaAchResult.NotFound("La homologación requiere causal externa y no fue suministrada.");
        }
        else
        {
            var exact = effective
                .Where(x => string.Equals(NormalizeNullable(x.CodigoCausalExterna), causalExterna, StringComparison.Ordinal))
                .ToList();
            matches = exact.Count > 0
                ? exact
                : effective.Where(x => string.IsNullOrWhiteSpace(x.CodigoCausalExterna) && !x.RequiereCausal).ToList();
        }

        if (matches.Count == 0)
            return HomologarRespuestaAchResult.NotFound("No existe homologación para el código externo suministrado.");

        var highestPriority = matches.Max(x => x.Priority);
        var winners = matches.Where(x => x.Priority == highestPriority).ToList();
        if (winners.Count != 1)
            return HomologarRespuestaAchResult.Ambiguous("Existen múltiples mappings vigentes con la mayor prioridad.");

        return ToResult(winners[0]);
    }

    private static HomologarRespuestaAchResult ToResult(AchResponseStatusMappingModel mapping)
        => mapping.PermiteNotificacion
            ? HomologarRespuestaAchResult.Success(true, mapping.IdEstadoInterno, mapping.IdEstadoServicioExterno,
                mapping.EstadoInternoNombre, mapping.CausalNormalizada, mapping.DescripcionCausalNormalizada, mapping.Id)
            : HomologarRespuestaAchResult.NotAllowed(mapping.IdEstadoInterno, mapping.IdEstadoServicioExterno,
                mapping.EstadoInternoNombre, mapping.CausalNormalizada, mapping.DescripcionCausalNormalizada,
                "La homologación existe, pero no permite notificación.", mapping.Id);

    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
