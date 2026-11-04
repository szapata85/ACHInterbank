using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public partial class NachaInboundSimulatorTests
{
    [Theory]
    [InlineData("ACHCOL", "ACH-CYCLE")]
    [InlineData("CENIT", "CENIT-CYCLE")]
    public async Task I07_PrenotificationReturnCycle_DoesNotRequireExistingReturnTransactions(
        string houseCode, string expectedCycleId)
    {
        await using var context = CreateContext();
        Seed(context);
        // La semilla solo contiene créditos y débitos, ninguna devolución de prenotificación.
        Assert.False(await context.AchTransactions.AnyAsync(x => x.IsPrenotification));
        var simulator = CreateService(context, CreateTempOutput());
        var cycles = await simulator.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = houseCode,
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingPrenotificationReturn
        });
        var cycle = Assert.Single(cycles);
        Assert.Equal(expectedCycleId, cycle.CycleId);
        Assert.Equal(0, cycle.TransactionCount);
        Assert.Empty(await context.NachaInboundSimulations.ToListAsync());
    }

    [Fact]
    public async Task I07_PrenotificationReturnCycle_DoesNotExposeCancelledCycle()
    {
        await using var context = CreateContext();
        Seed(context);
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == "ACH-CYCLE");
        cycle.OperationalStatus = AchCycleOperationalStatus.Cancelled;
        await context.SaveChangesAsync();
        var simulator = CreateService(context, CreateTempOutput());
        Assert.Empty(await simulator.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingPrenotificationReturn
        }));
    }
}
