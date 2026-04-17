using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaCycleResolver
{
    Task<IncomingNachaCycleResolutionResult> ResolveAsync(IncomingNachaCycleResolutionRequest request, CancellationToken ct = default);
}
