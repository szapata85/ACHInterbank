using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Registries;

public sealed class IntegrationSourceProviderRegistry : IIntegrationSourceProviderRegistry
{
    private readonly IReadOnlyDictionary<SourceProviderKey, IIntegrationSourceValueProvider> _providers;

    public IntegrationSourceProviderRegistry(IEnumerable<IIntegrationSourceValueProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var map = new Dictionary<SourceProviderKey, IIntegrationSourceValueProvider>();
        foreach (var provider in providers)
        {
            if (provider is null)
            {
                throw new ArgumentException("El registro no admite providers nulos.", nameof(providers));
            }

            var descriptor = provider.Descriptor
                ?? throw new InvalidOperationException($"{provider.GetType().Name} no expone descriptor.");
            var key = new SourceProviderKey(descriptor.SourceKind, NormalizeKey(descriptor.SourceKey));
            if (!map.TryAdd(key, provider))
            {
                throw new InvalidOperationException(
                    $"Existe más de un source provider para {descriptor.SourceKind}:{descriptor.SourceKey}.");
            }
        }

        _providers = map;
        Descriptors = map.Values
            .Select(provider => provider.Descriptor)
            .OrderBy(descriptor => descriptor.SortOrder)
            .ThenBy(descriptor => descriptor.SourceKind)
            .ThenBy(descriptor => descriptor.SourceKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyCollection<IntegrationSourceProviderDescriptor> Descriptors { get; }

    public IIntegrationSourceValueProvider GetRequired(
        IntegrationSourceKindEnum sourceKind,
        string sourceKey)
    {
        if (sourceKind == IntegrationSourceKindEnum.Expression)
        {
            throw new NotSupportedException("El motor contract-driven no admite fuentes basadas en expresiones arbitrarias.");
        }

        var normalizedKey = NormalizeKey(sourceKey);
        return _providers.TryGetValue(new SourceProviderKey(sourceKind, normalizedKey), out var provider)
            ? provider
            : throw new KeyNotFoundException($"No existe source provider para {sourceKind}:{sourceKey}.");
    }

    public IIntegrationSourceValueProvider GetRequired(
        IntegrationSourceKindEnum sourceKind,
        string sourceKey,
        string methodCode)
    {
        if (string.IsNullOrWhiteSpace(methodCode))
        {
            throw new ArgumentException("MethodCode es obligatorio.", nameof(methodCode));
        }

        var provider = GetRequired(sourceKind, sourceKey);
        if (!provider.Descriptor.SupportsMethod(methodCode))
        {
            throw new InvalidOperationException(
                $"SOURCE_METHOD_INCOMPATIBLE: {sourceKind}:{sourceKey} no está habilitado para {methodCode.Trim()}.");
        }

        return provider;
    }

    private static string NormalizeKey(string value)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("SourceKey es obligatorio.", nameof(value))
            : value.Trim().ToUpperInvariant();

    private readonly record struct SourceProviderKey(
        IntegrationSourceKindEnum SourceKind,
        string SourceKey);
}
