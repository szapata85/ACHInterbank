using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Tests.PaymentRails;

public class PaymentRailContextServiceTests
{
    private static IPaymentRailContextService BuildSut()
    {
        var mapper = new ClearingHouseToPaymentRailMapper();
        var resolver = new PaymentRailOperationalStrategyResolver(
            mapper,
            new IPaymentRailOperationalStrategy[]
            {
                new AchColombiaPaymentRailOperationalStrategy(),
                new CenitPaymentRailOperationalStrategy(),
                new UnknownPaymentRailOperationalStrategy()
            });

        return new PaymentRailContextService(resolver);
    }

    [Fact]
    public void ResolveContext_FromClearingHouse_ReturnsRailContextWithoutChangingLegacyInputs()
    {
        var sut = BuildSut();

        var result = sut.ResolveContext(
            clearingHouseId: 1,
            clearingHouseCode: "ACHCOL",
            achCycleId: "ACH-20260425-01",
            operationalDate: new DateTime(2026, 4, 25));

        result.RailCode.Should().Be(PaymentRailCodes.AchColombia);
        result.IsKnownRail.Should().BeTrue();
        result.OperationalContext.ClearingHouseId.Should().Be(1);
        result.OperationalContext.ClearingHouseCode.Should().Be("ACHCOL");
        result.OperationalContext.AchCycleId.Should().Be("ACH-20260425-01");
    }

    [Fact]
    public void BuildShadowSnapshot_CarriesLegacyAndRailParallelView()
    {
        var sut = BuildSut();
        var resolved = sut.ResolveContext(2, "CENIT", "CENIT-20260425-02", new DateTime(2026, 4, 25));

        var snapshot = sut.BuildShadowSnapshot(resolved, legacySource: "ClearingHouseId", legacyValue: "2");

        snapshot.LegacySource.Should().Be("ClearingHouseId");
        snapshot.LegacyValue.Should().Be("2");
        snapshot.RailCode.Should().Be(PaymentRailCodes.Cenit);
        snapshot.IsKnownRail.Should().BeTrue();
        snapshot.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }
}
