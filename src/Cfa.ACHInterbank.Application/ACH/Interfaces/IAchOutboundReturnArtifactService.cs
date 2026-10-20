using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchOutboundReturnArtifactService
{
    Task<AchOutboundReturnArtifact> BuildAsync(string fileName, CancellationToken ct = default);
}
