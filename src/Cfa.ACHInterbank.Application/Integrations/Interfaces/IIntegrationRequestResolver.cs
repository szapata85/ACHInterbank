using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationRequestResolver
{
    Task<ResolvedIntegrationRequest> ResolveAsync(
        IntegrationExecutionContext context,
        CancellationToken ct = default);
}
