using System.Text.Json;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class FieldValidationEngine : IFieldValidationEngine
{
    public Task<FieldValidationResult> ValidateAsync(FieldRuntimePlan fieldPlan, object? value, CancellationToken ct = default)
    {
        var result = new FieldValidationResult();
        var text = value?.ToString();

        foreach (var rule in fieldPlan.Rules.Where(x => x.IsEnabled))
        {
            switch (rule.RuleTypeCode.ToUpperInvariant())
            {
                case "REQUIRED":
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        result.Issues.Add(new FieldValidationIssue { RuleCode = rule.RuleCode, Severity = rule.Severity, Message = "Valor requerido." });
                    }
                    break;
                case "REGEX":
                    if (!string.IsNullOrWhiteSpace(text) && !CheckRegex(rule.RuleConfigJson, text))
                    {
                        result.Issues.Add(new FieldValidationIssue { RuleCode = rule.RuleCode, Severity = rule.Severity, Message = "Valor no cumple patrón." });
                    }
                    break;
                case "ENUM":
                    if (!string.IsNullOrWhiteSpace(text) && !CheckEnum(rule.RuleConfigJson, text))
                    {
                        result.Issues.Add(new FieldValidationIssue { RuleCode = rule.RuleCode, Severity = rule.Severity, Message = "Valor fuera de catálogo permitido." });
                    }
                    break;
                case "RANGE":
                    if (!string.IsNullOrWhiteSpace(text) && !CheckRange(rule.RuleConfigJson, text))
                    {
                        result.Issues.Add(new FieldValidationIssue { RuleCode = rule.RuleCode, Severity = rule.Severity, Message = "Valor fuera de rango." });
                    }
                    break;
                case "DATE_FORMAT":
                    if (!string.IsNullOrWhiteSpace(text) && !CheckDateFormat(rule.RuleConfigJson, text))
                    {
                        result.Issues.Add(new FieldValidationIssue { RuleCode = rule.RuleCode, Severity = rule.Severity, Message = "Formato de fecha inválido." });
                    }
                    break;
            }
        }

        return Task.FromResult(result);
    }

    private static bool CheckRegex(string? config, string value)
    {
        if (string.IsNullOrWhiteSpace(config)) return true;
        using var doc = JsonDocument.Parse(config);
        var pattern = doc.RootElement.TryGetProperty("pattern", out var p) ? p.GetString() : null;
        return string.IsNullOrWhiteSpace(pattern) || Regex.IsMatch(value, pattern);
    }

    private static bool CheckEnum(string? config, string value)
    {
        if (string.IsNullOrWhiteSpace(config)) return true;
        using var doc = JsonDocument.Parse(config);
        var allowed = doc.RootElement.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        return allowed.Count == 0 || allowed.Contains(value);
    }

    private static bool CheckRange(string? config, string value)
    {
        if (string.IsNullOrWhiteSpace(config)) return true;
        if (!decimal.TryParse(value, out var parsed)) return false;
        using var doc = JsonDocument.Parse(config);
        var min = doc.RootElement.TryGetProperty("min", out var minElement) ? minElement.GetDecimal() : decimal.MinValue;
        var max = doc.RootElement.TryGetProperty("max", out var maxElement) ? maxElement.GetDecimal() : decimal.MaxValue;
        return parsed >= min && parsed <= max;
    }

    private static bool CheckDateFormat(string? config, string value)
    {
        if (string.IsNullOrWhiteSpace(config)) return true;
        using var doc = JsonDocument.Parse(config);
        var format = doc.RootElement.TryGetProperty("format", out var formatElement) ? formatElement.GetString() : "yyyyMMdd";
        return DateTime.TryParseExact(value, format, null, System.Globalization.DateTimeStyles.None, out _);
    }
}
