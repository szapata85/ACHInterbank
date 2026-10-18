using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftOperationalTriggerTests
{
    private static readonly DateTime ProcessingDate = new(2026, 10, 9);

    [Fact]
    public async Task C1_0630Trigger_ShouldUseProcessingDateD_WithoutReplacingOvernightRegulation()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            cycleConfigId: 701,
            cycleId: "ACH-C1-D",
            regulatoryStart: new TimeSpan(19, 1, 0),
            regulatoryCutoff: new TimeSpan(7, 0, 0),
            operationalStart: new TimeSpan(6, 30, 0),
            operationalLatest: null);
        var exchange = SuccessfulExchange("ACH-C1-D", ProcessingDate, 1);
        var handler = CreateHandler(context, exchange, LocalSnapshot(ProcessingDate.AddHours(6).AddMinutes(30)));

        var result = await handler.ExecuteAsync(EnabledTask(), ExecutionContext(1, 701), default);

        Assert.Contains("ciclo_operativo=ACH-C1-D", result);
        Assert.Contains("fecha_operativa=2026-10-09", result);
        exchange.VerifyAll();

        var regulation = await context.ClearingHouseCycleConfigs.AsNoTracking().SingleAsync(x => x.Id == 701);
        Assert.Equal(new TimeSpan(19, 1, 0), regulation.StartTime);
        Assert.Equal(new TimeSpan(7, 0, 0), regulation.CutoffTime);
    }

    [Fact]
    public async Task C7_1750_ShouldBeAllowed_AndRegulatoryCutoffMustRemain1800()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            cycleConfigId: 707,
            cycleId: "ACH-C7-D",
            regulatoryStart: new TimeSpan(16, 1, 0),
            regulatoryCutoff: new TimeSpan(18, 0, 0),
            operationalStart: new TimeSpan(17, 50, 0),
            operationalLatest: new TimeSpan(17, 50, 0));
        var exchange = SuccessfulExchange("ACH-C7-D", ProcessingDate, 1);
        var handler = CreateHandler(context, exchange, LocalSnapshot(ProcessingDate.AddHours(17).AddMinutes(50)));

        var result = await handler.ExecuteAsync(EnabledTask(), ExecutionContext(1, 707), default);

        Assert.Contains("ciclo_operativo=ACH-C7-D", result);
        exchange.VerifyAll();
        var regulation = await context.ClearingHouseCycleConfigs.AsNoTracking().SingleAsync(x => x.Id == 707);
        Assert.Equal(new TimeSpan(18, 0, 0), regulation.CutoffTime);
    }

    [Fact]
    public async Task C7_1751StaleOrMisfiredTrigger_ShouldNotGenerateAfterLatestStart()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            cycleConfigId: 707,
            cycleId: "ACH-C7-D",
            regulatoryStart: new TimeSpan(16, 1, 0),
            regulatoryCutoff: new TimeSpan(18, 0, 0),
            operationalStart: new TimeSpan(17, 50, 0),
            operationalLatest: new TimeSpan(17, 50, 0));
        var exchange = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);
        var handler = CreateHandler(context, exchange, LocalSnapshot(ProcessingDate.AddHours(17).AddMinutes(51)));

        var result = await handler.ExecuteAsync(EnabledTask(), ExecutionContext(1, 707, misfireDetected: true), default);

        Assert.Contains("misfire_despues_latest_start", result);
        exchange.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RescheduledOldFingerprint_ShouldBeRejectedDefensively()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            cycleConfigId: 701,
            cycleId: "ACH-C1-D",
            regulatoryStart: new TimeSpan(19, 1, 0),
            regulatoryCutoff: new TimeSpan(7, 0, 0),
            operationalStart: new TimeSpan(6, 30, 0),
            operationalLatest: null);
        var exchange = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);
        var handler = CreateHandler(context, exchange, LocalSnapshot(ProcessingDate.AddHours(6).AddMinutes(30)));
        var execution = ExecutionContext(1, 701, scheduleFingerprint: "obsolete-fingerprint");

        var result = await handler.ExecuteAsync(EnabledTask(), execution, default);

        Assert.Contains("trigger_operacional_obsoleto", result);
        exchange.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StaleTrigger_ShouldRecheckCurrentScheduleAndMftProfileBeforeGeneration()
    {
        await using var context = CreateContext();
        await SeedAsync(
            context,
            cycleConfigId: 701,
            cycleId: "ACH-C1-D",
            regulatoryStart: new TimeSpan(19, 1, 0),
            regulatoryCutoff: new TimeSpan(7, 0, 0),
            operationalStart: new TimeSpan(6, 30, 0),
            operationalLatest: null);
        var schedule = await context.AchColombiaCycleOperationalSchedules.SingleAsync();
        schedule.AutomaticOutboundEnabled = false;
        await context.SaveChangesAsync();
        var exchange = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);
        var handler = CreateHandler(context, exchange, LocalSnapshot(ProcessingDate.AddHours(6).AddMinutes(30)));

        var result = await handler.ExecuteAsync(EnabledTask(), ExecutionContext(1, 701), default);

        Assert.Contains("programacion_operacional_inactiva", result);
        exchange.VerifyNoOtherCalls();
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"achcol-mft-operational-{Guid.NewGuid():N}")
            .Options);

    private static async Task SeedAsync(
        AchDbContext context,
        int cycleConfigId,
        string cycleId,
        TimeSpan regulatoryStart,
        TimeSpan regulatoryCutoff,
        TimeSpan operationalStart,
        TimeSpan? operationalLatest)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 100,
            ClearingHouseId = 1,
            TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId
        });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            ClearingHouseId = 100,
            Code = RegulatoryCycleScheduleCatalog.AchColombiaCode,
            Name = "ACH Colombia",
            OriginCode = "0001001",
            IsActive = true
        });
        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            Id = cycleConfigId,
            ClearingHouseId = 1,
            PolicyVersion = "TEST-ACTIVE",
            CycleName = cycleConfigId == 701 ? "Ciclo 1" : "Ciclo 7",
            StartTime = regulatoryStart,
            EndTime = regulatoryCutoff,
            CutoffTime = regulatoryCutoff,
            OutputReleaseTime = regulatoryCutoff,
            IsActive = true,
            EffectiveFrom = ProcessingDate.AddYears(-1)
        });
        context.AchColombiaCycleOperationalSchedules.Add(new AchColombiaCycleOperationalSchedule
        {
            Id = 1,
            ClearingHouseId = 1,
            ClearingHouseCycleConfigId = cycleConfigId,
            AutomaticOutboundEnabled = true,
            AutomaticOutboundStartTime = operationalStart,
            AutomaticOutboundLatestStartTime = operationalLatest,
            OutboundExecutionStrategy = operationalLatest.HasValue
                ? AchColombiaOutboundExecutionStrategy.Window
                : AchColombiaOutboundExecutionStrategy.ExactTime,
            EffectiveTimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            IsActive = true
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            CycleName = cycleConfigId == 701 ? "Ciclo 1" : "Ciclo 7",
            ProcessingDate = ProcessingDate,
            StartTime = regulatoryStart,
            EndTime = regulatoryCutoff,
            CutoffTime = regulatoryCutoff,
            OutputReleaseTime = regulatoryCutoff,
            ClearingHouseId = 1,
            ClearingHouseCycleConfigId = cycleConfigId
        });
        context.AchManagedFileTransferConfigurations.Add(new AchManagedFileTransferConfiguration
        {
            Id = 1,
            ClearingHouseId = 1,
            ProfileEnabled = true,
            AutomaticOutboundEnabled = true,
            AutomaticInboundEnabled = true,
            Provider = "ManagedFolder",
            Protocol = "ManagedFile"
        });
        await context.SaveChangesAsync();
    }

    private static AchColombiaManagedMftOutboundHandler CreateHandler(
        AchDbContext context,
        Mock<IAchColombiaManagedFileExchangeService> exchange,
        OperationalTimeSnapshot snapshot)
    {
        var time = new Mock<IOperationalTimeSnapshotProvider>();
        time.Setup(x => x.CaptureNow()).Returns(snapshot);
        return new AchColombiaManagedMftOutboundHandler(
            context,
            exchange.Object,
            time.Object,
            new OperationalCycleWindowResolver());
    }

    private static Mock<IAchColombiaManagedFileExchangeService> SuccessfulExchange(
        string cycleId,
        DateTime processingDate,
        int houseId)
    {
        var exchange = new Mock<IAchColombiaManagedFileExchangeService>(MockBehavior.Strict);
        var idempotencyKey = $"task:AchColombiaManagedMftOutbound:CH:{houseId}:DATE:{processingDate:yyyyMMdd}:CYCLE:{cycleId}:FLOW:OUTBOUND";
        exchange.Setup(x => x.ExecuteOutboundAsync(
                cycleId,
                AchManagedFileExecutionOrigin.Automatic,
                "task:AchColombiaManagedMftOutbound",
                idempotencyKey,
                It.IsAny<CancellationToken>(),
                null))
            .ReturnsAsync(new AchManagedFileExecutionResult(1, 1, 0, [Guid.NewGuid()]));
        return exchange;
    }

    private static TaskDefinition EnabledTask()
        => new()
        {
            Id = 930,
            Code = "AchColombiaManagedMftOutbound",
            Status = TaskStatusEnum.Enabled,
            Paused = false
        };

    private static SchedulerTaskExecutionContext ExecutionContext(
        int operationalScheduleId,
        int cycleConfigId,
        bool misfireDetected = false,
        string? scheduleFingerprint = null,
        DateTimeOffset? scheduledFireTimeUtc = null)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            "fire",
            "scheduler-node",
            false,
            0,
            new Dictionary<string, string>
            {
                ["OperationalScheduleId"] = operationalScheduleId.ToString(),
                ["ClearingHouseId"] = "1",
                ["CycleConfigId"] = cycleConfigId.ToString(),
                ["Flow"] = "outbound",
                ["TriggerType"] = "AchColombiaOperationalOutbound",
                ["ScheduleFingerprint"] = scheduleFingerprint ?? string.Empty
            },
            scheduledFireTimeUtc,
            misfireDetected);

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
