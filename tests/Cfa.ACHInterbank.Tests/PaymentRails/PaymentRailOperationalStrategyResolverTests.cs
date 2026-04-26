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
    public void ResolveRail_ByClearingHouseId_ResolvesExpectedRail()
    {
        var sut = BuildResolver();

        var ach = sut.ResolveRail(new PaymentRailResolveRequest(1, null, null));
        var cenit = sut.ResolveRail(new PaymentRailResolveRequest(2, null, null));

        ach.RailCode.Should().Be(PaymentRailCodes.AchColombia);
        ach.IsKnownRail.Should().BeTrue();
        cenit.RailCode.Should().Be(PaymentRailCodes.Cenit);
        cenit.IsKnownRail.Should().BeTrue();
    }

    [Fact]
    public void ResolveRail_WhenConflictBetweenIdAndCode_ReturnsUnknownFailClosed()
    {
        var sut = BuildResolver();

        var result = sut.ResolveRail(new PaymentRailResolveRequest(1, "CENIT", null));

        result.RailCode.Should().Be(PaymentRailCodes.Unknown);
        result.IsKnownRail.Should().BeFalse();
        result.ResolutionSource.Should().Be("Conflict");
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
