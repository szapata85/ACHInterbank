using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigResolver
{
    Task<NachaConfigResolutionResult> ResolveAsync(NachaConfigResolutionRequest request, CancellationToken ct = default);
    Task<NachaConfigResolutionResult> ResolvePublishedOrdinaryAsync(NachaConfigResolutionRequest request, CancellationToken ct = default);
    Task<NachaConfigResolutionResult> ResolvePublishedInboundAsync(
        NachaConfigResolutionRequest request,
        IReadOnlyList<string> physicalRecords,
        CancellationToken ct = default);
}
