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

    [Fact]
    public void CompareReturnOperation_WhenWrapperPassive_ShouldBeEquivalent()
    {
        var sut = new PaymentRailShadowCompareService();
        var context = new PaymentRailResolvedContext(
            RailCode: PaymentRailCodes.Cenit,
            IsKnownRail: true,
            ResolutionSource: "Test",
            ResolutionMessage: "Test",
            StrategyRailCode: PaymentRailCodes.Cenit,
            Capabilities: new PaymentRailCapabilityDescriptor(true, true, true, true, true, true, "Test"),
            CapabilityStatuses: [new PaymentRailCapabilityStatus(PaymentRailCapabilityKind.Return, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy", "Test")],
            OperationalContext: new PaymentRailOperationalContext(2, "CENIT", "CENIT-1", new DateTime(2026, 4, 26), Guid.NewGuid().ToString("N")));
        var wrapper = new PaymentRailWrapperCallResult(
            PaymentRailCodes.Cenit,
            PaymentRailCapabilityKind.Return,
            IsCapabilitySupported: true,
            UseLegacyDecision: true,
            BehaviorChanged: false,
            ShadowCompareReady: true,
            WrapperDecisionCode: "PAYMENT_RAIL_WRAPPER_PASSIVE",
            Message: "Test");

        var result = sut.CompareReturnOperation(context, wrapper, "RETURN_GENERATED:R01", legacyOperationSucceeded: true);

        result.IsEquivalent.Should().BeTrue();
        result.ComparisonCode.Should().Be("PAYMENT_RAIL_SHADOW_RETURN_EQUIVALENT");
    }

    [Fact]
    public void CompareNettingAndLiquidity_WhenUnsupported_ShouldBeNonEquivalent()
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
            OperationalContext: new PaymentRailOperationalContext(null, "UNKNOWN", null, new DateTime(2026, 4, 26), Guid.NewGuid().ToString("N")));
        var wrapper = new PaymentRailWrapperCallResult(
            PaymentRailCodes.Unknown,
            PaymentRailCapabilityKind.Netting,
            IsCapabilitySupported: false,
            UseLegacyDecision: true,
            BehaviorChanged: false,
            ShadowCompareReady: false,
            WrapperDecisionCode: "PAYMENT_RAIL_WRAPPER_UNKNOWN_FAIL_CLOSED",
            Message: "Test");

        var netting = sut.CompareNettingOperation(context, wrapper, "CENIT_NETTING_CALCULATED", 10, 100m, 100m);
        var liquidity = sut.CompareLiquidityOperation(context, wrapper, "CENIT_LIQUIDITY_OPTIMIZED", 1, 1, 1);

        netting.IsEquivalent.Should().BeFalse();
        netting.ComparisonCode.Should().Be("PAYMENT_RAIL_SHADOW_NETTING_NON_EQUIVALENT");
        liquidity.IsEquivalent.Should().BeFalse();
        liquidity.ComparisonCode.Should().Be("PAYMENT_RAIL_SHADOW_LIQUIDITY_NON_EQUIVALENT");
    }
}
