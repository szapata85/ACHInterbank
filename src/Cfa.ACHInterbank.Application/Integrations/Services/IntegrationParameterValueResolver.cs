using System.Globalization;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Services;

public sealed class IntegrationParameterValueResolver : IIntegrationParameterValueResolver
{
    private readonly IIntegrationSourceProviderRegistry _sourceRegistry;
    private readonly IIntegrationTransformationRegistry _transformationRegistry;

    public IntegrationParameterValueResolver(
        IIntegrationSourceProviderRegistry sourceRegistry,
        IIntegrationTransformationRegistry transformationRegistry)
    {
        _sourceRegistry = sourceRegistry ?? throw new ArgumentNullException(nameof(sourceRegistry));
        _transformationRegistry = transformationRegistry ?? throw new ArgumentNullException(nameof(transformationRegistry));
    }

    public async ValueTask<object?> ResolveAsync(
        IntegrationExecutionParameterBinding binding,
        IntegrationExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(context);

        var provider = _sourceRegistry.GetRequired(
            binding.Source.SourceKind,
            binding.Source.SourceKey,
            context.MethodCode);

        var value = await provider.ResolveAsync(binding.Source, context, ct);
        var valueType = provider.Descriptor.DataType;

        if (value is null && binding.DefaultValue is not null)
        {
            value = binding.DefaultValue;
            valueType = binding.DataType;
        }

        if (!string.IsNullOrWhiteSpace(binding.TransformationCode))
        {
            var transformation = _transformationRegistry.GetRequired(binding.TransformationCode);
            ValidateTransformationInput(transformation.Descriptor, valueType, value, binding.FormatMask, binding.ParameterPath);
            value = transformation.Apply(
                value,
                new IntegrationTransformationContext(
                    context.MethodCode,
                    binding.ParameterPath,
                    binding.DataType,
                    binding.FormatMask));
            valueType = transformation.Descriptor.OutputType;
        }

        if (value is null)
        {
            if (binding.Required && !binding.Nillable)
            {
                throw new InvalidOperationException(
                    $"SOURCE_VALUE_REQUIRED: {binding.ParameterPath} no produjo valor y no admite null.");
            }

            return null;
        }

        ValidateTargetValue(binding, valueType, value);
        return value;
    }

    private static void ValidateTransformationInput(
        IntegrationTransformationDescriptor descriptor,
        string sourceDataType,
        object? value,
        string? formatMask,
        string parameterPath)
    {
        if (value is null && !descriptor.SupportsNull)
        {
            throw new InvalidOperationException(
                $"TRANSFORMATION_NULL_NOT_SUPPORTED: {descriptor.Code} no admite null para {parameterPath}.");
        }

        if (descriptor.RequiresMask && string.IsNullOrWhiteSpace(formatMask))
        {
            throw new InvalidOperationException(
                $"TRANSFORMATION_MASK_REQUIRED: {descriptor.Code} requiere FormatMask para {parameterPath}.");
        }

        if (value is null)
        {
            return;
        }

        if (!descriptor.InputTypes.Any(type => IntegrationTypeCompatibility.AreCompatible(sourceDataType, type)))
        {
            throw new InvalidOperationException(
                $"TRANSFORMATION_TYPE_INCOMPATIBLE: {descriptor.Code} no acepta {sourceDataType} para {parameterPath}.");
        }
    }

    private static void ValidateTargetValue(
        IntegrationExecutionParameterBinding binding,
        string declaredValueType,
        object value)
    {
        if (!IntegrationTypeCompatibility.CanRepresent(binding.DataType, value))
        {
            throw new InvalidOperationException(
                $"SOURCE_TYPE_INCOMPATIBLE: {binding.ParameterPath} requiere {binding.DataType} y recibió un valor no convertible desde {declaredValueType}.");
        }
    }

    private static class IntegrationTypeCompatibility
    {
        private static readonly HashSet<string> NumericTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "int", "integer", "int32", "long", "int64", "decimal", "double", "float", "single"
        };

        public static bool AreCompatible(string sourceType, string expectedType)
        {
            var source = Normalize(sourceType);
            var expected = Normalize(expectedType);
            if (expected == "object" || expected == "same" || source == expected)
            {
                return true;
            }

            if (NumericTypes.Contains(source) && NumericTypes.Contains(expected))
            {
                return true;
            }

            if (source == "string")
            {
                return true;
            }

            return source == "datetime" && expected == "string"
                || source == "collection" && expected == "string";
        }

        public static bool CanRepresent(string targetType, object value)
        {
            var target = Normalize(targetType);
            if (target is "string" or "object")
            {
                return true;
            }

            var text = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return target switch
            {
                "int" or "integer" or "int32" => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                "long" or "int64" => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                "decimal" => decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out _),
                "double" => double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _),
                "float" or "single" => float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _),
                "datetime" => value is DateTime or DateTimeOffset
                    || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _),
                "bool" or "boolean" => value is bool || bool.TryParse(text, out _),
                _ => throw new NotSupportedException($"DATA_TYPE_NOT_SUPPORTED: {targetType} no está soportado por el motor contract-driven.")
            };
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "string" : value.Trim().ToLowerInvariant();
    }
}
