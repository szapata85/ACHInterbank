using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Registries;

public sealed class IntegrationTransformationRegistry : IIntegrationTransformationRegistry
{
    private readonly IReadOnlyDictionary<string, IIntegrationTransformation> _transformations;

    public IntegrationTransformationRegistry(IEnumerable<IIntegrationTransformation> transformations)
    {
        ArgumentNullException.ThrowIfNull(transformations);

        var map = new Dictionary<string, IIntegrationTransformation>(StringComparer.OrdinalIgnoreCase);
        foreach (var transformation in transformations)
        {
            if (transformation is null)
            {
                throw new ArgumentException("El registro no admite transformations nulas.", nameof(transformations));
            }

            var descriptor = transformation.Descriptor
                ?? throw new InvalidOperationException($"{transformation.GetType().Name} no expone descriptor.");
            if (!map.TryAdd(descriptor.Code, transformation))
            {
                throw new InvalidOperationException(
                    $"Existe más de una transformation registrada con código {descriptor.Code}.");
            }
        }

        _transformations = map;
        Descriptors = map.Values
            .Select(transformation => transformation.Descriptor)
            .OrderBy(descriptor => descriptor.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyCollection<IntegrationTransformationDescriptor> Descriptors { get; }

    public IIntegrationTransformation GetRequired(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("El código de transformación es obligatorio.", nameof(code));
        }

        var normalized = code.Trim();
        return _transformations.TryGetValue(normalized, out var transformation)
            ? transformation
            : throw new KeyNotFoundException($"No existe transformation registrada con código {normalized}.");
    }
}
