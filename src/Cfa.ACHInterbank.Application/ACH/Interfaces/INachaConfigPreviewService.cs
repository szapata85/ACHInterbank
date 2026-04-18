using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigPreviewService
{
    Task<NachaConfigResolverPreviewResultDto> PreviewResolverAsync(NachaConfigResolverPreviewRequest request, CancellationToken ct = default);
}
