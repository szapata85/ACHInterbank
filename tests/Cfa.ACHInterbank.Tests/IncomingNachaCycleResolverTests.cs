using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
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
    public async Task ResolveAsync_ResolvesOfficialNameWithoutTraditionalExtension()
    {
        using var context = BuildContext();
        Seed(context, multiCandidate: false);
        var sut = new IncomingNachaCycleResolver(context);

        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "1234567.001.1",
            Records = [BuildHeader("1111111111", "20260417")]
        });

        Assert.True(result.IsResolved);
        Assert.Equal("ACH-20260417-01", result.AchCycleId);
    }

    [Fact]
    public async Task ResolveAsync_ResolvesCenitOfficialName_UsingSecondSegmentCycleNumber()
    {
        using var context = BuildContext();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 2,
            Name = "CENIT",
            Code = "CENIT",
            OriginCode = "0000001283",
            ClearingHouseId = 2,
            ClearingHouseConfig = new ClearingHouseConfig { Id = 2, HolidayStrategy = "Colombian" }
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = "CENIT-20260713-01",
            CycleName = "Ciclo 1",
            ClearingHouseId = 2,
            ProcessingDate = new DateTime(2026, 7, 13),
            CutoffTime = new TimeSpan(8, 0, 0),
            StartTime = new TimeSpan(7, 0, 0),
            EndTime = new TimeSpan(9, 0, 0)
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = "CENIT-20260713-02",
            CycleName = "Ciclo 2",
            ClearingHouseId = 2,
            ProcessingDate = new DateTime(2026, 7, 13),
            CutoffTime = new TimeSpan(10, 0, 0),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(11, 0, 0)
        });
        await context.SaveChangesAsync();

        var sut = new IncomingNachaCycleResolver(context);
        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "0001283.002.20260713.1",
            Records = [BuildHeader("0000001283", "20260713")]
        });

        Assert.True(result.IsResolved);
        Assert.Equal("CENIT-20260713-02", result.AchCycleId);
        Assert.Contains("fileCycleNumber", result.EvidenceJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2", result.EvidenceJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsError_WhenCenitFilenameDateDiffersFromHeader()
    {
        using var context = BuildContext();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 2,
            Name = "CENIT",
            Code = "CENIT",
            OriginCode = "0000001283",
            ClearingHouseId = 2,
            ClearingHouseConfig = new ClearingHouseConfig { Id = 2, HolidayStrategy = "Colombian" }
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = "CENIT-20260713-02",
            CycleName = "Ciclo 2",
            ClearingHouseId = 2,
            ProcessingDate = new DateTime(2026, 7, 13),
            CutoffTime = new TimeSpan(10, 0, 0),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(11, 0, 0)
        });
        await context.SaveChangesAsync();

        var sut = new IncomingNachaCycleResolver(context);
        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "0001283.002.20260714.1",
            Records = [BuildHeader("0000001283", "20260713")]
        });

        Assert.False(result.IsResolved);
        Assert.Contains("CENIT_FILENAME_HEADER_DATE_MISMATCH", result.Errors);
        Assert.Equal(Domain.Models.ACH.IncomingNachaCycleResolutionStatus.NoResuelto, result.Status);
    }

    [Fact]
    public async Task ResolveAsync_DoesNotFallback_WhenOfficialNameCarriesMissingCycleNumber()
    {
        using var context = BuildContext();
        Seed(context, multiCandidate: false);
        var sut = new IncomingNachaCycleResolver(context);

        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "1234567.001.6",
            Records = [BuildHeader("1111111111", "20260417")]
        });

        Assert.True(result.IsResolved);
        Assert.Equal("ACH-20260417-01", result.AchCycleId);
        Assert.Equal(Domain.Models.ACH.IncomingNachaCycleResolutionStatus.ResueltoInferido, result.Status);
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

    [Theory]
    [InlineData("0840", "ACHCOL-20260727-02")]
    [InlineData("0740", "ACHCOL-20260727-01")]
    [InlineData("1200", null)]
    public async Task ResolveAsync_ResolvesAchColombiaCycle_FromHeaderCreationTimeWindow(
        string creationTime,
        string? expectedCycleId)
    {
        using var context = BuildContext();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 7,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "1111111111",
            ClearingHouseId = 7,
            ClearingHouseConfig = new ClearingHouseConfig { Id = 7, HolidayStrategy = "Colombian" }
        });
        context.AchCycles.AddRange(
            new AchCycle
            {
                Id = "ACHCOL-20260727-01",
                CycleName = "Ciclo 1",
                ClearingHouseId = 7,
                ProcessingDate = new DateTime(2026, 7, 27),
                StartTime = new TimeSpan(19, 1, 0),
                EndTime = new TimeSpan(8, 15, 0),
                CutoffTime = new TimeSpan(8, 15, 0)
            },
            new AchCycle
            {
                Id = "ACHCOL-20260727-02",
                CycleName = "Ciclo 2",
                ClearingHouseId = 7,
                ProcessingDate = new DateTime(2026, 7, 27),
                StartTime = new TimeSpan(8, 16, 0),
                EndTime = new TimeSpan(10, 45, 0),
                CutoffTime = new TimeSpan(10, 45, 0)
            });
        await context.SaveChangesAsync();
        var sut = new IncomingNachaCycleResolver(context);

        var result = await sut.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = "0001283.001.20260727.1.OUT",
            Records = [BuildHeader("1111111111", "20260727", creationTime)]
        });

        if (expectedCycleId is null)
        {
            Assert.False(result.IsResolved);
            Assert.Equal(Domain.Models.ACH.IncomingNachaCycleResolutionStatus.NoResuelto, result.Status);
            Assert.Contains("ACHCOL_HEADER_TIME_WITHOUT_CYCLE_WINDOW", result.Errors);
        }
        else
        {
            Assert.True(result.IsResolved);
            Assert.Equal(expectedCycleId, result.AchCycleId);
            Assert.Equal("Header+Ventana", result.ResolutionMode);
            Assert.Contains("resolvedByHeaderTime", result.EvidenceJson, StringComparison.Ordinal);
        }
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

    private static string BuildHeader(string immediateOrigin, string processingDate, string creationTime = "1200")
    {
        return "1" + "01" + "0000000001" + immediateOrigin.PadLeft(10, '0') + processingDate + creationTime + "A" + "10610" + "1" + "DESTINO".PadRight(23) + "ORIGEN".PadRight(23) + "REF00001".PadRight(8) + new string(' ', 10);
    }
}
