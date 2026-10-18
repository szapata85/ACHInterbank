using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftOutboundWindowTests
{
    private static readonly DateTime ProcessingDate = new(2026, 9, 30);

    [Fact]
    public async Task HistoricalFiveCyclePolicy_ShouldInvokeOnlyActiveCycle()
    {
        await using var context = CreateContext();
        await SeedCyclesAsync(context, ProcessingDate, RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint);
        var service = AutomaticService("ACH-2");

        var result = await CreateHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(10)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("ciclo_activo=ACH-2", result);
        service.VerifyAll();
        VerifySingleAutomaticCall(service, "ACH-2");
    }

    [Fact]
    public async Task BeforeFirstOpening_ShouldNotInvokeOutboundService()
    {
        await using var context = CreateContext();
        await SeedCyclesAsync(context, ProcessingDate, RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint);
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);

        var result = await CreateHandler(context, service, LocalSnapshot(ProcessingDate.AddDays(-1).AddHours(19)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("ciclo_activo=ninguno", result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AfterLastClose_BeforeNextOvernightOpening_ShouldNotProcessPastOrFutureCycles()
    {
        await using var context = CreateContext();
        await SeedCyclesAsync(context, ProcessingDate, RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint);
        await SeedCyclesAsync(context, ProcessingDate.AddDays(1), RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint, "NEXT-");
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);

        var result = await CreateHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(18).AddMinutes(30)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("ciclo_activo=ninguno", result);
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("2026-09-30T08:30:00", "ACH-1")]
    [InlineData("2026-09-30T08:30:30", null)]
    [InlineData("2026-09-30T08:31:00", "ACH-2")]
    public async Task TransitionBetweenCycles_ShouldUseOnlyWindowCurrentlyOpen(string localTimestamp, string? expectedCycleId)
    {
        await using var context = CreateContext();
        await SeedCyclesAsync(context, ProcessingDate, RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint);
        var service = expectedCycleId is null
            ? new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict)
            : AutomaticService(expectedCycleId);

        var result = await CreateHandler(context, service, LocalSnapshot(DateTime.Parse(localTimestamp)))
            .ExecuteAsync(new TaskDefinition(), default);

        if (expectedCycleId is null)
        {
            Assert.Contains("ciclo_activo=ninguno", result);
            service.VerifyNoOtherCalls();
        }
        else
        {
            Assert.Contains($"ciclo_activo={expectedCycleId}", result);
            VerifySingleAutomaticCall(service, expectedCycleId);
        }
    }

    [Fact]
    public async Task OvernightCycle_ShouldSelectNextProcessingDateCycleOne()
    {
        await using var context = CreateContext();
        await SeedCyclesAsync(context, ProcessingDate, RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint);
        var service = AutomaticService("ACH-1");

        var result = await CreateHandler(context, service, LocalSnapshot(ProcessingDate.AddDays(-1).AddHours(20)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("ciclo_activo=ACH-1", result);
        VerifySingleAutomaticCall(service, "ACH-1");
    }

    [Fact]
    public async Task SyntheticSevenCyclePolicy_ShouldSelectActiveCycleWithoutAssumingCycleCount()
    {
        await using var context = CreateContext();
        var synthetic = Enumerable.Range(1, 7)
            .Select(number => new RegulatoryCycleSchedule(
                number,
                TimeSpan.FromHours((number - 1) * 2),
                TimeSpan.FromHours((number - 1) * 2 + 1),
                TimeSpan.FromHours((number - 1) * 2 + 1),
                TimeSpan.FromHours((number - 1) * 2 + 1)))
            .ToArray();
        await SeedCyclesAsync(context, ProcessingDate, synthetic, "S7-");
        var service = AutomaticService("S7-6");

        var result = await CreateHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(10).AddMinutes(30)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("ciclo_activo=S7-6", result);
        VerifySingleAutomaticCall(service, "S7-6");
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"achcol-mft-window-{Guid.NewGuid():N}")
            .Options);

    private static async Task SeedCyclesAsync(
        AchDbContext context,
        DateTime processingDate,
        IReadOnlyList<RegulatoryCycleSchedule> schedules,
        string prefix = "ACH-")
    {
        if (!await context.ClearingHouseConfigs.AnyAsync())
        {
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 1,
                ClearingHouseId = 1,
                TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId
            });
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1,
                ClearingHouseId = 1,
                Code = RegulatoryCycleScheduleCatalog.AchColombiaCode,
                Name = "ACH Colombia",
                OriginCode = "0001001"
            });
        }

        context.AchCycles.AddRange(schedules.Select(schedule => new AchCycle
        {
            Id = $"{prefix}{schedule.CycleNumber}",
            CycleName = $"Ciclo {schedule.CycleNumber}",
            ProcessingDate = processingDate.Date,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            CutoffTime = schedule.CutoffTime,
            OutputReleaseTime = schedule.OutputReleaseTime,
            ClearingHouseId = 1
        }));
        await context.SaveChangesAsync();
    }

    private static AchColombiaManagedMftOutboundHandler CreateHandler(
        AchDbContext context,
        Mock<IAchColombiaManagedFileExchangeService> service,
        OperationalTimeSnapshot snapshot)
    {
        var time = new Mock<IOperationalTimeSnapshotProvider>();
        time.Setup(x => x.CaptureNow()).Returns(snapshot);
        return new AchColombiaManagedMftOutboundHandler(
            context,
            service.Object,
            time.Object,
            new OperationalCycleWindowResolver());
    }

    private static Mock<IAchColombiaManagedFileExchangeService> AutomaticService(string cycleId)
    {
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);
        service.Setup(x => x.ExecuteOutboundAsync(
                cycleId,
                AchManagedFileExecutionOrigin.Automatic,
                "task:AchColombiaManagedMftOutbound",
                $"task:AchColombiaManagedMftOutbound:{cycleId}",
                It.IsAny<CancellationToken>(),
                null))
            .ReturnsAsync(new AchManagedFileExecutionResult(1, 1, 0, [Guid.NewGuid()]));
        return service;
    }

    private static void VerifySingleAutomaticCall(Mock<IAchColombiaManagedFileExchangeService> service, string cycleId)
    {
        service.Verify(x => x.ExecuteOutboundAsync(
            cycleId,
            AchManagedFileExecutionOrigin.Automatic,
            "task:AchColombiaManagedMftOutbound",
            $"task:AchColombiaManagedMftOutbound:{cycleId}",
            It.IsAny<CancellationToken>(),
            null), Times.Once);
        service.VerifyNoOtherCalls();
    }

    private static OperationalTimeSnapshot LocalSnapshot(DateTime localBogota)
    {
        var local = DateTime.SpecifyKind(localBogota, DateTimeKind.Unspecified);
        var capturedUtc = DateTime.SpecifyKind(local.AddHours(5), DateTimeKind.Utc);
        return new OperationalTimeSnapshot(
            capturedUtc,
            local,
            DateOnly.FromDateTime(local),
            RegulatoryCycleScheduleCatalog.BogotaTimeZoneId);
    }
}
