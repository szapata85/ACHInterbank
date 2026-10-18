using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationTransformation
{
    IntegrationTransformationDescriptor Descriptor { get; }

    object? Apply(object? input, IntegrationTransformationContext context);
}
