using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationSourceProviderRegistry
{
    IReadOnlyCollection<IntegrationSourceProviderDescriptor> Descriptors { get; }

    IIntegrationSourceValueProvider GetRequired(
        IntegrationSourceKindEnum sourceKind,
        string sourceKey);

    IIntegrationSourceValueProvider GetRequired(
        IntegrationSourceKindEnum sourceKind,
        string sourceKey,
        string methodCode);
}
