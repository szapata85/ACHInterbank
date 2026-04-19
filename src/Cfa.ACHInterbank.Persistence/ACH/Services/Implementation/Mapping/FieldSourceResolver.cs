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
    private static readonly ConcurrentDictionary<(Type Type, string Path), Func<object, object?>> AccessorCache = new();

    public FieldSourceResolver(IExpressionDslCompiler dslCompiler, IExpressionDslExecutor dslExecutor)
    {
        _dslCompiler = dslCompiler;
        _dslExecutor = dslExecutor;
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

        var accessor = BuildAccessor(sourceRecord.GetType(), path);
        var resolved = accessor(sourceRecord);
        return Task.FromResult(new SourceResolutionResult
        {
            Success = resolved is not null,
            Value = resolved,
            SourceUsed = $"ENTITY:{path}",
            ErrorCode = resolved is null ? "SOURCE_MISSING" : null
        });
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
