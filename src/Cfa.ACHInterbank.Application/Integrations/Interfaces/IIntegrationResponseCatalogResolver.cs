using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationResponseCatalogResolver
{
    Task<IntegrationResponseCatalogResult> ResolveAsync(
        string source,
        string method,
        string? responseCode,
        DateTime processedAtUtc,
        CancellationToken ct = default);
}
