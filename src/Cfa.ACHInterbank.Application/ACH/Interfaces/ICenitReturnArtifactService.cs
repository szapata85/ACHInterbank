using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>I05: generación local explícita de una reserva CENIT ya aprobada, nunca transporte.</summary>
public interface ICenitReturnArtifactService
{
    Task<CenitReturnArtifactResult> GenerateAsync(Guid preparationId, string actorId,
        CancellationToken ct = default);
    Task<CenitReturnArtifactResult> ReadLocalAsync(Guid preparationId, CancellationToken ct = default);
    Task<CenitReturnArtifactMetadata> GetMetadataAsync(Guid preparationId, CancellationToken ct = default);
}
