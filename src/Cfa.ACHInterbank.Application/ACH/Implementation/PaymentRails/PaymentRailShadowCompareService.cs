using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class PaymentRailShadowCompareService : IPaymentRailShadowCompareService
{
    public PaymentRailShadowCompareResult CompareCycleResolution(
        PaymentRailResolvedContext resolvedContext,
        PaymentRailWrapperCallResult wrapperResult,
        string legacyDecisionCode,
        bool legacyResolved)
    {
        var equivalent = wrapperResult.UseLegacyDecision
                         && !wrapperResult.BehaviorChanged
                         && (!legacyResolved || wrapperResult.IsCapabilitySupported);

        var code = equivalent
            ? "PAYMENT_RAIL_SHADOW_CYCLE_EQUIVALENT"
            : "PAYMENT_RAIL_SHADOW_CYCLE_NON_EQUIVALENT";

        var notes = equivalent
            ? "Shadow cycle compare equivalente; owner legacy preservado."
            : "Shadow cycle compare no equivalente; se mantiene decisión legacy sin cutover.";

        return new PaymentRailShadowCompareResult(
            IsEquivalent: equivalent,
            ComparisonCode: code,
            LegacyDecisionCode: legacyDecisionCode,
            WrapperDecisionCode: wrapperResult.WrapperDecisionCode,
            RailCode: resolvedContext.RailCode,
            Capability: PaymentRailCapabilityKind.Cycle.ToString(),
            Notes: notes,
            ComparedAtUtc: DateTime.UtcNow);
    }

    public PaymentRailShadowCompareResult CompareDispatchPlanning(
        PaymentRailResolvedContext resolvedContext,
        PaymentRailWrapperCallResult wrapperResult,
        string legacyDecisionCode,
        bool legacyEligible,
        bool legacyWaitingWindow,
        int legacyPriority)
    {
        var equivalent = wrapperResult.UseLegacyDecision
                         && !wrapperResult.BehaviorChanged
                         && wrapperResult.IsCapabilitySupported;

        var code = equivalent
            ? "PAYMENT_RAIL_SHADOW_DISPATCH_EQUIVALENT"
            : "PAYMENT_RAIL_SHADOW_DISPATCH_NON_EQUIVALENT";

        var notes = equivalent
            ? $"Shadow dispatch equivalente; LegacyEligible={legacyEligible};LegacyWaitingWindow={legacyWaitingWindow};LegacyPriority={legacyPriority}."
            : $"Shadow dispatch no equivalente; LegacyEligible={legacyEligible};LegacyWaitingWindow={legacyWaitingWindow};LegacyPriority={legacyPriority}. Se conserva owner legacy.";

        return new PaymentRailShadowCompareResult(
            IsEquivalent: equivalent,
            ComparisonCode: code,
            LegacyDecisionCode: legacyDecisionCode,
            WrapperDecisionCode: wrapperResult.WrapperDecisionCode,
            RailCode: resolvedContext.RailCode,
            Capability: PaymentRailCapabilityKind.Dispatch.ToString(),
            Notes: notes,
            ComparedAtUtc: DateTime.UtcNow);
    }
}
