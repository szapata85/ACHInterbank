using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using System.Collections.Concurrent;
using System.Reflection;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFixedWidthRecordRenderer : INachaFixedWidthRecordRenderer
{
    private static readonly ConcurrentDictionary<string, string> NormalizedIdentifierCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string[]> IdentifierCandidatesCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, PropertyResolutionCache> PropertyCacheByType = new();
    private static readonly ConcurrentDictionary<(Type Type, string Identifier), PropertyLookupResult> PropertyLookupCache = new();

    public Task<string> RenderRecordAsync<T>(string recordType, T entity, NachaRecordLayout layout)
    {
        var fields = layout.Fields.OrderBy(f => f.StartPosition).ToList();
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');

        if (!string.IsNullOrEmpty(layout.RecordCode))
            buffer[0] = layout.RecordCode[0];

        var entityType = entity?.GetType() ?? typeof(T);

        foreach (var field in fields)
        {
            object? raw;
            if (TryResolveConstant(field.DbColumn, out raw))
            {
                string constantValue = FormatValue(raw, field);

                if (constantValue.Length > field.Length)
                    constantValue = constantValue.Substring(0, field.Length);

                constantValue = field.Justification == 'R'
                    ? constantValue.PadLeft(field.Length, field.PadChar)
                    : constantValue.PadRight(field.Length, field.PadChar);

                int constantStart = field.StartPosition - 1;
                constantValue.CopyTo(0, buffer, constantStart, constantValue.Length);
                continue;
            }

            var prop = ResolveProperty(entityType, field.DbColumn)
                ?? ResolveProperty(entityType, field.FieldName);
            if (prop == null) continue;

            raw = prop.GetValue(entity);
            string value = FormatValue(raw, field);

            if (recordType == "5" && string.Equals(field.FieldName, "SettlementDate", StringComparison.OrdinalIgnoreCase))
            {
                value = NormalizeBatchSettlementDate(value);
            }

            if (value.Length > field.Length)
                value = value.Substring(0, field.Length);

            value = field.Justification == 'R'
                ? value.PadLeft(field.Length, field.PadChar)
                : value.PadRight(field.Length, field.PadChar);

            int start = field.StartPosition - 1;
            value.CopyTo(0, buffer, start, value.Length);
        }

        return Task.FromResult(new string(buffer));
    }

    public Task<string> RenderRecordAsync(string recordType, IReadOnlyDictionary<string, object?> values, NachaRecordLayout layout)
    {
        var fields = layout.Fields.OrderBy(f => f.StartPosition).ToList();
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');

        if (!string.IsNullOrEmpty(layout.RecordCode))
            buffer[0] = layout.RecordCode[0];

        foreach (var field in fields)
        {
            object? raw;
            if (!TryResolveConstant(field.DbColumn, out raw) &&
                !TryResolveValue(values, field.DbColumn, out raw) &&
                !TryResolveValue(values, field.FieldName, out raw))
            {
                continue;
            }

            var value = FormatValue(raw, field);

            if (recordType == "5" && string.Equals(field.FieldName, "SettlementDate", StringComparison.OrdinalIgnoreCase))
            {
                value = NormalizeBatchSettlementDate(value);
            }

            if (value.Length > field.Length)
                value = value.Substring(0, field.Length);

            value = field.Justification == 'R'
                ? value.PadLeft(field.Length, field.PadChar)
                : value.PadRight(field.Length, field.PadChar);

            int start = field.StartPosition - 1;
            value.CopyTo(0, buffer, start, value.Length);
        }

        return Task.FromResult(new string(buffer));
    }

    private static string NormalizeBatchSettlementDate(string? value)
    {
        var validation = BatchHeaderType5JulianDateValidator.ValidateAndFormat(value);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Error Fatal 65 en Fecha de Compensación Juliana.");
        }

        return validation.FormattedValue;
    }

    private static PropertyInfo? ResolveProperty(Type type, string? dbColumn)
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

    private static bool TryResolveConstant(string? dbColumn, out object? raw)
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

    private static bool TryResolveValue(IReadOnlyDictionary<string, object?> values, string? dbColumn, out object? raw)
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

    private static string NormalizeIdentifier(string value)
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

    private static string FormatValue(object? raw, NachaRecordField field)
    {
        if (raw == null) return string.Empty;

        return raw switch
        {
            DateTime dt => dt.ToString(field.Format ?? "yyyyMMdd"),
            decimal d => ((long)(d * 100)).ToString(),
            bool b => b ? "1" : "0",
            _ => raw.ToString() ?? string.Empty
        };
    }

    private sealed record PropertyResolutionCache(IReadOnlyDictionary<string, PropertyInfo> NormalizedProperties);
    private readonly record struct PropertyLookupResult(bool Found, PropertyInfo? Property)
    {
        public static PropertyLookupResult NotFound => new(false, null);
        public static PropertyLookupResult From(PropertyInfo property) => new(true, property);
    }
}
