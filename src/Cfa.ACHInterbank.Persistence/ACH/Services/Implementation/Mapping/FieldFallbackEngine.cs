using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class FieldFallbackEngine : IFieldFallbackEngine
{
    public Task<FallbackResult> ApplyAsync(FieldRuntimePlan fieldPlan, FieldPipelineState state, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(fieldPlan.FallbackPolicyJson))
        {
            return Task.FromResult(ApplyPolicy(fieldPlan.FallbackPolicyJson, state));
        }

        if (state.Validation.HasBlockingErrors)
        {
            return Task.FromResult(new FallbackResult { Applied = true, FailFastTriggered = true, Strategy = "FAIL_FAST", Warning = "Validation error bloqueante." });
        }

        if (state.Source.Success)
        {
            return Task.FromResult(new FallbackResult { Applied = false, Value = state.Transform.Value, Strategy = "NONE" });
        }

        return Task.FromResult(new FallbackResult { Applied = true, Value = null, Strategy = "NULL_IF_MISSING", Warning = "Source missing." });
    }

    private static FallbackResult ApplyPolicy(string policyJson, FieldPipelineState state)
    {
        using var doc = JsonDocument.Parse(policyJson);
        var root = doc.RootElement;
        var strategy = root.TryGetProperty("strategy", out var st) ? st.GetString() ?? "ordered_steps" : "ordered_steps";

        if (root.TryGetProperty("onRuleError", out var onRuleError)
            && string.Equals(onRuleError.GetString(), "fail_fast", StringComparison.OrdinalIgnoreCase)
            && state.Validation.HasBlockingErrors)
        {
            return new FallbackResult { Applied = true, FailFastTriggered = true, Strategy = "FAIL_FAST", Warning = "Fail-fast por regla bloqueante." };
        }

        if (root.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
        {
            foreach (var step in steps.EnumerateArray())
            {
                var type = step.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : string.Empty;
                if (string.Equals(type, "default", StringComparison.OrdinalIgnoreCase))
                {
                    var value = step.TryGetProperty("value", out var val) ? val.GetString() : null;
                    return new FallbackResult { Applied = true, Value = value, Strategy = "DEFAULT", Warning = "Fallback default aplicado." };
                }

                if (string.Equals(type, "null_if_missing", StringComparison.OrdinalIgnoreCase))
                {
                    return new FallbackResult { Applied = true, Value = null, Strategy = "NULL_IF_MISSING", Warning = "Fallback null_if_missing aplicado." };
                }

                if (string.Equals(type, "coalesce", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(state.Transform.Value?.ToString()))
                {
                    return new FallbackResult { Applied = true, Value = state.Transform.Value, Strategy = "COALESCE", Warning = "Fallback coalesce aplicado." };
                }
            }
        }

        return new FallbackResult { Applied = false, Value = state.Transform.Value, Strategy = strategy };
    }
}
