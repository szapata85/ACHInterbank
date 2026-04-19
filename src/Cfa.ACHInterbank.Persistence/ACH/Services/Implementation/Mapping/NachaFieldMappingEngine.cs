using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class NachaFieldMappingEngine : INachaFieldMappingEngine
{
    private readonly IFieldSourceResolver _sourceResolver;
    private readonly INachaCanonicalMapper _canonicalMapper;
    private readonly IFieldTransformationEngine _transformationEngine;
    private readonly IFieldValidationEngine _validationEngine;
    private readonly IFieldFallbackEngine _fallbackEngine;

    public NachaFieldMappingEngine(
        IFieldSourceResolver sourceResolver,
        INachaCanonicalMapper canonicalMapper,
        IFieldTransformationEngine transformationEngine,
        IFieldValidationEngine validationEngine,
        IFieldFallbackEngine fallbackEngine)
    {
        _sourceResolver = sourceResolver;
        _canonicalMapper = canonicalMapper;
        _transformationEngine = transformationEngine;
        _validationEngine = validationEngine;
        _fallbackEngine = fallbackEngine;
    }

    public async Task<FieldMappingResult> MapFieldAsync(FieldMappingRequest request, CancellationToken ct = default)
    {
        var keyOrAlias = request.FieldPlan.PropertyPath ?? request.FieldPlan.FieldCode;
        var canonicalProbe = (_canonicalMapper as NachaCanonicalMapper)?.Probe(request.RecordCode, keyOrAlias);
        var resolvedCanonical = _canonicalMapper.TryResolveCanonicalKey(request.RecordCode, keyOrAlias, out var canonicalKey);
        canonicalKey = resolvedCanonical ? canonicalKey : keyOrAlias;

        if (!resolvedCanonical && canonicalProbe?.Failure == NachaCanonicalMapper.CanonicalResolutionFailure.AmbiguousAlias)
        {
            return new FieldMappingResult
            {
                Success = false,
                FinalValue = null,
                Trace = new FieldTrace
                {
                    FieldCode = request.FieldPlan.FieldCode,
                    CanonicalKey = null,
                    RawValue = null,
                    TransformedValue = null,
                    FinalValue = null,
                    SourceUsed = $"CANONICAL:{keyOrAlias}",
                    TransformSteps = [],
                    ValidationIssues =
                    [
                        new FieldValidationIssue
                        {
                            RuleCode = "AMBIGUOUS_ALIAS",
                            Severity = "ERROR",
                            Message = $"Alias '{keyOrAlias}' es ambiguo para record {request.RecordCode}."
                        }
                    ],
                    FallbackStrategy = null
                }
            };
        }

        var source = await _sourceResolver.ResolveAsync(request.FieldPlan, request.SourceRecord, request.ContextValues, ct);
        var transformed = await _transformationEngine.ApplyAsync(request.FieldPlan, source.Value, ct);
        var validation = await _validationEngine.ValidateAsync(request.FieldPlan, transformed.Value, ct);
        var fallback = await _fallbackEngine.ApplyAsync(request.FieldPlan, new FieldPipelineState
        {
            Source = source,
            Transform = transformed,
            Validation = validation
        }, ct);

        var finalValue = fallback.FailFastTriggered ? null : (fallback.Applied ? fallback.Value : transformed.Value);
        var success = !fallback.FailFastTriggered && !validation.HasBlockingErrors;
        if (!resolvedCanonical && canonicalProbe?.Failure is NachaCanonicalMapper.CanonicalResolutionFailure.UnresolvableAlias or NachaCanonicalMapper.CanonicalResolutionFailure.InvalidCanonicalKey)
        {
            validation.Issues.Add(new FieldValidationIssue
            {
                RuleCode = canonicalProbe.Failure == NachaCanonicalMapper.CanonicalResolutionFailure.InvalidCanonicalKey
                    ? "INVALID_CANONICAL_KEY"
                    : "UNRESOLVABLE_ALIAS",
                Severity = "WARN",
                Message = $"No se pudo normalizar '{keyOrAlias}' para record {request.RecordCode}; se conserva lookup directo."
            });
        }

        return new FieldMappingResult
        {
            Success = success,
            FinalValue = finalValue,
            Trace = new FieldTrace
            {
                FieldCode = request.FieldPlan.FieldCode,
                CanonicalKey = canonicalKey,
                RawValue = source.Value,
                TransformedValue = transformed.Value,
                FinalValue = finalValue,
                SourceUsed = source.SourceUsed,
                TransformSteps = transformed.AppliedSteps,
                ValidationIssues = validation.Issues,
                FallbackStrategy = fallback.Strategy
            }
        };
    }
}
