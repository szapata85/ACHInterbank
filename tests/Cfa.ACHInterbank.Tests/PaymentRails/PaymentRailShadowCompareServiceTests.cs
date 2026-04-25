using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Tests.PaymentRails;

public class PaymentRailShadowCompareServiceTests
{
    [Fact]
    public void CompareCycleResolution_WhenWrapperPassive_ShouldBeEquivalent()
    {
        var sut = new PaymentRailShadowCompareService();
        var context = new PaymentRailResolvedContext(
            RailCode: PaymentRailCodes.AchColombia,
            IsKnownRail: true,
            ResolutionSource: "Test",
            ResolutionMessage: "Test",
            StrategyRailCode: PaymentRailCodes.AchColombia,
            Capabilities: new PaymentRailCapabilityDescriptor(true, true, true, false, false, true, "Test"),
            CapabilityStatuses: [new PaymentRailCapabilityStatus(PaymentRailCapabilityKind.Cycle, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy", "Test")],
            OperationalContext: new PaymentRailOperationalContext(1, "ACHCOL", "ACH-1", new DateTime(2026, 4, 25), Guid.NewGuid().ToString("N")));
        var wrapper = new PaymentRailWrapperCallResult(
            PaymentRailCodes.AchColombia,
            PaymentRailCapabilityKind.Cycle,
            IsCapabilitySupported: true,
            UseLegacyDecision: true,
            BehaviorChanged: false,
            ShadowCompareReady: true,
            WrapperDecisionCode: "PAYMENT_RAIL_WRAPPER_PASSIVE",
            Message: "Test");

        var result = sut.CompareCycleResolution(context, wrapper, legacyDecisionCode: "ACH-1", legacyResolved: true);

        result.IsEquivalent.Should().BeTrue();
        result.ComparisonCode.Should().Be("PAYMENT_RAIL_SHADOW_CYCLE_EQUIVALENT");
    }

    [Fact]
    public void CompareDispatchPlanning_WhenUnsupported_ShouldBeNonEquivalent()
    {
        var sut = new PaymentRailShadowCompareService();
        var context = new PaymentRailResolvedContext(
            RailCode: PaymentRailCodes.Unknown,
            IsKnownRail: false,
            ResolutionSource: "Test",
            ResolutionMessage: "Test",
            StrategyRailCode: PaymentRailCodes.Unknown,
            Capabilities: new PaymentRailCapabilityDescriptor(false, false, false, false, false, true, "Test"),
            CapabilityStatuses: [],
            OperationalContext: new PaymentRailOperationalContext(null, "UNKNOWN", null, new DateTime(2026, 4, 25), Guid.NewGuid().ToString("N")));
        var wrapper = new PaymentRailWrapperCallResult(
            PaymentRailCodes.Unknown,
            PaymentRailCapabilityKind.Dispatch,
            IsCapabilitySupported: false,
            UseLegacyDecision: true,
            BehaviorChanged: false,
            ShadowCompareReady: false,
            WrapperDecisionCode: "PAYMENT_RAIL_WRAPPER_UNKNOWN_FAIL_CLOSED",
            Message: "Test");

        var result = sut.CompareDispatchPlanning(context, wrapper, "Blocked", legacyEligible: false, legacyWaitingWindow: false, legacyPriority: 999);

        result.IsEquivalent.Should().BeFalse();
        result.ComparisonCode.Should().Be("PAYMENT_RAIL_SHADOW_DISPATCH_NON_EQUIVALENT");
    }
}
