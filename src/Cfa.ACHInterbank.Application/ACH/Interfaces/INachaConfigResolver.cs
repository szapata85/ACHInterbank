using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigResolver
{
    Task<NachaConfigResolutionResult> ResolveAsync(NachaConfigResolutionRequest request, CancellationToken ct = default);
}
