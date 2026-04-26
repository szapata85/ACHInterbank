using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaCycleResolverTests
{
    [Fact]
    public async Task ResolveAsync_ResolvesSingleCandidate()
    {
        using var context = BuildContext();
        Seed(context, multiCandidate: false);
        var sut = new IncomingNachaCycleResolver(context);

        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "entrante.1.ach",
            Records = [BuildHeader("1111111111", "20260417")]
        });

        Assert.True(result.IsResolved);
        Assert.Equal("ACH-20260417-01", result.AchCycleId);
        Assert.Contains("shadowCompare", result.EvidenceJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsAmbiguous_WhenMultipleCandidates()
    {
        using var context = BuildContext();
        Seed(context, multiCandidate: true);
        var sut = new IncomingNachaCycleResolver(context);

        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "entrante.ach",
            Records = [BuildHeader("1111111111", "20260417")]
        });

        Assert.True(result.IsAmbiguous);
        Assert.Equal(Domain.Models.ACH.IncomingNachaCycleResolutionStatus.Ambiguo, result.Status);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNoResuelto_WhenNoCandidates()
    {
        using var context = BuildContext();
        Seed(context, multiCandidate: false);
        var sut = new IncomingNachaCycleResolver(context);

        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "entrante.5.ach",
            Records = [BuildHeader("1111111111", "20260101")]
        });

        Assert.False(result.IsResolved);
        Assert.Equal(Domain.Models.ACH.IncomingNachaCycleResolutionStatus.NoResuelto, result.Status);
    }

    [Fact]
    public async Task ResolveAsync_InfersClearingHouseByCatalogWithoutHardcodedIds()
    {
        using var context = BuildContext();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 99,
            Name = "CENIT Banco de la República",
            Code = "CENIT",
            OriginCode = "9999999999",
            ClearingHouseId = 99,
            ClearingHouseConfig = new ClearingHouseConfig { Id = 99, HolidayStrategy = "Colombian" }
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = "CENIT-20260417-01",
            CycleName = "Ciclo 1",
            ClearingHouseId = 99,
            ProcessingDate = new DateTime(2026, 4, 17),
            CutoffTime = new TimeSpan(8, 0, 0),
            StartTime = new TimeSpan(7, 0, 0),
            EndTime = new TimeSpan(9, 0, 0)
        });
        await context.SaveChangesAsync();

        var sut = new IncomingNachaCycleResolver(context);
        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "entrante_cenit_1.ach",
            Records = [BuildHeader("0000000000", "20260417")]
        });

        Assert.True(result.IsResolved);
        Assert.Equal(99, result.ClearingHouseId);
        Assert.Equal("CENIT-20260417-01", result.AchCycleId);
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AchDbContext(options);
    }

    private static void Seed(AchDbContext context, bool multiCandidate)
    {
        context.ClearingHouses.Add(new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACH", OriginCode = "1111111111", ClearingHouseId = 1, ClearingHouseConfig = new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" } });
        context.AchCycles.Add(new AchCycle { Id = "ACH-20260417-01", CycleName = "Ciclo 1", ClearingHouseId = 1, ProcessingDate = new DateTime(2026, 4, 17), CutoffTime = new TimeSpan(8, 0, 0), StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(9, 0, 0) });
        if (multiCandidate)
        {
            context.AchCycles.Add(new AchCycle { Id = "ACH-20260417-02", CycleName = "Ciclo 2", ClearingHouseId = 1, ProcessingDate = new DateTime(2026, 4, 17), CutoffTime = new TimeSpan(10, 0, 0), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(11, 0, 0) });
        }

        context.SaveChanges();
    }

    private static string BuildHeader(string immediateOrigin, string processingDate)
    {
        return "1" + "01" + "0000000001" + immediateOrigin.PadLeft(10, '0') + processingDate + "1200" + "A" + "10610" + "1" + "DESTINO".PadRight(23) + "ORIGEN".PadRight(23) + "REF00001".PadRight(8) + new string(' ', 10);
    }
}
