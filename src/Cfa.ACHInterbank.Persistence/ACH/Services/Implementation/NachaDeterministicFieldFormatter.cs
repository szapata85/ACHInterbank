using System.Collections.Concurrent;
using System.Reflection;
using Cfa.ACHInterbank.Application.Helpers.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Deterministic, IO-free field resolution and legacy formatting primitives used by NACHA generation.
/// This component deliberately contains no clearing-house policy and no persistence concerns.
/// </summary>
internal static class NachaDeterministicFieldFormatter
{
    private static readonly ConcurrentDictionary<string, string> NormalizedIdentifierCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string[]> IdentifierCandidatesCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, PropertyResolutionCache> PropertyCacheByType = new();
    private static readonly ConcurrentDictionary<(Type Type, string Identifier), PropertyLookupResult> PropertyLookupCache = new();

    internal static string NormalizeBatchSettlementDate(string? value)
    {
        var validation = BatchHeaderType5JulianDateValidator.ValidateAndFormat(value);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Error Fatal 65 en Fecha de Compensación Juliana.");
        }

        return validation.FormattedValue;
    }

    internal static PropertyInfo? ResolveProperty(Type type, string? dbColumn)
    {
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return null;
        }

        var normalizedIdentifier = dbColumn.Trim();
        var lookupResult = PropertyLookupCache.GetOrAdd(
            (type, normalizedIdentifier),
            static key => ResolvePropertyUncached(key.Type, key.Identifier));

        return lookupResult.Found ? lookupResult.Property : null;
    }

    internal static bool TryResolveConstant(string? dbColumn, out object? raw)
    {
        raw = null;
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return false;
        }

        if (!dbColumn.StartsWith("CONST:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        raw = dbColumn[6..];
        return true;
    }

    internal static bool TryResolveValue(IReadOnlyDictionary<string, object?> values, string? dbColumn, out object? raw)
    {
        raw = null;
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return false;
        }

        foreach (var candidate in EnumerateIdentifierCandidates(dbColumn))
        {
            if (values.TryGetValue(candidate, out raw))
            {
                return true;
            }

            var exactIgnoreCase = values.FirstOrDefault(kv => string.Equals(kv.Key, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(exactIgnoreCase.Key))
            {
                raw = exactIgnoreCase.Value;
                return true;
            }

            var normalizedTarget = NormalizeIdentifier(candidate);
            var normalizedMatch = values.FirstOrDefault(kv => NormalizeIdentifier(kv.Key) == normalizedTarget);
            if (!string.IsNullOrEmpty(normalizedMatch.Key))
            {
                raw = normalizedMatch.Value;
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return NormalizedIdentifierCache.GetOrAdd(value, static raw =>
        {
            var chars = new char[raw.Length];
            var index = 0;

            foreach (var ch in raw)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    chars[index++] = char.ToUpperInvariant(ch);
                }
            }

            return index == 0 ? string.Empty : new string(chars, 0, index);
        });
    }

    internal static string FormatLegacyValue(object? raw, NachaRecordField field)
    {
        if (raw == null)
        {
            return string.Empty;
        }

        return raw switch
        {
            DateTime dt => dt.ToString(field.Format ?? "yyyyMMdd"),
            decimal d => ((long)(d * 100)).ToString(),
            bool b => b ? "1" : "0",
            _ => raw.ToString() ?? string.Empty
        };
    }

    private static PropertyLookupResult ResolvePropertyUncached(Type type, string identifier)
    {
        var typeCache = PropertyCacheByType.GetOrAdd(type, static t =>
        {
            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var normalizedMap = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            foreach (var property in properties)
            {
                var normalizedPropertyName = NormalizeIdentifier(property.Name);
                if (!normalizedMap.ContainsKey(normalizedPropertyName))
                {
                    normalizedMap[normalizedPropertyName] = property;
                }
            }

            return new PropertyResolutionCache(normalizedMap);
        });

        foreach (var candidate in EnumerateIdentifierCandidates(identifier))
        {
            var property = type.GetProperty(candidate,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);
            if (property is not null)
            {
                return PropertyLookupResult.From(property);
            }

            var normalizedTarget = NormalizeIdentifier(candidate);
            if (typeCache.NormalizedProperties.TryGetValue(normalizedTarget, out property))
            {
                return PropertyLookupResult.From(property);
            }
        }

        return PropertyLookupResult.NotFound;
    }

    private static IEnumerable<string> EnumerateIdentifierCandidates(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var candidate in IdentifierCandidatesCache.GetOrAdd(value, static raw =>
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                return Array.Empty<string>();
            }

            var separators = new[] { '.', ':', '/' };
            var segments = trimmed.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length > 1)
            {
                return new[] { trimmed, segments[^1] };
            }

            return new[] { trimmed };
        }))
        {
            yield return candidate;
        }
    }

    private sealed record PropertyResolutionCache(IReadOnlyDictionary<string, PropertyInfo> NormalizedProperties);

    private readonly record struct PropertyLookupResult(bool Found, PropertyInfo? Property)
    {
        internal static PropertyLookupResult NotFound => new(false, null);
        internal static PropertyLookupResult From(PropertyInfo property) => new(true, property);
    }
}
