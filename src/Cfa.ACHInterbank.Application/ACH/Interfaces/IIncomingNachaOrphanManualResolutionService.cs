using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaOrphanManualResolutionService
{
    Task<IncomingNachaOrphanManualResolutionResult> ResolveAsync(IncomingNachaOrphanManualResolutionRequest request, CancellationToken ct = default);
}
