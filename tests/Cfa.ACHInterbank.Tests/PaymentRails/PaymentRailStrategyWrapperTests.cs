using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Tests.PaymentRails;

public class PaymentRailStrategyWrapperTests
{
    [Fact]
    public void AchStrategy_ShouldExposePassiveWrapperCapabilities_AndKeepLegacyDecision()
    {
        IPaymentRailOperationalStrategy sut = new AchColombiaPaymentRailOperationalStrategy();
        var request = new PaymentRailWrapperCallRequest(
            new PaymentRailOperationalContext(1, "ACHCOL", "ACH-20260425-01", new DateTime(2026, 4, 25), Guid.NewGuid().ToString("N")),
            PaymentRailCapabilityKind.Dispatch,
            LegacyDecisionCode: "LEGACY_DISPATCH_DECISION");

        var result = sut.EvaluateCapabilityWrapper(request);
        var shadow = sut.BuildCapabilityShadowSnapshot(request, result);

        result.IsCapabilitySupported.Should().BeTrue();
        result.UseLegacyDecision.Should().BeTrue();
        result.BehaviorChanged.Should().BeFalse();
        result.ShadowCompareReady.Should().BeTrue();
        shadow.LegacyValue.Should().Be("LEGACY_DISPATCH_DECISION");

        sut.CapabilityStatuses.Should().Contain(x => x.Capability == PaymentRailCapabilityKind.Cycle && x.ExecutionMode == PaymentRailCapabilityExecutionMode.WrapperPassive);
        sut.CapabilityStatuses.Should().Contain(x => x.Capability == PaymentRailCapabilityKind.Netting && !x.IsSupported);
    }

    [Fact]
    public void CenitStrategy_ShouldExposeNettingAndLiquidityAsPassiveWrappers()
    {
        IPaymentRailOperationalStrategy sut = new CenitPaymentRailOperationalStrategy();

        sut.CapabilityStatuses.Should().Contain(x => x.Capability == PaymentRailCapabilityKind.Netting && x.IsSupported && x.ExecutionMode == PaymentRailCapabilityExecutionMode.WrapperPassive);
        sut.CapabilityStatuses.Should().Contain(x => x.Capability == PaymentRailCapabilityKind.Liquidity && x.IsSupported && x.ExecutionMode == PaymentRailCapabilityExecutionMode.WrapperPassive);

        var request = new PaymentRailWrapperCallRequest(
            new PaymentRailOperationalContext(2, "CENIT", "CENIT-20260425-04", new DateTime(2026, 4, 25), Guid.NewGuid().ToString("N")),
            PaymentRailCapabilityKind.Liquidity,
            LegacyDecisionCode: "LEGACY_LIQUIDITY_DECISION");

        var result = sut.EvaluateCapabilityWrapper(request);
        result.UseLegacyDecision.Should().BeTrue();
        result.BehaviorChanged.Should().BeFalse();
        result.WrapperDecisionCode.Should().Be("PAYMENT_RAIL_WRAPPER_PASSIVE");
    }

    [Fact]
    public void UnknownStrategy_ShouldFailClosedForWrapperCalls()
    {
        IPaymentRailOperationalStrategy sut = new UnknownPaymentRailOperationalStrategy();
        var request = new PaymentRailWrapperCallRequest(
            new PaymentRailOperationalContext(null, "UNKNOWN", null, null, Guid.NewGuid().ToString("N")),
            PaymentRailCapabilityKind.Cycle,
            LegacyDecisionCode: "LEGACY_CYCLE");

        var result = sut.EvaluateCapabilityWrapper(request);

        result.IsCapabilitySupported.Should().BeFalse();
        result.UseLegacyDecision.Should().BeTrue();
        result.BehaviorChanged.Should().BeFalse();
        result.WrapperDecisionCode.Should().Be("PAYMENT_RAIL_WRAPPER_UNKNOWN_FAIL_CLOSED");
    }
}
