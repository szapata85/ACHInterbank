using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class FieldMappingPlanCompiler : IFieldMappingPlanCompiler
{
    private readonly IExpressionDslCompiler _dslCompiler;

    public FieldMappingPlanCompiler(IExpressionDslCompiler dslCompiler)
    {
        _dslCompiler = dslCompiler;
    }

    public RecordRuntimePlan CompileRecordPlan(CfgLayoutVariant layoutVariant, List<string> issues)
    {
        var fieldPlans = new List<FieldRuntimePlan>();

        foreach (var field in layoutVariant.Fields.Where(x => x.IsEnabled).OrderBy(x => x.StartPosition))
        {
            var sourceType = field.SourceDefinition?.DataSourceType?.Code ?? "ENTIDAD";

            if (sourceType is "SQL_VIEW" or "SQL_PROCEDURE")
            {
                issues.Add($"Field {field.FieldCode}: source type no soportado en fase 1: {sourceType}.");
            }

            var compiledExpression = _dslCompiler.Compile(field.SourceDefinition?.ExpressionDsl, issues);

            var rules = field.Rules
                .Where(r => r.IsEnabled)
                .Select(r => new FieldRulePlan
                {
                    RuleCode = r.RuleCode,
                    RuleTypeCode = r.RuleType?.Code ?? string.Empty,
                    RuleConfigJson = r.RuleConfigJson,
                    Severity = r.Severity,
                    IsEnabled = r.IsEnabled
                })
                .ToList();

            fieldPlans.Add(new FieldRuntimePlan
            {
                LayoutFieldId = field.Id,
                RecordCode = layoutVariant.RecordCode?.Code ?? string.Empty,
                FieldCode = field.FieldCode,
                FieldNameEs = field.FieldNameEs,
                StartPosition = field.StartPosition,
                Length = field.Length,
                PadChar = field.PadChar,
                Justification = field.Justification,
                FormatMask = field.FormatMask,
                SourceTypeCode = sourceType,
                PropertyPath = field.SourceDefinition?.PropertyPath,
                ConstantValue = field.SourceDefinition?.ConstantValue,
                ExpressionDslJson = field.SourceDefinition?.ExpressionDsl,
                TransformationPipelineJson = field.TransformationPipelineJson,
                FallbackPolicyJson = field.SourceDefinition?.FallbackPolicyJson,
                Rules = rules,
                CompiledExpression = compiledExpression
            });
        }

        return new RecordRuntimePlan
        {
            LayoutVariantId = layoutVariant.Id,
            RecordCode = layoutVariant.RecordCode?.Code ?? string.Empty,
            TotalLength = layoutVariant.TotalLength,
            Fields = fieldPlans
        };
    }
}
