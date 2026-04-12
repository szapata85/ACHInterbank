using Cfa.ACHInterbank.Application.Integrations.Dtos;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationCatalogService
{
    Task<IReadOnlyCollection<IntegrationMethodDto>> GetMethodsAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<IntegrationMethodParameterDto>> GetMethodParametersAsync(int methodId, CancellationToken ct = default);
    Task<IReadOnlyCollection<IntegrationSourceCatalogFieldDto>> GetSourceCatalogAsync(int? methodId, CancellationToken ct = default);
    Task<IReadOnlyCollection<IntegrationTransformationCatalogDto>> GetTransformationsAsync(CancellationToken ct = default);
}
