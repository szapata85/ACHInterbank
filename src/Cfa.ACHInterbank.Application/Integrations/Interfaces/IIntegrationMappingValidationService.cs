using Cfa.ACHInterbank.Application.Integrations.Dtos;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationMappingValidationService
{
    Task<IntegrationMappingValidationResultDto> ValidateAsync(Guid mappingSetId, bool includeWarnings = true, CancellationToken ct = default);
}
