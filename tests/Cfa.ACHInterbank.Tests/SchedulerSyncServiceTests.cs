using System.Reflection;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using Quartz.Impl.Matchers;

public class SchedulerSyncServiceTests
{
    private static DbContextOptions<AchDbContext> BuildOptions(string dbName)
        => new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(dbName).Options;

    private static ServiceProvider BuildProvider(string dbName)
        => new ServiceCollection().AddDbContext<AchDbContext>(o => o.UseInMemoryDatabase(dbName), ServiceLifetime.Scoped).BuildServiceProvider();

    private static SchedulerSyncService BuildService(string dbName, IScheduler scheduler)
    {
        var schedulerFactory = new Mock<ISchedulerFactory>();
        schedulerFactory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(scheduler);
        return new SchedulerSyncService(BuildProvider(dbName), schedulerFactory.Object, NullLogger<SchedulerSyncService>.Instance, new QuartzTaskCalendarEvaluator());
    }

    [Fact]
    public void SchedulerSyncService_ShouldNotThrow_WhenTimeZoneInvalid()
    {
        var service = BuildService(nameof(SchedulerSyncService_ShouldNotThrow_WhenTimeZoneInvalid), Mock.Of<IScheduler>());
        var task = new TaskDefinition { Id = 1, Code = "Test", TimeZoneId = "Invalid/TZ", PeriodicityType = PeriodicityTypeEnum.Cron, CronExpression = "0 0 12 * * ?" };
        var method = typeof(SchedulerSyncService).GetMethod("BuildTrigger", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var action = () => method.Invoke(service, new object[] { task, new TriggerKey("trg:1", "db-tasks") });
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(SchedulerMisfirePolicy.DoNothing, MisfireInstruction.CronTrigger.DoNothing)]
    [InlineData(SchedulerMisfirePolicy.FireAndProceed, MisfireInstruction.CronTrigger.FireOnceNow)]
    public void SchedulerSyncService_ShouldApplyExplicitCronMisfirePolicy(
        SchedulerMisfirePolicy policy,
        int expectedInstruction)
    {
        var service = BuildService(nameof(SchedulerSyncService_ShouldApplyExplicitCronMisfirePolicy) + policy, Mock.Of<IScheduler>());
        var task = new TaskDefinition
        {
            Id = 91,
            Code = "Misfire",
            TimeZoneId = "America/Bogota",
            PeriodicityType = PeriodicityTypeEnum.Cron,
            CronExpression = "0 30 6 ? * MON-FRI",
            MisfirePolicy = policy
        };
        var method = typeof(SchedulerSyncService).GetMethod("BuildTrigger", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var trigger = (ITrigger)method.Invoke(service, new object[] { task, new TriggerKey("trg:91", "db-tasks") })!;

        trigger.MisfireInstruction.Should().Be(expectedInstruction);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SchedulerSyncService_ShouldOnlyRequestRecoveryWhenTaskAllowsIt(bool requestsRecovery)
    {
        var dbName = nameof(SchedulerSyncService_ShouldOnlyRequestRecoveryWhenTaskAllowsIt) + requestsRecovery;
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition
            {
                Id = 92,
                Code = "Recovery",
                Name = "Recovery",
                Status = TaskStatusEnum.Enabled,
                PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
                N = 5,
                RequestsRecovery = requestsRecovery
            });
            await db.SaveChangesAsync();
        }

        IJobDetail? scheduledJob = null;
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>()))
            .Callback<IJobDetail, IReadOnlyCollection<ITrigger>, bool, CancellationToken>((job, _, _, _) => scheduledJob = job)
            .Returns(Task.CompletedTask);
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<JobKey>());

        var service = BuildService(dbName, scheduler.Object);
        var method = typeof(SchedulerSyncService).GetMethod("ReconcileAllTasksAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;

        scheduledJob.Should().NotBeNull();
        scheduledJob!.RequestsRecovery.Should().Be(requestsRecovery);
    }

    [Fact]
    public async Task SchedulerSyncService_ShouldScheduleAllTasks_OnFirstSync()
    {
        var dbName = nameof(SchedulerSyncService_ShouldScheduleAllTasks_OnFirstSync);
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 1, Code = "A", Name = "A", Status = TaskStatusEnum.Enabled, PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 5 });
            db.TaskDefinitions.Add(new TaskDefinition { Id = 2, Code = "B", Name = "B", Status = TaskStatusEnum.Enabled, PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 5 });
            await db.SaveChangesAsync();
        }

        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<JobKey>());

        var service = BuildService(dbName, scheduler.Object);
        var method = typeof(SchedulerSyncService).GetMethod("SyncOnceAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;

        scheduler.Verify(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SchedulerSyncService_ShouldNotAdvanceWatermark_WhenSyncFailsCompletely()
    {
        var dbName = nameof(SchedulerSyncService_ShouldNotAdvanceWatermark_WhenSyncFailsCompletely);
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var service = BuildService(dbName, scheduler.Object);
        var lastSyncField = typeof(SchedulerSyncService).GetField("_lastSync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var initial = (DateTimeOffset)lastSyncField.GetValue(service)!;
        var method = typeof(SchedulerSyncService).GetMethod("SyncOnceAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        Func<Task> action = async () => await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;
        await action.Should().ThrowAsync<Exception>();
        ((DateTimeOffset)lastSyncField.GetValue(service)!).Should().Be(initial);
    }

    [Fact]
    public async Task SchedulerSyncService_ShouldContinue_WhenOneTaskFails()
    {
        var dbName = nameof(SchedulerSyncService_ShouldContinue_WhenOneTaskFails);
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 1, Code = "Bad", Name = "Bad", Status = TaskStatusEnum.Enabled, PeriodicityType = PeriodicityTypeEnum.Cron, CronExpression = "invalid cron" });
            db.TaskDefinitions.Add(new TaskDefinition { Id = 2, Code = "Good", Name = "Good", Status = TaskStatusEnum.Enabled, PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 5 });
            await db.SaveChangesAsync();
        }

        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<JobKey>());

        var service = BuildService(dbName, scheduler.Object);
        var method = typeof(SchedulerSyncService).GetMethod("SyncOnceAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;

        scheduler.Verify(x => x.ScheduleJob(It.Is<IJobDetail>(j => j.Key.Name == "job:2"), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SchedulerSyncService_ShouldDeleteDisabledTaskJob_DuringReconciliation()
    {
        var dbName = nameof(SchedulerSyncService_ShouldDeleteDisabledTaskJob_DuringReconciliation);
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 3, Code = "Disabled", Name = "Disabled", Status = TaskStatusEnum.Disabled, PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 5 });
            await db.SaveChangesAsync();
        }

        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<JobKey> { new("job:3", "db-tasks") });

        var service = BuildService(dbName, scheduler.Object);
        var method = typeof(SchedulerSyncService).GetMethod("ReconcileAllTasksAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;

        scheduler.Verify(x => x.DeleteJob(It.Is<JobKey>(k => k.Name == "job:3"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SchedulerSyncService_ShouldDeleteExpiredTaskJob_DuringReconciliation()
    {
        var dbName = nameof(SchedulerSyncService_ShouldDeleteExpiredTaskJob_DuringReconciliation);
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 4, Code = "Expired", Name = "Expired", Status = TaskStatusEnum.Enabled, EndAt = DateTimeOffset.UtcNow.AddMinutes(-1), PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 5 });
            await db.SaveChangesAsync();
        }

        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<JobKey> { new("job:4", "db-tasks") });

        var service = BuildService(dbName, scheduler.Object);
        var method = typeof(SchedulerSyncService).GetMethod("ReconcileAllTasksAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;

        scheduler.Verify(x => x.DeleteJob(It.Is<JobKey>(k => k.Name == "job:4"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SchedulerSyncService_ShouldDeleteOrphanJob_WhenTaskDeletedFromDatabase()
    {
        var dbName = nameof(SchedulerSyncService_ShouldDeleteOrphanJob_WhenTaskDeletedFromDatabase);
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 6, Code = "Alive", Name = "Alive", Status = TaskStatusEnum.Enabled, PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 5 });
            await db.SaveChangesAsync();
        }

        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<JobKey> { new("job:5", "db-tasks"), new("job:6", "db-tasks") });
        scheduler.Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = BuildService(dbName, scheduler.Object);
        var method = typeof(SchedulerSyncService).GetMethod("ReconcileAllTasksAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;

        scheduler.Verify(x => x.DeleteJob(It.Is<JobKey>(k => k.Name == "job:5"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SchedulerSyncService_ShouldRecreateMissingQuartzJob_WhenTaskExistsInDatabase()
    {
        var dbName = nameof(SchedulerSyncService_ShouldRecreateMissingQuartzJob_WhenTaskExistsInDatabase);
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 7, Code = "Recreate", Name = "Recreate", Status = TaskStatusEnum.Enabled, PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 5 });
            await db.SaveChangesAsync();
        }

        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<JobKey>());
        scheduler.Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = BuildService(dbName, scheduler.Object);
        var method = typeof(SchedulerSyncService).GetMethod("ReconcileAllTasksAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;

        scheduler.Verify(x => x.ScheduleJob(It.Is<IJobDetail>(j => j.Key.Name == "job:7"), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SchedulerSyncService_ShouldNotRequireManualSchedulerStart()
    {
        var probe = new DirectoryInfo(AppContext.BaseDirectory);
        while (probe is not null && !Directory.Exists(Path.Combine(probe.FullName, "src")))
        {
            probe = probe.Parent;
        }

        probe.Should().NotBeNull();
        var path = Path.Combine(probe!.FullName, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Quartz", "SchedulerSyncService.cs");
        var content = File.ReadAllText(path);

        content.Should().NotContain("scheduler.Start(");
    }

    [Fact]
    public void QuartzShiftPolicy_ShouldNotUseRescheduleJob_ForShiftToNextBusinessDay()
    {
        var probe = new DirectoryInfo(AppContext.BaseDirectory);
        while (probe is not null && !Directory.Exists(Path.Combine(probe.FullName, "src")))
        {
            probe = probe.Parent;
        }

        probe.Should().NotBeNull();
        var files = new[]
        {
            Path.Combine(probe!.FullName, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Quartz", "Calendar", "QuartzTaskCalendarEvaluator.cs"),
            Path.Combine(probe.FullName, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Quartz", "Jobs", "DynamicJobExecutor.cs"),
            Path.Combine(probe.FullName, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Quartz", "SchedulerSyncService.cs")
        };

        var contents = files.Select(File.ReadAllText).ToList();

        contents.Should().OnlyContain(c => !c.Contains("RescheduleJob"));
        contents.Should().OnlyContain(c => !c.Contains("shifted:"));
        contents.Should().Contain(c => c.Contains("ShiftToNextBusinessDay"));
    }
}
