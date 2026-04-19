using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class ExpressionDslEngine : IExpressionDslCompiler, IExpressionDslExecutor
{
    private static readonly HashSet<string> SupportedOps =
    [
        "const", "prop", "ctx", "coalesce", "concat", "trim", "upper", "lower", "substring", "default"
    ];

    public CompiledExpressionDsl? Compile(string? expressionDslJson, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(expressionDslJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(expressionDslJson);
            var root = ParseNode(doc.RootElement, issues, depth: 0);
            return issues.Count == 0 && root is not null
                ? new CompiledExpressionDsl { Root = root }
                : null;
        }
        catch (Exception ex)
        {
            issues.Add($"DSL inválido: {ex.Message}");
            return null;
        }
    }

    public object? Evaluate(CompiledExpressionDsl expression, object sourceRecord, IReadOnlyDictionary<string, object?> contextValues)
        => Eval(expression.Root, sourceRecord, contextValues);

    private static ExpressionNode? ParseNode(JsonElement element, List<string> issues, int depth)
    {
        if (depth > 8)
        {
            issues.Add("DSL supera profundidad máxima permitida (8).");
            return null;
        }

        if (!element.TryGetProperty("op", out var opElement))
        {
            issues.Add("DSL requiere propiedad 'op'.");
            return null;
        }

        var op = opElement.GetString()?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(op) || !SupportedOps.Contains(op))
        {
            issues.Add($"Operación DSL no soportada: {op ?? "NULL"}.");
            return null;
        }

        var node = new ExpressionNode { Op = op, Path = element.TryGetProperty("path", out var path) ? path.GetString() : null, Key = element.TryGetProperty("key", out var key) ? key.GetString() : null, Value = element.TryGetProperty("value", out var value) ? ReadJsonValue(value) : null };
        if (element.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in args.EnumerateArray())
            {
                var parsed = ParseNode(child, issues, depth + 1);
                if (parsed is not null)
                {
                    node.Args.Add(parsed);
                }
            }
        }

        return node;
    }

    private static object? Eval(ExpressionNode node, object sourceRecord, IReadOnlyDictionary<string, object?> ctx)
    {
        return node.Op switch
        {
            "const" => node.Value,
            "prop" => ResolveProperty(sourceRecord, node.Path),
            "ctx" => ctx.TryGetValue(node.Key ?? string.Empty, out var value) ? value : null,
            "coalesce" => node.Args.Select(x => Eval(x, sourceRecord, ctx)).FirstOrDefault(x => !IsNullOrEmpty(x)),
            "default" => IsNullOrEmpty(Eval(node.Args[0], sourceRecord, ctx)) ? Eval(node.Args[1], sourceRecord, ctx) : Eval(node.Args[0], sourceRecord, ctx),
            "concat" => string.Concat(node.Args.Select(x => Eval(x, sourceRecord, ctx)?.ToString() ?? string.Empty)),
            "trim" => Eval(node.Args[0], sourceRecord, ctx)?.ToString()?.Trim(),
            "upper" => Eval(node.Args[0], sourceRecord, ctx)?.ToString()?.ToUpperInvariant(),
            "lower" => Eval(node.Args[0], sourceRecord, ctx)?.ToString()?.ToLowerInvariant(),
            "substring" => Substring(Eval(node.Args[0], sourceRecord, ctx)?.ToString(), node),
            _ => null
        };
    }

    private static string? Substring(string? value, ExpressionNode node)
    {
        if (value is null) return null;
        var start = Convert.ToInt32(node.Args.ElementAtOrDefault(1)?.Value ?? 0);
        var length = Convert.ToInt32(node.Args.ElementAtOrDefault(2)?.Value ?? value.Length);
        if (start < 0) start = 0;
        if (start >= value.Length) return string.Empty;
        if (length < 0) length = 0;
        if (start + length > value.Length) length = value.Length - start;
        return value.Substring(start, length);
    }

    private static object? ResolveProperty(object sourceRecord, string? path)
    {
        if (sourceRecord is null || string.IsNullOrWhiteSpace(path)) return null;
        var segment = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();
        var prop = sourceRecord.GetType().GetProperties().FirstOrDefault(x => string.Equals(x.Name, segment, StringComparison.OrdinalIgnoreCase));
        return prop?.GetValue(sourceRecord);
    }

    private static bool IsNullOrEmpty(object? value) => value is null || string.IsNullOrWhiteSpace(value.ToString());

    private static object? ReadJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetInt64(out var i) => i,
        JsonValueKind.Number when value.TryGetDecimal(out var d) => d,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => value.GetRawText()
    };
}
