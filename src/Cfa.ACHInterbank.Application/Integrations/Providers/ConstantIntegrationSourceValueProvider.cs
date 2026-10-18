using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Providers;

public sealed class ConstantIntegrationSourceValueProvider : IIntegrationSourceValueProvider
{
    public ConstantIntegrationSourceValueProvider(IEnumerable<string>? supportedMethodCodes = null)
    {
        Descriptor = new IntegrationSourceProviderDescriptor(
            IntegrationSourceKindEnum.Constant,
            "constant.value",
            "Valor fijo",
            "Valor fijo aprobado en la regla de mapping.",
            "string",
            true,
            "Configuración",
            0,
            IntegrationSensitiveClassification.None,
            supportedMethodCodes);
    }

    public IntegrationSourceProviderDescriptor Descriptor { get; }

    public ValueTask<object?> ResolveAsync(
        IntegrationSourceReference source,
        IntegrationExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        ct.ThrowIfCancellationRequested();

        if (source.SourceKind != IntegrationSourceKindEnum.Constant
            || !string.Equals(source.SourceKey, Descriptor.SourceKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SOURCE_PROVIDER_MISMATCH: {Descriptor.SourceKind}:{Descriptor.SourceKey} no puede resolver {source.SourceKind}:{source.SourceKey}.");
        }

        return ValueTask.FromResult<object?>(source.ConstantValue);
    }
}
