using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationTransformationRegistry
{
    IReadOnlyCollection<IntegrationTransformationDescriptor> Descriptors { get; }

    IIntegrationTransformation GetRequired(string code);
}
