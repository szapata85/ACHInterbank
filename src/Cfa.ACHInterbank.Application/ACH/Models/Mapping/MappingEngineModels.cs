using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models.Mapping;

public sealed class RecordMappingRequest
{
    public required string RecordCode { get; init; }
    public required object SourceRecord { get; init; }
    public required RecordRuntimePlan RecordPlan { get; init; }
    public required IReadOnlyDictionary<string, object?> ContextValues { get; init; }
    public bool EnableDiagnostics { get; init; }
    public bool ShadowCompare { get; init; }
    public NachaRecordLayout? LegacyLayout { get; init; }
}

public sealed class FieldMappingRequest
{
    public required string RecordCode { get; init; }
    public required FieldRuntimePlan FieldPlan { get; init; }
    public required object SourceRecord { get; init; }
    public required IReadOnlyDictionary<string, object?> ContextValues { get; init; }
}

public sealed class RecordRuntimePlan
{
    public int LayoutVariantId { get; init; }
    public required string RecordCode { get; init; }
    public int TotalLength { get; init; }
    public required IReadOnlyList<FieldRuntimePlan> Fields { get; init; }
}

public sealed class FieldRuntimePlan
{
    public int LayoutFieldId { get; init; }
    public required string RecordCode { get; init; }
    public required string FieldCode { get; init; }
    public required string FieldNameEs { get; init; }
    public int StartPosition { get; init; }
    public int Length { get; init; }
    public char PadChar { get; init; }
    public char Justification { get; init; }
    public string? FormatMask { get; init; }
    public required string SourceTypeCode { get; init; }
    public string? PropertyPath { get; init; }
    public string? ConstantValue { get; init; }
    public string? ExpressionDslJson { get; init; }
    public string? TransformationPipelineJson { get; init; }
    public string? FallbackPolicyJson { get; init; }
    public required IReadOnlyList<FieldRulePlan> Rules { get; init; }
    public CompiledExpressionDsl? CompiledExpression { get; set; }
}

public sealed class FieldRulePlan
{
    public required string RuleCode { get; init; }
    public required string RuleTypeCode { get; init; }
    public string? RuleConfigJson { get; init; }
    public string Severity { get; init; } = "ERROR";
    public bool IsEnabled { get; init; } = true;
}

public sealed class CompiledExpressionDsl
{
    public required ExpressionNode Root { get; init; }
}

public sealed class ExpressionNode
{
    public required string Op { get; init; }
    public string? Path { get; init; }
    public string? Key { get; init; }
    public object? Value { get; init; }
    public List<ExpressionNode> Args { get; init; } = [];
}

public sealed class SourceResolutionResult
{
    public bool Success { get; set; }
    public object? Value { get; init; }
    public string SourceUsed { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
}

public sealed class TransformationResult
{
    public object? Value { get; init; }
    public List<string> AppliedSteps { get; init; } = [];
    public string? ErrorCode { get; init; }
}

public sealed class FieldValidationIssue
{
    public required string RuleCode { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
}

public sealed class FieldValidationResult
{
    public List<FieldValidationIssue> Issues { get; set; } = [];
    public bool HasBlockingErrors => Issues.Any(x => string.Equals(x.Severity, "ERROR", StringComparison.OrdinalIgnoreCase));
}

public sealed class FieldPipelineState
{
    public SourceResolutionResult Source { get; init; } = new();
    public TransformationResult Transform { get; init; } = new();
    public FieldValidationResult Validation { get; init; } = new();
}

public sealed class FallbackResult
{
    public bool Applied { get; init; }
    public object? Value { get; init; }
    public string Strategy { get; init; } = string.Empty;
    public string? Warning { get; init; }
    public bool FailFastTriggered { get; init; }
}

public sealed class FieldTrace
{
    public required string FieldCode { get; init; }
    public string? CanonicalKey { get; init; }
    public object? RawValue { get; init; }
    public object? TransformedValue { get; init; }
    public object? FinalValue { get; init; }
    public string SourceUsed { get; init; } = string.Empty;
    public List<string> TransformSteps { get; init; } = [];
    public List<FieldValidationIssue> ValidationIssues { get; init; } = [];
    public string? FallbackStrategy { get; init; }
}

public sealed class FieldMappingResult
{
    public bool Success { get; init; }
    public object? FinalValue { get; init; }
    public required FieldTrace Trace { get; init; }
}

public sealed class RecordMappingResult
{
    public bool Success { get; set; }
    public Dictionary<string, object?> ValuesByFieldCode { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<FieldTrace> FieldTraces { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}
