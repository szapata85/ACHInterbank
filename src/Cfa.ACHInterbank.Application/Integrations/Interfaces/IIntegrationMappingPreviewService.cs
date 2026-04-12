using Cfa.ACHInterbank.Application.Integrations.Dtos;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationMappingPreviewService
{
    Task<IntegrationMappingPreviewResultDto> PreviewAsync(Guid mappingSetId, PreviewIntegrationMappingSetRequest request, CancellationToken ct = default);
}
