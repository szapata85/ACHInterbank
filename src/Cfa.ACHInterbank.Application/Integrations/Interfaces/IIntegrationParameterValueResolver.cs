using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationParameterValueResolver
{
    ValueTask<object?> ResolveAsync(
        IntegrationExecutionParameterBinding binding,
        IntegrationExecutionContext context,
        CancellationToken ct = default);
}
