using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Mapping;

public class MappingEnginePhase1Tests
{
    [Fact]
    public async Task SourceResolver_ShouldResolveConstant()
    {
        var dsl = new ExpressionDslEngine();
        var sut = new FieldSourceResolver(dsl, dsl);
        var plan = BuildFieldPlan(sourceType: "CONSTANTE", constant: "ABC");

        var result = await sut.ResolveAsync(plan, new { Name = "X" }, new Dictionary<string, object?>());

        result.Success.Should().BeTrue();
        result.Value.Should().Be("ABC");
    }

    [Fact]
    public async Task SourceResolver_ShouldResolvePropertyPath()
    {
        var dsl = new ExpressionDslEngine();
        var sut = new FieldSourceResolver(dsl, dsl);
        var plan = BuildFieldPlan(sourceType: "ENTIDAD", propertyPath: "TraceNumber");

        var result = await sut.ResolveAsync(plan, new { TraceNumber = "123456" }, new Dictionary<string, object?>());

        result.Success.Should().BeTrue();
        result.Value.Should().Be("123456");
    }


    [Fact]
    public async Task SourceResolver_ShouldResolveContextValue()
    {
        var dsl = new ExpressionDslEngine();
        var sut = new FieldSourceResolver(dsl, dsl);
        var plan = BuildFieldPlan(sourceType: "CONTEXT_VALUE", propertyPath: "CycleName");

        var result = await sut.ResolveAsync(plan, new { Name = "X" }, new Dictionary<string, object?> { ["CycleName"] = "C40" });

        result.Success.Should().BeTrue();
        result.Value.Should().Be("C40");
    }

    [Fact]
    public void ExpressionDsl_ShouldCompileAndExecute()
    {
        var dsl = new ExpressionDslEngine();
        var issues = new List<string>();
        var compiled = dsl.Compile("{\"op\":\"upper\",\"args\":[{\"op\":\"prop\",\"path\":\"name\"}]}", issues);

        compiled.Should().NotBeNull();
        issues.Should().BeEmpty();
        var result = dsl.Evaluate(compiled!, new { Name = "juan" }, new Dictionary<string, object?>());
        result.Should().Be("JUAN");
    }

    [Fact]
    public async Task TransformPipeline_ShouldApplyMultipleSteps()
    {
        var sut = new FieldTransformationEngine();
        var plan = BuildFieldPlan(sourceType: "ENTIDAD", propertyPath: "Value", pipeline: "[{\"type\":\"trim\"},{\"type\":\"upper\"},{\"type\":\"truncate\",\"length\":3}]");

        var result = await sut.ApplyAsync(plan, " abcd ");

        result.Value.Should().Be("ABC");
        result.AppliedSteps.Should().ContainInOrder("trim", "upper", "truncate");
    }

    [Fact]
    public async Task RuleEngine_ShouldValidateRequiredRegexEnum()
    {
        var sut = new FieldValidationEngine();
        var plan = BuildFieldPlan(sourceType: "ENTIDAD", propertyPath: "Code", rules:
        [
            new FieldRulePlan { RuleCode = "R1", RuleTypeCode = "REQUIRED", Severity = "ERROR", IsEnabled = true },
            new FieldRulePlan { RuleCode = "R2", RuleTypeCode = "REGEX", Severity = "ERROR", IsEnabled = true, RuleConfigJson = "{\"pattern\":\"^[A-Z]{3}$\"}" },
            new FieldRulePlan { RuleCode = "R3", RuleTypeCode = "ENUM", Severity = "ERROR", IsEnabled = true, RuleConfigJson = "{\"values\":[\"ABC\",\"XYZ\"]}" }
        ]);

        var ok = await sut.ValidateAsync(plan, "ABC");
        ok.Issues.Should().BeEmpty();

        var bad = await sut.ValidateAsync(plan, "12");
        bad.Issues.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FallbackEngine_ShouldApplyDefaultAndFailFast()
    {
        var sut = new FieldFallbackEngine();
        var plan = BuildFieldPlan(sourceType: "ENTIDAD", propertyPath: "Code", fallback: "{\"strategy\":\"ordered_steps\",\"onRuleError\":\"fail_fast\",\"steps\":[{\"type\":\"default\",\"value\":\"000\"}]}");

        var failFast = await sut.ApplyAsync(plan, new FieldPipelineState
        {
            Validation = new FieldValidationResult
            {
                Issues = [new FieldValidationIssue { RuleCode = "R1", Severity = "ERROR", Message = "err" }]
            }
        });
        failFast.FailFastTriggered.Should().BeTrue();

        var normal = await sut.ApplyAsync(plan, new FieldPipelineState
        {
            Source = new SourceResolutionResult { Success = false },
            Transform = new TransformationResult { Value = null },
            Validation = new FieldValidationResult()
        });
        normal.Value.Should().Be("000");
    }

    [Fact]
    public void CanonicalMapper_ShouldResolveRecord6Alias()
    {
        var sut = new NachaCanonicalMapper();
        var key = sut.ResolveCanonicalKey("6", "receivercustomercode");
        key.Should().Be("ReceiverCustomerCode");
    }

    [Fact]
    public async Task RecordMappingEngine_ShouldMapRecord6()
    {
        var dsl = new ExpressionDslEngine();
        var source = new FieldSourceResolver(dsl, dsl);
        var transform = new FieldTransformationEngine();
        var validation = new FieldValidationEngine();
        var fallback = new FieldFallbackEngine();
        var canonical = new NachaCanonicalMapper();
        var fieldEngine = new NachaFieldMappingEngine(source, canonical, transform, validation, fallback);
        var recordEngine = new NachaRecordMappingEngine(fieldEngine);

        var plan = new RecordRuntimePlan
        {
            LayoutVariantId = 1,
            RecordCode = "6",
            TotalLength = 106,
            Fields =
            [
                BuildFieldPlan(sourceType: "ENTIDAD", propertyPath: "TraceNumber", fieldCode: "TRACE", pipeline: "[{\"type\":\"trim\"}]"),
                BuildFieldPlan(sourceType: "CONSTANTE", constant: "22", fieldCode: "TX")
            ]
        };

        var result = await recordEngine.MapRecordAsync(new RecordMappingRequest
        {
            RecordCode = "6",
            SourceRecord = new { TraceNumber = " 123 " },
            RecordPlan = plan,
            ContextValues = new Dictionary<string, object?>()
        });

        result.ValuesByFieldCode["TRACE"].Should().Be("123");
        result.ValuesByFieldCode["TX"].Should().Be("22");
    }

    private static FieldRuntimePlan BuildFieldPlan(
        string sourceType,
        string? propertyPath = null,
        string? constant = null,
        string? pipeline = null,
        string? fallback = null,
        string fieldCode = "F1",
        IReadOnlyList<FieldRulePlan>? rules = null)
    {
        return new FieldRuntimePlan
        {
            LayoutFieldId = 1,
            RecordCode = "6",
            FieldCode = fieldCode,
            FieldNameEs = fieldCode,
            StartPosition = 1,
            Length = 10,
            PadChar = ' ',
            Justification = 'L',
            SourceTypeCode = sourceType,
            PropertyPath = propertyPath,
            ConstantValue = constant,
            TransformationPipelineJson = pipeline,
            FallbackPolicyJson = fallback,
            Rules = rules ?? []
        };
    }
}
