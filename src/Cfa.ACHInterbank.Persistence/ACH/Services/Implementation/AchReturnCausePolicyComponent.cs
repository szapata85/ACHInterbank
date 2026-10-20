using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed class AchReturnCausePolicyComponent(
    IAchCauseCodePolicy? causeCodePolicy,
    ILogger<AchReturnsService> logger)
{
    public async Task ValidateAsync(
        AchTransaction transaction,
        AchReturnGenerationScope scope,
        string reasonCode,
        CancellationToken ct)
    {
        if (causeCodePolicy is null)
        {
            return;
        }

        var policyResult = await causeCodePolicy.EvaluateAsync(
            new AchCauseCodePolicyRequest(
                reasonCode,
                AchCauseCodeFlow.OutboundReturn,
                scope.ReturnCycle.ClearingHouseId,
                scope.ReturnCycle.ClearingHouse?.Code,
                transaction.Type.ToString(),
                transaction.EffectiveEntryDate,
                Source: nameof(AchReturnsService.GenerateReturnsFileAsync)),
            ct);

        foreach (var issue in policyResult.Issues.Where(i => i.Severity != AchCauseCodePolicySeverity.Error))
        {
            logger.LogWarning(
                "CAUSE_POLICY_WARNING flow={Flow} code={Code} severity={Severity} detail={Detail}",
                AchCauseCodeFlow.OutboundReturn,
                reasonCode,
                issue.Severity,
                issue.Message);
        }

        var blocking = policyResult.Issues.Where(i => i.Severity == AchCauseCodePolicySeverity.Error).ToList();
        if (!policyResult.IsAllowed || blocking.Count > 0)
        {
            var detail = blocking.Count > 0
                ? string.Join(" | ", blocking.Select(x => $"{x.Code}: {x.Message}"))
                : "Causal no permitida por política rail-flow.";
            throw new InvalidOperationException($"La causal {reasonCode} no está permitida para la cámara/flujo de devolución saliente. {detail}");
        }
    }
}
