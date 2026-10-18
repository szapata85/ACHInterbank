using System.Collections;
using System.Globalization;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Transformations;

public sealed class TrimIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = DescriptorFactory.String("Trim", false, true, "Recortar", "Elimina espacios al inicio y final.");
    public object? Apply(object? input, IntegrationTransformationContext context) => input is null ? null : Text(input).Trim();
    private static string Text(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
}

public sealed class UppercaseIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = DescriptorFactory.String("Uppercase", false, true, "Mayúsculas", "Convierte texto a mayúsculas invariantes.");
    public object? Apply(object? input, IntegrationTransformationContext context) => input is null ? null : Text(input).ToUpperInvariant();
    private static string Text(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
}

public sealed class LowercaseIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = DescriptorFactory.String("Lowercase", false, true, "Minúsculas", "Convierte texto a minúsculas invariantes.");
    public object? Apply(object? input, IntegrationTransformationContext context) => input is null ? null : Text(input).ToLowerInvariant();
    private static string Text(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
}

public sealed class PadLeftIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = DescriptorFactory.String("PadLeft", true, true, "Rellenar izquierda", "Rellena con ceros a la izquierda hasta la longitud indicada.");

    public object? Apply(object? input, IntegrationTransformationContext context)
    {
        if (input is null) return null;
        var width = TransformationMaskParser.RequiredPositiveInt(context.FormatMask, Descriptor.Code);
        return Text(input).PadLeft(width, '0');
    }

    private static string Text(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
}

public sealed class PadRightIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = DescriptorFactory.String("PadRight", true, true, "Rellenar derecha", "Rellena con ceros a la derecha hasta la longitud indicada.");

    public object? Apply(object? input, IntegrationTransformationContext context)
    {
        if (input is null) return null;
        var width = TransformationMaskParser.RequiredPositiveInt(context.FormatMask, Descriptor.Code);
        return Text(input).PadRight(width, '0');
    }

    private static string Text(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
}

public sealed class SubstringIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = DescriptorFactory.String("Substring", true, true, "Subcadena", "Extrae una subcadena con máscara inicio:longitud.");

    public object? Apply(object? input, IntegrationTransformationContext context)
    {
        if (input is null) return null;
        var text = Convert.ToString(input, CultureInfo.InvariantCulture) ?? string.Empty;
        var (start, length) = TransformationMaskParser.RequiredRange(context.FormatMask, Descriptor.Code);
        if (start > text.Length || (length.HasValue && start + length.Value > text.Length))
        {
            throw new InvalidOperationException($"TRANSFORMATION_MASK_OUT_OF_RANGE: {Descriptor.Code} excede la longitud del valor.");
        }

        return length.HasValue ? text.Substring(start, length.Value) : text[start..];
    }
}

public sealed class ConcatIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = new(
        "Concat",
        ["collection", "string"],
        "string",
        false,
        true,
        "Concatenar",
        "Concatena una colección usando FormatMask como separador opcional.");

    public object? Apply(object? input, IntegrationTransformationContext context)
    {
        if (input is null) return null;
        if (input is string text) return text;
        if (input is not IEnumerable values)
        {
            throw new InvalidOperationException("TRANSFORMATION_VALUE_INVALID: Concat requiere texto o colección.");
        }

        var separator = context.FormatMask ?? string.Empty;
        var materialized = values.Cast<object?>()
            .Where(value => value is not null)
            .Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        return string.Join(separator, materialized);
    }
}

public sealed class DateFormatIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = new(
        "DateFormat",
        ["datetime", "string"],
        "string",
        true,
        true,
        "Formatear fecha",
        "Formatea una fecha con una máscara .NET aprobada.");

    public object? Apply(object? input, IntegrationTransformationContext context)
    {
        if (input is null) return null;
        var mask = TransformationMaskParser.Required(context.FormatMask, Descriptor.Code);
        var value = input switch
        {
            DateTime dateTime => dateTime,
            DateTimeOffset offset => offset.UtcDateTime,
            string text when TryParseDate(text, out var parsed) => parsed,
            _ => throw new InvalidOperationException("TRANSFORMATION_VALUE_INVALID: DateFormat requiere una fecha válida.")
        };
        return value.ToString(mask, CultureInfo.InvariantCulture);
    }

    private static bool TryParseDate(string text, out DateTime value)
        => DateTime.TryParseExact(
               text,
               ["yyyyMMdd", "yyyy-MM-dd", "O"],
               CultureInfo.InvariantCulture,
               DateTimeStyles.RoundtripKind,
               out value)
           || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
}

public sealed class NumericFormatIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = new(
        "NumericFormat",
        ["int", "long", "decimal", "double", "float", "string"],
        "string",
        true,
        true,
        "Formatear número",
        "Formatea un número con una máscara numérica invariante.");

    public object? Apply(object? input, IntegrationTransformationContext context)
    {
        if (input is null) return null;
        var mask = TransformationMaskParser.Required(context.FormatMask, Descriptor.Code);
        var number = input switch
        {
            byte value => (decimal)value,
            short value => value,
            int value => value,
            long value => value,
            float value => (decimal)value,
            double value => (decimal)value,
            decimal value => value,
            string text when decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => throw new InvalidOperationException("TRANSFORMATION_VALUE_INVALID: NumericFormat requiere un número válido.")
        };
        return number.ToString(mask, CultureInfo.InvariantCulture);
    }
}

public sealed class NullIfEmptyIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = DescriptorFactory.String("NullIfEmpty", false, true, "Null si vacío", "Devuelve null cuando el texto está vacío o contiene solo espacios.");
    public object? Apply(object? input, IntegrationTransformationContext context)
    {
        if (input is null) return null;
        var text = Convert.ToString(input, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

public sealed class DefaultIfNullIntegrationTransformation : IIntegrationTransformation
{
    public IntegrationTransformationDescriptor Descriptor { get; } = new(
        "DefaultIfNull",
        ["string", "int", "long", "decimal", "double", "float", "datetime"],
        "same",
        true,
        true,
        "Default si null",
        "Usa FormatMask como valor por defecto cuando la entrada es null.");

    public object? Apply(object? input, IntegrationTransformationContext context)
        => input ?? TransformationMaskParser.Required(context.FormatMask, Descriptor.Code);
}

internal static class DescriptorFactory
{
    public static IntegrationTransformationDescriptor String(
        string code,
        bool requiresMask,
        bool supportsNull,
        string displayName,
        string description)
        => new(code, ["string"], "string", requiresMask, supportsNull, displayName, description);
}

internal static class TransformationMaskParser
{
    public static string Required(string? mask, string code)
        => string.IsNullOrWhiteSpace(mask)
            ? throw new InvalidOperationException($"TRANSFORMATION_MASK_REQUIRED: {code} requiere FormatMask.")
            : mask;

    public static int RequiredPositiveInt(string? mask, string code)
    {
        var raw = Required(mask, code);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new InvalidOperationException($"TRANSFORMATION_MASK_INVALID: {code} requiere una longitud entera positiva.");
    }

    public static (int Start, int? Length) RequiredRange(string? mask, string code)
    {
        var raw = Required(mask, code);
        var parts = raw.Split(':', StringSplitOptions.None);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var start)
            || start < 0)
        {
            throw new InvalidOperationException($"TRANSFORMATION_MASK_INVALID: {code} requiere máscara inicio:longitud.");
        }

        if (string.IsNullOrWhiteSpace(parts[1])) return (start, null);
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) || length < 0)
        {
            throw new InvalidOperationException($"TRANSFORMATION_MASK_INVALID: {code} requiere longitud no negativa.");
        }

        return (start, length);
    }
}
