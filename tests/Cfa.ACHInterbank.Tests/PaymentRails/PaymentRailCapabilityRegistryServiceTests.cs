using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests.PaymentRails;

public class PaymentRailCapabilityRegistryServiceTests
{
    [Fact]
    public async Task GetEffectiveCapabilitiesAsync_WithoutOverrides_UsesStrategyDefaults()
    {
        await using var context = BuildContext();
        var sut = BuildService(context);

        var result = await sut.GetEffectiveCapabilitiesAsync(clearingHouseId: 2, clearingHouseCode: "CENIT");

        result.Should().ContainSingle(x => x.CapabilityCode == PaymentRailCapabilityRegistryCodes.CycleResolution && x.State == PaymentRailCapabilityRegistryState.ShadowOnly);
        result.Should().ContainSingle(x => x.CapabilityCode == PaymentRailCapabilityRegistryCodes.Netting && x.State == PaymentRailCapabilityRegistryState.ShadowOnly);
        result.Should().ContainSingle(x => x.CapabilityCode == PaymentRailCapabilityRegistryCodes.CrossBorderPayments && x.State == PaymentRailCapabilityRegistryState.Planned);
    }

    [Fact]
    public async Task UpsertCapabilityAsync_WithOverride_PrioritizesRegistry()
    {
        await using var context = BuildContext();
        var sut = BuildService(context);

        await sut.UpsertCapabilityAsync(new UpsertPaymentRailCapabilityRegistryRequest(
            RailCode: PaymentRailCodes.Cenit,
            CapabilityCode: PaymentRailCapabilityRegistryCodes.Netting,
            State: PaymentRailCapabilityRegistryState.Disabled,
            ChangedBy: "qa.prompt7",
            ChangeTicket: "P7-001",
            Notes: "Ventana de mantenimiento"));

        var result = await sut.GetEffectiveCapabilitiesAsync(clearingHouseId: 2, clearingHouseCode: "CENIT");
        var netting = result.Single(x => x.CapabilityCode == PaymentRailCapabilityRegistryCodes.Netting);

        netting.State.Should().Be(PaymentRailCapabilityRegistryState.Disabled);
        netting.Source.Should().Be(PaymentRailCapabilityRegistrySources.RegistryOverride);
        netting.Version.Should().StartWith("registry:");
        netting.ChangeTicket.Should().Be("P7-001");
    }

    [Fact]
    public async Task GetEffectiveCapabilitiesByRailAsync_WithKnownRail_ReturnsCatalog()
    {
        await using var context = BuildContext();
        var sut = BuildService(context);

        var result = await sut.GetEffectiveCapabilitiesByRailAsync(PaymentRailCodes.AchColombia);

        result.Should().HaveCount(PaymentRailCapabilityRegistryCodes.All.Count);
        result.Should().OnlyContain(x => x.RailCode == PaymentRailCodes.AchColombia);
        result.Should().Contain(x => x.Source == PaymentRailCapabilityRegistrySources.StrategyDefault);
    }

    [Fact]
    public async Task GetEffectiveCapabilityByRailAsync_WithOverride_ReturnsSpecificCapability()
    {
        await using var context = BuildContext();
        var sut = BuildService(context);

        await sut.UpsertCapabilityAsync(new UpsertPaymentRailCapabilityRegistryRequest(
            RailCode: PaymentRailCodes.Cenit,
            CapabilityCode: PaymentRailCapabilityRegistryCodes.Liquidity,
            State: PaymentRailCapabilityRegistryState.Disabled,
            ChangedBy: "qa.prompt8",
            ChangeTicket: "P8-RO-01",
            Notes: "Solo consulta"));

        var capability = await sut.GetEffectiveCapabilityByRailAsync(PaymentRailCodes.Cenit, PaymentRailCapabilityRegistryCodes.Liquidity);

        capability.Should().NotBeNull();
        capability!.Source.Should().Be(PaymentRailCapabilityRegistrySources.RegistryOverride);
        capability.State.Should().Be(PaymentRailCapabilityRegistryState.Disabled);
        capability.ChangeSource.Should().Be("Manual");
    }

    [Fact]
    public void GetAvailableRails_ReturnsOperationalRailsAndFailClosedRail()
    {
        using var context = BuildContext();
        var sut = BuildService(context);

        var rails = sut.GetAvailableRails();

        rails.Should().Contain(x => x.RailCode == PaymentRailCodes.AchColombia && x.IsOperational);
        rails.Should().Contain(x => x.RailCode == PaymentRailCodes.Cenit && x.IsOperational);
        rails.Should().Contain(x => x.RailCode == PaymentRailCodes.Unknown && !x.IsOperational);
    }

    private static PaymentRailCapabilityRegistryService BuildService(AchDbContext context)
    {
        var mapper = new ClearingHouseToPaymentRailMapper();
        var resolver = new PaymentRailOperationalStrategyResolver(mapper,
        [
            new AchColombiaPaymentRailOperationalStrategy(),
            new CenitPaymentRailOperationalStrategy(),
            new UnknownPaymentRailOperationalStrategy()
        ]);
        var contextService = new PaymentRailContextService(resolver);
        return new PaymentRailCapabilityRegistryService(context, contextService, resolver);
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AchDbContext(options);
    }
}
