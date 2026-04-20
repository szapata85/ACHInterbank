using System.Collections.Concurrent;
using System.Linq.Expressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class FieldSourceResolver : IFieldSourceResolver
{
    private readonly IExpressionDslCompiler _dslCompiler;
    private readonly IExpressionDslExecutor _dslExecutor;
    private readonly INachaCanonicalMapper _canonicalMapper;
    private static readonly ConcurrentDictionary<(Type Type, string Path), Func<object, object?>> AccessorCache = new();

    public FieldSourceResolver(IExpressionDslCompiler dslCompiler, IExpressionDslExecutor dslExecutor, INachaCanonicalMapper canonicalMapper)
    {
        _dslCompiler = dslCompiler;
        _dslExecutor = dslExecutor;
        _canonicalMapper = canonicalMapper;
    }

    public Task<SourceResolutionResult> ResolveAsync(FieldRuntimePlan fieldPlan, object sourceRecord, IReadOnlyDictionary<string, object?> contextValues, CancellationToken ct = default)
    {
        if (fieldPlan.SourceTypeCode.Equals("CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new SourceResolutionResult { Success = true, Value = fieldPlan.ConstantValue, SourceUsed = "CONSTANTE" });
        }

        if (fieldPlan.SourceTypeCode.Equals("CONTEXT_VALUE", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(fieldPlan.PropertyPath) && contextValues.TryGetValue(fieldPlan.PropertyPath, out var ctxValue))
            {
                return Task.FromResult(new SourceResolutionResult { Success = true, Value = ctxValue, SourceUsed = $"CTX:{fieldPlan.PropertyPath}" });
            }

            return Task.FromResult(new SourceResolutionResult { Success = false, SourceUsed = "CONTEXT_VALUE", ErrorCode = "SOURCE_MISSING" });
        }

        if (fieldPlan.SourceTypeCode.Equals("EXPRESION", StringComparison.OrdinalIgnoreCase))
        {
            var issues = new List<string>();
            var compiled = fieldPlan.CompiledExpression ?? _dslCompiler.Compile(fieldPlan.ExpressionDslJson, issues);
            if (compiled is null)
            {
                return Task.FromResult(new SourceResolutionResult { Success = false, SourceUsed = "EXPRESION", ErrorCode = "DSL_COMPILE_ERROR" });
            }

            var value = _dslExecutor.Evaluate(compiled, sourceRecord, contextValues);
            return Task.FromResult(new SourceResolutionResult { Success = true, Value = value, SourceUsed = "EXPRESION" });
        }

        var path = fieldPlan.PropertyPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(new SourceResolutionResult { Success = false, SourceUsed = "ENTITY", ErrorCode = "PROPERTY_PATH_NOT_FOUND" });
        }

        _canonicalMapper.TryResolveCanonicalKey(fieldPlan.RecordCode, path, out var canonicalPath);
        var fallbackPath = string.IsNullOrWhiteSpace(canonicalPath) ? path : canonicalPath;

        if (sourceRecord is IReadOnlyDictionary<string, object?> dictSource)
        {
            if (TryResolveDictionaryValue(dictSource, path, fallbackPath, out var dictValue, out var resolvedKey))
            {
                return Task.FromResult(new SourceResolutionResult
                {
                    Success = true,
                    Value = dictValue,
                    SourceUsed = $"DICT:{resolvedKey}"
                });
            }

            return Task.FromResult(new SourceResolutionResult { Success = false, SourceUsed = $"DICT:{path}", ErrorCode = "SOURCE_MISSING" });
        }

        var resolvedPath = path;
        var accessor = BuildAccessor(sourceRecord.GetType(), path);
        var resolved = accessor(sourceRecord);
        if (resolved is null && !string.Equals(path, fallbackPath, StringComparison.OrdinalIgnoreCase))
        {
            var canonicalAccessor = BuildAccessor(sourceRecord.GetType(), fallbackPath);
            resolved = canonicalAccessor(sourceRecord);
            if (resolved is not null)
            {
                resolvedPath = fallbackPath;
            }
        }

        return Task.FromResult(new SourceResolutionResult
        {
            Success = resolved is not null,
            Value = resolved,
            SourceUsed = $"ENTITY:{resolvedPath}",
            ErrorCode = resolved is null ? "SOURCE_MISSING" : null
        });
    }


    private static bool TryResolveDictionaryValue(IReadOnlyDictionary<string, object?> source, string propertyPath, string canonicalPath, out object? value, out string resolvedKey)
    {
        value = null;
        resolvedKey = propertyPath;
        foreach (var candidate in BuildPathCandidates(propertyPath, canonicalPath))
        {
            if (source.TryGetValue(candidate, out value))
            {
                resolvedKey = candidate;
                return true;
            }

            var found = source.FirstOrDefault(kv => string.Equals(kv.Key, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(found.Key))
            {
                value = found.Value;
                resolvedKey = found.Key;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> BuildPathCandidates(string propertyPath, string canonicalPath)
    {
        yield return propertyPath;

        var pathTokens = propertyPath.Split(new[] { ".", ":", "/" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathTokens.Length > 0)
        {
            yield return pathTokens[^1];
        }

        if (!string.IsNullOrWhiteSpace(canonicalPath))
        {
            if (!string.Equals(canonicalPath, propertyPath, StringComparison.OrdinalIgnoreCase))
            {
                yield return canonicalPath;
            }

            var canonicalTokens = canonicalPath.Split(new[] { ".", ":", "/" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (canonicalTokens.Length > 0)
            {
                yield return canonicalTokens[^1];
            }
        }
    }

    private static Func<object, object?> BuildAccessor(Type type, string propertyPath)
    {
        var normalized = propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();
        return AccessorCache.GetOrAdd((type, normalized), key =>
        {
            var source = Expression.Parameter(typeof(object), "source");
            var cast = Expression.Convert(source, key.Type);
            var property = key.Type.GetProperties().FirstOrDefault(x => string.Equals(x.Name, key.Path, StringComparison.OrdinalIgnoreCase));
            if (property is null)
            {
                return _ => null;
            }

            var propExpr = Expression.Property(cast, property);
            var boxed = Expression.Convert(propExpr, typeof(object));
            return Expression.Lambda<Func<object, object?>>(boxed, source).Compile();
        });
    }
}
