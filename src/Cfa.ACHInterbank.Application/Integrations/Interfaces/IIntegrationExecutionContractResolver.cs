using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationExecutionContractResolver
{
    Task<IntegrationExecutionContract> ResolveActiveAsync(
        string methodCode,
        CancellationToken ct = default);
}
