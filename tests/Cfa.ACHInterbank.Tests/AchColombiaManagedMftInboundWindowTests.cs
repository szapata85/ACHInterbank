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

public sealed class AchColombiaManagedMftInboundWindowTests
{
    private static readonly DateTime ProcessingDate = new(2026, 9, 30);

    [Fact]
    public async Task BeforeFirstReleaseStart_ShouldNotPoll()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);

        var result = await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(9).AddMinutes(14).AddSeconds(59)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("polling=omitido", result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExactReleaseStart_ShouldPoll()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        var service = AutomaticInboundService();

        await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(9).AddMinutes(15)))
            .ExecuteAsync(new TaskDefinition(), default);

        VerifySingleAutomaticInboundCall(service);
    }

    [Fact]
    public async Task InsideReleaseWindow_ShouldPoll()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        var service = AutomaticInboundService();

        await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(9).AddMinutes(20)))
            .ExecuteAsync(new TaskDefinition(), default);

        VerifySingleAutomaticInboundCall(service);
    }

    [Fact]
    public async Task AfterReleaseEnd_ShouldNotPollUntilAnotherExplicitWindowIsActive()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);

        var result = await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(10)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("polling=omitido", result);
        Assert.Contains("fuera_ventana_liberacion", result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExactReleaseEnd_ShouldStillPoll()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        var service = AutomaticInboundService();

        await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(9).AddMinutes(30)))
            .ExecuteAsync(new TaskDefinition(), default);

        VerifySingleAutomaticInboundCall(service);
    }

    [Fact]
    public async Task HistoricalFiveCyclePolicyWithoutExplicitStageWindows_ShouldNotInventInboundWindow()
    {
        await using var context = CreateContext();
        await SeedCyclesAsync(context, RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint);
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);

        var result = await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(7)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("polling=omitido", result);
        Assert.Contains("fuera_ventana_liberacion", result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SyntheticSevenCyclePolicy_ShouldGateBeforeItsFirstExplicitReleaseStart()
    {
        await using var context = CreateContext();
        var schedules = Enumerable.Range(1, 7)
            .Select(number => new RegulatoryCycleSchedule(
                number,
                TimeSpan.FromHours(number),
                TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(30)),
                TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(30)),
                TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(50)),
                OutputReleaseStartTime: TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(40)),
                OutputReleaseEndTime: TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(50))))
            .ToArray();
        await SeedCyclesAsync(context, schedules, "S7-");
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);

        var result = await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(1).AddMinutes(39)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("polling=omitido", result);
        Assert.Equal(7, await context.AchCycles.CountAsync());
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SyntheticSevenCyclePolicy_InsideFirstReleaseWindow_ShouldPoll()
    {
        await using var context = CreateContext();
        var schedules = Enumerable.Range(1, 7)
            .Select(number => new RegulatoryCycleSchedule(
                number,
                TimeSpan.FromHours(number),
                TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(30)),
                TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(30)),
                TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(50)),
                OutputReleaseStartTime: TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(40)),
                OutputReleaseEndTime: TimeSpan.FromHours(number).Add(TimeSpan.FromMinutes(50))))
            .ToArray();
        await SeedCyclesAsync(context, schedules, "S7-");
        var service = AutomaticInboundService();

        await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(1).AddMinutes(40)))
            .ExecuteAsync(new TaskDefinition(), default);

        VerifySingleAutomaticInboundCall(service);
    }

    [Fact]
    public async Task PartialReleaseConfiguration_ShouldUseOnlyExplicitCompleteWindows()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        var first = await context.AchCycles.SingleAsync(x => x.Id == "ACH-1");
        first.OutputReleaseEndTime = null;
        first.OutputReleaseTime = new TimeSpan(23, 59, 0);
        await context.SaveChangesAsync();

        var outside = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);
        var skipped = await CreateInboundHandler(context, outside, LocalSnapshot(ProcessingDate.AddHours(9).AddMinutes(20)))
            .ExecuteAsync(new TaskDefinition(), default);
        Assert.Contains("polling=omitido", skipped);
        outside.VerifyNoOtherCalls();

        var insideCycle2 = AutomaticInboundService();
        await CreateInboundHandler(context, insideCycle2, LocalSnapshot(ProcessingDate.AddHours(11).AddMinutes(50)))
            .ExecuteAsync(new TaskDefinition(), default);
        VerifySingleAutomaticInboundCall(insideCycle2);
    }

    [Fact]
    public async Task BetweenReleaseWindows_ShouldNotPoll()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);

        var result = await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(10).AddMinutes(30)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("polling=omitido", result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InboundWindowGating_ShouldIgnoreCenitEvenWhenItsWindowIsActive()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 2,
            ClearingHouseId = 2,
            TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId
        });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 2,
            ClearingHouseId = 2,
            Code = RegulatoryCycleScheduleCatalog.CenitCode,
            Name = "CENIT",
            OriginCode = "0002002"
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = "CENIT-1",
            CycleName = "Ciclo CENIT",
            ProcessingDate = ProcessingDate,
            StartTime = new(9, 0, 0),
            EndTime = new(11, 0, 0),
            CutoffTime = new(11, 0, 0),
            OutputReleaseTime = new(11, 0, 0),
            OutputReleaseStartTime = new(10, 0, 0),
            OutputReleaseEndTime = new(11, 0, 0),
            ClearingHouseId = 2
        });
        await context.SaveChangesAsync();
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);

        var result = await CreateInboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(10).AddMinutes(30)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("polling=omitido", result);
        service.VerifyNoOtherCalls();
        var cenit = await context.AchCycles.SingleAsync(x => x.Id == "CENIT-1");
        Assert.Equal(new TimeSpan(10, 0, 0), cenit.OutputReleaseStartTime);
        Assert.Equal(new TimeSpan(11, 0, 0), cenit.OutputReleaseEndTime);
    }

    [Fact]
    public async Task ReleaseWindowAwareness_ShouldNotChangeOutboundSendingWindowSelection()
    {
        await using var context = CreateContext();
        await SeedFiveCyclesWithReleaseWindowsAsync(context);
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);
        service.Setup(x => x.ExecuteOutboundAsync(
                "ACH-2",
                AchManagedFileExecutionOrigin.Automatic,
                "task:AchColombiaManagedMftOutbound",
                "task:AchColombiaManagedMftOutbound:ACH-2",
                It.IsAny<CancellationToken>(),
                null))
            .ReturnsAsync(new AchManagedFileExecutionResult(1, 1, 0, [Guid.NewGuid()]));

        var result = await CreateOutboundHandler(context, service, LocalSnapshot(ProcessingDate.AddHours(10)))
            .ExecuteAsync(new TaskDefinition(), default);

        Assert.Contains("ciclo_activo=ACH-2", result);
        service.VerifyAll();
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"achcol-mft-inbound-window-{Guid.NewGuid():N}")
            .Options);

    private static Task SeedFiveCyclesWithReleaseWindowsAsync(AchDbContext context)
        => SeedCyclesAsync(context,
        [
            StageSchedule(1, new(19, 1, 0), new(8, 30, 0), new(9, 15, 0), new(9, 30, 0)),
            StageSchedule(2, new(8, 31, 0), new(11, 0, 0), new(11, 45, 0), new(12, 0, 0)),
            StageSchedule(3, new(11, 1, 0), new(14, 0, 0), new(14, 45, 0), new(15, 0, 0)),
            StageSchedule(4, new(14, 1, 0), new(16, 0, 0), new(16, 45, 0), new(17, 0, 0)),
            StageSchedule(5, new(16, 1, 0), new(18, 0, 0), new(18, 45, 0), new(19, 0, 0))
        ]);

    private static RegulatoryCycleSchedule StageSchedule(
        int number,
        TimeSpan start,
        TimeSpan close,
        TimeSpan releaseStart,
        TimeSpan releaseEnd)
        => new(
            number,
            start,
            close,
            close,
            releaseEnd,
            OutputReleaseStartTime: releaseStart,
            OutputReleaseEndTime: releaseEnd);

    private static async Task SeedCyclesAsync(
        AchDbContext context,
        IReadOnlyList<RegulatoryCycleSchedule> schedules,
        string prefix = "ACH-")
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
        context.AchCycles.AddRange(schedules.Select(schedule => new AchCycle
        {
            Id = $"{prefix}{schedule.CycleNumber}",
            CycleName = $"Ciclo {schedule.CycleNumber}",
            ProcessingDate = ProcessingDate,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            CutoffTime = schedule.CutoffTime,
            OutputReleaseTime = schedule.OutputReleaseTime,
            OutputReleaseStartTime = schedule.OutputReleaseStartTime,
            OutputReleaseEndTime = schedule.OutputReleaseEndTime,
            ClearingHouseId = 1
        }));
        await context.SaveChangesAsync();
    }

    private static AchColombiaManagedMftInboundHandler CreateInboundHandler(
        AchDbContext context,
        Mock<IAchColombiaManagedFileExchangeService> service,
        OperationalTimeSnapshot snapshot)
    {
        var time = new Mock<IOperationalTimeSnapshotProvider>();
        time.Setup(x => x.CaptureNow()).Returns(snapshot);
        return new AchColombiaManagedMftInboundHandler(
            context,
            service.Object,
            time.Object,
            new OperationalCycleWindowResolver());
    }

    private static AchColombiaManagedMftOutboundHandler CreateOutboundHandler(
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

    private static Mock<IAchColombiaManagedFileExchangeService> AutomaticInboundService()
    {
        var service = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);
        service.Setup(x => x.ExecuteInboundAsync(
                AchManagedFileExecutionOrigin.Automatic,
                "task:AchColombiaManagedMftInbound",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchManagedFileExecutionResult(0, 0, 0, []));
        return service;
    }

    private static void VerifySingleAutomaticInboundCall(Mock<IAchColombiaManagedFileExchangeService> service)
    {
        service.Verify(x => x.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task:AchColombiaManagedMftInbound",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
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
