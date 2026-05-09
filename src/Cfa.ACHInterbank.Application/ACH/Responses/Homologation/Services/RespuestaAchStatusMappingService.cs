using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Services;

public sealed class RespuestaAchStatusMappingService : IRespuestaAchStatusMappingService
{
    private readonly IAchResponseStatusMappingRepository _repository;

    public RespuestaAchStatusMappingService(IAchResponseStatusMappingRepository repository)
    {
        _repository = repository;
    }

    public async Task<HomologarRespuestaAchResult> HomologarAsync(HomologarRespuestaAchRequest request, CancellationToken cancellationToken = default)
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
            return HomologarRespuestaAchResult.NotFound("No existe homologación activa/vigente para la combinación consultada.");

        if (causalExterna is null)
        {
            var stateOnly = effective
                .Where(x => string.IsNullOrWhiteSpace(x.CodigoCausalExterna))
                .Where(x => !x.RequiereCausal)
                .OrderByDescending(x => x.FechaInicioVigencia)
                .FirstOrDefault();

            if (stateOnly is not null)
                return ToResult(stateOnly);

            if (effective.Any(x => x.RequiereCausal))
                return HomologarRespuestaAchResult.NotFound("La homologación requiere causal externa y no fue suministrada.");

            return HomologarRespuestaAchResult.NotFound("No existe homologación sin causal para el estado consultado.");
        }

        var exact = effective
            .Where(x => string.Equals(NormalizeNullable(x.CodigoCausalExterna), causalExterna, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.FechaInicioVigencia)
            .FirstOrDefault();

        if (exact is not null)
            return ToResult(exact);

        var fallback = effective
            .Where(x => string.IsNullOrWhiteSpace(x.CodigoCausalExterna))
            .Where(x => !x.RequiereCausal)
            .OrderByDescending(x => x.FechaInicioVigencia)
            .FirstOrDefault();

        if (fallback is not null)
            return ToResult(fallback);

        return HomologarRespuestaAchResult.NotFound("No existe homologación para la causal externa suministrada.");
    }

    private static HomologarRespuestaAchResult ToResult(AchResponseStatusMappingModel mapping)
    {
        return mapping.PermiteNotificacion
            ? HomologarRespuestaAchResult.Success(
                true,
                mapping.IdEstadoInterno,
                mapping.IdEstadoServicioExterno,
                mapping.EstadoInternoNombre,
                mapping.CausalNormalizada,
                mapping.DescripcionCausalNormalizada)
            : HomologarRespuestaAchResult.NotAllowed(
                mapping.IdEstadoInterno,
                mapping.IdEstadoServicioExterno,
                mapping.EstadoInternoNombre,
                mapping.CausalNormalizada,
                mapping.DescripcionCausalNormalizada,
                "La homologación existe, pero no permite notificación.");
    }

    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
