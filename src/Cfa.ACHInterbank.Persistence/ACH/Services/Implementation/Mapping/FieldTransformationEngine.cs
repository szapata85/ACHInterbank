using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class FieldTransformationEngine : IFieldTransformationEngine
{
    public Task<TransformationResult> ApplyAsync(FieldRuntimePlan fieldPlan, object? value, CancellationToken ct = default)
    {
        var applied = new List<string>();
        if (string.IsNullOrWhiteSpace(fieldPlan.TransformationPipelineJson))
        {
            return Task.FromResult(new TransformationResult { Value = value });
        }

        try
        {
            using var doc = JsonDocument.Parse(fieldPlan.TransformationPipelineJson);
            var steps = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().ToList()
                : doc.RootElement.TryGetProperty("steps", out var arr) && arr.ValueKind == JsonValueKind.Array
                    ? arr.EnumerateArray().ToList()
                    : [];

            object? current = value;
            foreach (var step in steps)
            {
                var type = step.GetProperty("type").GetString()?.Trim().ToLowerInvariant();
                current = ApplyStep(type, current, step);
                applied.Add(type ?? "unknown");
            }

            return Task.FromResult(new TransformationResult { Value = current, AppliedSteps = applied });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TransformationResult { Value = value, AppliedSteps = applied, ErrorCode = $"TRANSFORM_ERROR:{ex.Message}" });
        }
    }

    private static object? ApplyStep(string? type, object? value, JsonElement step)
    {
        var text = value?.ToString();
        return type switch
        {
            "trim" => text?.Trim(),
            "upper" => text?.ToUpperInvariant(),
            "lower" => text?.ToLowerInvariant(),
            "truncate" => Truncate(text, step),
            "substring" => Substring(text, step),
            "remove_non_digits" => text is null ? null : new string(text.Where(char.IsDigit).ToArray()),
            "replace" => Replace(text, step),
            "null_to_default" => string.IsNullOrWhiteSpace(text) ? step.GetProperty("value").GetString() : text,
            _ => value
        };
    }

    private static string? Truncate(string? value, JsonElement step)
    {
        if (value is null) return null;
        var length = step.TryGetProperty("length", out var len) ? len.GetInt32() : value.Length;
        if (length < 0) return string.Empty;
        return value.Length <= length ? value : value[..length];
    }

    private static string? Substring(string? value, JsonElement step)
    {
        if (value is null) return null;
        var start = step.TryGetProperty("start", out var startElement) ? startElement.GetInt32() : 0;
        var length = step.TryGetProperty("length", out var lenElement) ? lenElement.GetInt32() : value.Length;
        if (start < 0) start = 0;
        if (start >= value.Length) return string.Empty;
        if (start + length > value.Length) length = value.Length - start;
        return value.Substring(start, length);
    }

    private static string? Replace(string? value, JsonElement step)
    {
        if (value is null) return null;
        var from = step.TryGetProperty("from", out var fromElement) ? fromElement.GetString() : string.Empty;
        var to = step.TryGetProperty("to", out var toElement) ? toElement.GetString() : string.Empty;
        return value.Replace(from ?? string.Empty, to ?? string.Empty, StringComparison.Ordinal);
    }
}
