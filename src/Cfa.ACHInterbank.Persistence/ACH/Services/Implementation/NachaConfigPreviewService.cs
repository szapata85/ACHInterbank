using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigPreviewService : INachaConfigPreviewService
{
    private readonly INachaConfigResolver _resolver;

    public NachaConfigPreviewService(INachaConfigResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<NachaConfigResolverPreviewResultDto> PreviewResolverAsync(NachaConfigResolverPreviewRequest request, CancellationToken ct = default)
    {
        var result = await _resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = request.CamaraCode,
            FlowTypeCode = request.FlujoCode,
            DirectionCode = request.DireccionCode,
            ServiceClassCode = request.ServicioCode,
            ProcessDateUtc = request.ProcessDateUtc,
            RecordCodes = request.RecordCodes
        }, ct);

        return new NachaConfigResolverPreviewResultDto
        {
            Success = result.Success,
            ProfileId = result.Profile?.Id,
            ProfileCode = result.Profile?.ProfileCode,
            LayoutByRecordCode = result.LayoutsByRecordCode.ToDictionary(x => x.Key, x => x.Value.VariantCode, StringComparer.OrdinalIgnoreCase),
            Trace = result.Trace,
            Warnings = result.Warnings
        };
    }
}
