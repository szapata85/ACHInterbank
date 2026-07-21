using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Tests.PaymentRails;

public class PaymentRailOperationalStrategyResolverTests
{
    private static IPaymentRailOperationalStrategyResolver BuildResolver()
    {
        var mapper = new ClearingHouseToPaymentRailMapper();
        var strategies = new IPaymentRailOperationalStrategy[]
        {
            new AchColombiaPaymentRailOperationalStrategy(),
            new CenitPaymentRailOperationalStrategy(),
            new UnknownPaymentRailOperationalStrategy()
        };

        return new PaymentRailOperationalStrategyResolver(mapper, strategies);
    }

    [Fact]
    public void ResolveRail_ByClearingHouseIdAlone_FailsClosedWithoutAssumingSeedIds()
    {
        var sut = BuildResolver();

        var ach = sut.ResolveRail(new PaymentRailResolveRequest(1, null, null));
        var cenit = sut.ResolveRail(new PaymentRailResolveRequest(2, null, null));

        ach.RailCode.Should().Be(PaymentRailCodes.Unknown);
        ach.IsKnownRail.Should().BeFalse();
        cenit.RailCode.Should().Be(PaymentRailCodes.Unknown);
        cenit.IsKnownRail.Should().BeFalse();
    }

    [Fact]
    public void ResolveRail_WhenIdAndFunctionalCodeAreProvided_UsesFunctionalCode()
    {
        var sut = BuildResolver();

        var result = sut.ResolveRail(new PaymentRailResolveRequest(1, "CENIT", null));

        result.RailCode.Should().Be(PaymentRailCodes.Cenit);
        result.IsKnownRail.Should().BeTrue();
        result.ResolutionSource.Should().Be("ClearingHouseCode");
    }

    [Fact]
    public void ResolveStrategy_WhenUnknownRail_ReturnsFailClosedStrategy()
    {
        var sut = BuildResolver();
        var request = new PaymentRailResolveRequest(null, "NOT_MAPPED", null);

        var strategy = sut.ResolveStrategy(request);
        var bridgeResult = strategy.EvaluateBridge(new PaymentRailBridgeRequest(
            new PaymentRailOperationalContext(null, "NOT_MAPPED", null, null, Guid.NewGuid().ToString("N")),
            "phase1-bridge-check"));

        strategy.RailCode.Should().Be(PaymentRailCodes.Unknown);
        bridgeResult.IsAllowed.Should().BeFalse();
        bridgeResult.ResultCode.Should().Be("PAYMENT_RAIL_UNKNOWN_FAIL_CLOSED");
    }
}
