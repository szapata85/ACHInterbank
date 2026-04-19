using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;

public interface INachaRecordMappingEngine
{
    Task<RecordMappingResult> MapRecordAsync(RecordMappingRequest request, CancellationToken ct = default);
}

public interface INachaFieldMappingEngine
{
    Task<FieldMappingResult> MapFieldAsync(FieldMappingRequest request, CancellationToken ct = default);
}

public interface IFieldSourceResolver
{
    Task<SourceResolutionResult> ResolveAsync(FieldRuntimePlan fieldPlan, object sourceRecord, IReadOnlyDictionary<string, object?> contextValues, CancellationToken ct = default);
}

public interface INachaCanonicalMapper
{
    string ResolveCanonicalKey(string recordCode, string keyOrAlias);
    bool TryResolveCanonicalKey(string recordCode, string keyOrAlias, out string canonicalKey);
}

public interface IFieldTransformationEngine
{
    Task<TransformationResult> ApplyAsync(FieldRuntimePlan fieldPlan, object? value, CancellationToken ct = default);
}

public interface IFieldValidationEngine
{
    Task<FieldValidationResult> ValidateAsync(FieldRuntimePlan fieldPlan, object? value, CancellationToken ct = default);
}

public interface IFieldFallbackEngine
{
    Task<FallbackResult> ApplyAsync(FieldRuntimePlan fieldPlan, FieldPipelineState state, CancellationToken ct = default);
}

public interface IExpressionDslCompiler
{
    CompiledExpressionDsl? Compile(string? expressionDslJson, List<string> issues);
}

public interface IExpressionDslExecutor
{
    object? Evaluate(CompiledExpressionDsl expression, object sourceRecord, IReadOnlyDictionary<string, object?> contextValues);
}

public interface IFieldMappingPlanCompiler
{
    RecordRuntimePlan CompileRecordPlan(CfgLayoutVariant layoutVariant, List<string> issues);
}
