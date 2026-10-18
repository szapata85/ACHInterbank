using System.Reflection;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using Quartz.Impl.Matchers;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaOperationalQuartzScheduleTests
{
    private const int TaskId = 930;
    private const int HouseId = 73;
    private const int HouseConfigId = 173;
    private const int C1ConfigId = 701;
    private const int C7ConfigId = 707;

    [Fact]
    public async Task C1_ExactTime0630_ShouldCreateDeterministicClusterSafeTrigger_WithoutLegacyPollingJob()
    {
        var dbName = nameof(C1_ExactTime0630_ShouldCreateDeterministicClusterSafeTrigger_WithoutLegacyPollingJob);
        await SeedAsync(dbName, C1ConfigId, new TimeSpan(19, 1, 0), new TimeSpan(7, 0, 0), new TimeSpan(6, 30, 0), null,
            AchColombiaOutboundExecutionStrategy.ExactTime);
        var fixture = QuartzFixture.Create();
        var legacyKey = new JobKey($"job:{TaskId}", "db-tasks");
        var legacyJob = JobBuilder.Create<NonConcurrentDynamicJob>()
            .WithIdentity(legacyKey)
            .UsingJobData("TaskId", TaskId.ToString())
            .StoreDurably()
            .Build();
        fixture.Jobs[legacyKey] = legacyJob;
        var legacyTrigger = TriggerBuilder.Create()
            .WithIdentity($"trg:{TaskId}", "db-tasks")
            .ForJob(legacyKey)
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever())
            .Build();
        fixture.Triggers[legacyTrigger.Key] = legacyTrigger;
        var sut = BuildService(dbName, fixture.Scheduler.Object);

        await sut.SynchronizeTaskAsync(TaskId);

        fixture.Jobs.Should().ContainSingle();
        var job = fixture.Jobs.Single().Value;
        job.Key.Should().Be(new JobKey($"job:{TaskId}:achcol:{HouseId}:cycle:{C1ConfigId}:outbound", "db-tasks"));
        job.JobType.Should().Be(typeof(NonConcurrentDynamicJob));
        job.RequestsRecovery.Should().BeTrue();
        fixture.Jobs.Keys.Should().NotContain(new JobKey($"job:{TaskId}", "db-tasks"));

        fixture.Triggers.Should().ContainSingle();
        var trigger = fixture.Triggers.Single().Value;
        trigger.Key.Should().Be(new TriggerKey($"trg:{TaskId}:achcol:{HouseId}:cycle:{C1ConfigId}:outbound", "db-tasks"));
        trigger.JobKey.Should().Be(job.Key);
        trigger.MisfireInstruction.Should().Be(MisfireInstruction.CronTrigger.DoNothing);
        trigger.JobDataMap.GetString("Flow").Should().Be("outbound");
        trigger.Should().BeAssignableTo<ICronTrigger>();
        ((ICronTrigger)trigger).CronExpressionString.Should().Be("0 30 6 * * ?");
        ((ICronTrigger)trigger).TimeZone.Id.Should().Be(RegulatoryCycleScheduleCatalog.BogotaTimeZoneId);

        await using var db = new AchDbContext(BuildOptions(dbName));
        var regulation = await db.ClearingHouseCycleConfigs.SingleAsync(x => x.Id == C1ConfigId);
        regulation.StartTime.Should().Be(new TimeSpan(19, 1, 0));
        regulation.CutoffTime.Should().Be(new TimeSpan(7, 0, 0));
    }

    [Fact]
    public async Task WindowWithoutStart_ShouldNotInventTrigger_EvenWhenLatestStartIsKnown()
    {
        var dbName = nameof(WindowWithoutStart_ShouldNotInventTrigger_EvenWhenLatestStartIsKnown);
        await SeedAsync(dbName, C7ConfigId, new TimeSpan(16, 1, 0), new TimeSpan(18, 0, 0), null, new TimeSpan(17, 50, 0),
            AchColombiaOutboundExecutionStrategy.Window);
        var fixture = QuartzFixture.Create();
        var sut = BuildService(dbName, fixture.Scheduler.Object);

        await sut.SynchronizeTaskAsync(TaskId);

        fixture.Jobs.Should().BeEmpty();
        fixture.Triggers.Should().BeEmpty();

        await using var db = new AchDbContext(BuildOptions(dbName));
        var regulation = await db.ClearingHouseCycleConfigs.SingleAsync(x => x.Id == C7ConfigId);
        regulation.CutoffTime.Should().Be(new TimeSpan(18, 0, 0));
    }

    [Theory]
    [InlineData(702)]
    [InlineData(703)]
    [InlineData(704)]
    [InlineData(705)]
    [InlineData(706)]
    public async Task C2ToC6_WithoutOperationalStart_ShouldNotCreateAutomaticTrigger(int cycleConfigId)
    {
        var dbName = $"{nameof(C2ToC6_WithoutOperationalStart_ShouldNotCreateAutomaticTrigger)}-{cycleConfigId}";
        await SeedAsync(dbName, cycleConfigId, new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0), null, null,
            AchColombiaOutboundExecutionStrategy.Window);
        var fixture = QuartzFixture.Create();
        var sut = BuildService(dbName, fixture.Scheduler.Object);

        await sut.SynchronizeTaskAsync(TaskId);

        fixture.Jobs.Should().BeEmpty();
        fixture.Triggers.Should().BeEmpty();
        await using var db = new AchDbContext(BuildOptions(dbName));
        var operational = await db.AchColombiaCycleOperationalSchedules.AsNoTracking().SingleAsync();
        operational.AutomaticOutboundStartTime.Should().BeNull();
        operational.AutomaticOutboundLatestStartTime.Should().BeNull();
    }

    [Fact]
    public async Task Resync_ShouldReplaceSameDeterministicIdentity_AndDisableShouldRemoveIt()
    {
        var dbName = nameof(Resync_ShouldReplaceSameDeterministicIdentity_AndDisableShouldRemoveIt);
        await SeedAsync(dbName, C1ConfigId, new TimeSpan(19, 1, 0), new TimeSpan(7, 0, 0), new TimeSpan(6, 30, 0), null,
            AchColombiaOutboundExecutionStrategy.ExactTime);
        var fixture = QuartzFixture.Create();
        var sut = BuildService(dbName, fixture.Scheduler.Object);

        await sut.SynchronizeTaskAsync(TaskId);
        var originalJobKey = fixture.Jobs.Keys.Single();
        var originalTriggerKey = fixture.Triggers.Keys.Single();

        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            var schedule = await db.AchColombiaCycleOperationalSchedules.SingleAsync();
            schedule.AutomaticOutboundStartTime = new TimeSpan(6, 35, 0);
            await db.SaveChangesAsync();
        }

        await sut.SynchronizeTaskAsync(TaskId);
        fixture.Jobs.Should().ContainSingle();
        fixture.Triggers.Should().ContainSingle();
        fixture.Jobs.Keys.Single().Should().Be(originalJobKey);
        fixture.Triggers.Keys.Single().Should().Be(originalTriggerKey);
        ((ICronTrigger)fixture.Triggers.Values.Single()).CronExpressionString.Should().Be("0 35 6 * * ?");

        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            var schedule = await db.AchColombiaCycleOperationalSchedules.SingleAsync();
            schedule.IsActive = false;
            await db.SaveChangesAsync();
        }

        await sut.SynchronizeTaskAsync(TaskId);
        fixture.Jobs.Should().BeEmpty();
        fixture.Triggers.Should().BeEmpty();
    }

    [Fact]
    public async Task FullReconciliationAfterRestart_ShouldRebuildSameDeterministicIdentity()
    {
        var dbName = nameof(FullReconciliationAfterRestart_ShouldRebuildSameDeterministicIdentity);
        await SeedAsync(dbName, C1ConfigId, new TimeSpan(19, 1, 0), new TimeSpan(7, 0, 0), new TimeSpan(6, 30, 0), null,
            AchColombiaOutboundExecutionStrategy.ExactTime);

        var first = QuartzFixture.Create();
        var firstService = BuildService(dbName, first.Scheduler.Object);
        await InvokeFullReconciliationAsync(firstService, first.Scheduler.Object);

        var restarted = QuartzFixture.Create();
        var restartedService = BuildService(dbName, restarted.Scheduler.Object);
        await InvokeFullReconciliationAsync(restartedService, restarted.Scheduler.Object);

        restarted.Jobs.Keys.Should().BeEquivalentTo(first.Jobs.Keys);
        restarted.Triggers.Keys.Should().BeEquivalentTo(first.Triggers.Keys);
        restarted.Jobs.Values.Single().JobType.Should().Be(typeof(NonConcurrentDynamicJob));
    }

    private static DbContextOptions<AchDbContext> BuildOptions(string dbName)
        => new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(dbName).Options;

    private static SchedulerSyncService BuildService(string dbName, IScheduler scheduler)
    {
        var provider = new ServiceCollection()
            .AddDbContext<AchDbContext>(o => o.UseInMemoryDatabase(dbName), ServiceLifetime.Scoped)
            .BuildServiceProvider();
        var factory = new Mock<ISchedulerFactory>();
        factory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(scheduler);
        return new SchedulerSyncService(provider, factory.Object, NullLogger<SchedulerSyncService>.Instance, new QuartzTaskCalendarEvaluator());
    }

    private static async Task SeedAsync(
        string dbName,
        int cycleConfigId,
        TimeSpan regulatoryStart,
        TimeSpan regulatoryCutoff,
        TimeSpan? operationalStart,
        TimeSpan? operationalLatest,
        AchColombiaOutboundExecutionStrategy strategy)
    {
        await using var db = new AchDbContext(BuildOptions(dbName));
        db.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = HouseConfigId,
            ClearingHouseId = HouseId,
            TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId
        });
        db.ClearingHouses.Add(new ClearingHouse
        {
            Id = HouseId,
            ClearingHouseId = HouseConfigId,
            Code = RegulatoryCycleScheduleCatalog.AchColombiaCode,
            Name = "ACH Colombia",
            OriginCode = "0000073",
            IsActive = true
        });
        db.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            Id = cycleConfigId,
            ClearingHouseId = HouseId,
            PolicyVersion = "TEST-ACTIVE",
            CycleName = $"Ciclo {cycleConfigId % 100}",
            StartTime = regulatoryStart,
            EndTime = regulatoryCutoff,
            CutoffTime = regulatoryCutoff,
            OutputReleaseTime = regulatoryCutoff,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.Date.AddYears(-1)
        });
        db.AchColombiaCycleOperationalSchedules.Add(new AchColombiaCycleOperationalSchedule
        {
            Id = cycleConfigId,
            ClearingHouseId = HouseId,
            ClearingHouseCycleConfigId = cycleConfigId,
            AutomaticOutboundEnabled = true,
            AutomaticOutboundStartTime = operationalStart,
            AutomaticOutboundLatestStartTime = operationalLatest,
            OutboundExecutionStrategy = strategy,
            EffectiveTimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            IsActive = true
        });
        db.TaskDefinitions.Add(new TaskDefinition
        {
            Id = TaskId,
            Code = "AchColombiaManagedMftOutbound",
            Name = "ACH Colombia MFT outbound",
            Status = TaskStatusEnum.Enabled,
            ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
            PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
            N = 5,
            RequestsRecovery = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task InvokeFullReconciliationAsync(SchedulerSyncService service, IScheduler scheduler)
    {
        var method = typeof(SchedulerSyncService).GetMethod("ReconcileAllTasksAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler, CancellationToken.None })!;
    }

    private sealed record QuartzFixture(
        Mock<IScheduler> Scheduler,
        Dictionary<JobKey, IJobDetail> Jobs,
        Dictionary<TriggerKey, ITrigger> Triggers)
    {
        public static QuartzFixture Create()
        {
            var jobs = new Dictionary<JobKey, IJobDetail>();
            var triggers = new Dictionary<TriggerKey, ITrigger>();
            var scheduler = new Mock<IScheduler>();

            scheduler.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((JobKey key, CancellationToken _) => jobs.ContainsKey(key));
            scheduler.Setup(x => x.GetJobDetail(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((JobKey key, CancellationToken _) => jobs.TryGetValue(key, out var job) ? job : null);
            scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => jobs.Keys.ToHashSet());
            scheduler.Setup(x => x.ScheduleJob(
                    It.IsAny<IJobDetail>(),
                    It.IsAny<IReadOnlyCollection<ITrigger>>(),
                    true,
                    It.IsAny<CancellationToken>()))
                .Callback<IJobDetail, IReadOnlyCollection<ITrigger>, bool, CancellationToken>((job, newTriggers, _, _) =>
                {
                    jobs[job.Key] = job;
                    foreach (var trigger in triggers.Values.Where(x => x.JobKey == job.Key).ToArray())
                    {
                        triggers.Remove(trigger.Key);
                    }
                    foreach (var trigger in newTriggers)
                    {
                        triggers[trigger.Key] = trigger;
                    }
                })
                .Returns(Task.CompletedTask);
            scheduler.Setup(x => x.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((JobKey key, CancellationToken _) =>
                {
                    foreach (var trigger in triggers.Values.Where(x => x.JobKey == key).ToArray())
                    {
                        triggers.Remove(trigger.Key);
                    }
                    return jobs.Remove(key);
                });
            scheduler.Setup(x => x.PauseJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new QuartzFixture(scheduler, jobs, triggers);
        }
    }
}
