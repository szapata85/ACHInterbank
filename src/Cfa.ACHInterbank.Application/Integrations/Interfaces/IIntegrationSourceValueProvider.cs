using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationSourceValueProvider
{
    IntegrationSourceProviderDescriptor Descriptor { get; }

    ValueTask<object?> ResolveAsync(
        IntegrationSourceReference source,
        IntegrationExecutionContext context,
        CancellationToken ct = default);
}
