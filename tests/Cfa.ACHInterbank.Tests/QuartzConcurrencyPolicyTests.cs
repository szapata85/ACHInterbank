using System.Reflection;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
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

public class QuartzConcurrencyPolicyTests
{
    private static SchedulerSyncService BuildService(string dbName, Mock<IScheduler> scheduler)
    {
        var sc = new ServiceCollection().AddDbContext<AchDbContext>(o => o.UseInMemoryDatabase(dbName), ServiceLifetime.Scoped).BuildServiceProvider();
        var factory = new Mock<ISchedulerFactory>();
        factory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(scheduler.Object);
        return new SchedulerSyncService(sc, factory.Object, NullLogger<SchedulerSyncService>.Instance, new QuartzTaskCalendarEvaluator());
    }

    [Fact]
    public void NonConcurrentDynamicJob_ShouldHaveDisallowConcurrentExecutionAttribute()
        => typeof(NonConcurrentDynamicJob).GetCustomAttribute<DisallowConcurrentExecutionAttribute>().Should().NotBeNull();

    [Fact]
    public void DynamicJob_ShouldNotHaveDisallowConcurrentExecutionAttribute()
        => typeof(DynamicJob).GetCustomAttribute<DisallowConcurrentExecutionAttribute>().Should().BeNull();

    [Theory]
    [InlineData(ConcurrencyPolicyEnum.AllowParallel, typeof(DynamicJob))]
    [InlineData(ConcurrencyPolicyEnum.SkipIfRunning, typeof(NonConcurrentDynamicJob))]
    [InlineData(ConcurrencyPolicyEnum.Queue, typeof(NonConcurrentDynamicJob))]
    public void SchedulerSyncService_ShouldMapJobType_ByConcurrencyPolicy(ConcurrencyPolicyEnum policy, Type expected)
    {
        var method = typeof(SchedulerSyncService).GetMethod("GetJobTypeForConcurrencyPolicy", BindingFlags.NonPublic | BindingFlags.Static)!;
        var type = (Type)method.Invoke(null, new object[] { policy })!;
        type.Should().Be(expected);
    }

    [Fact]
    public async Task SchedulerSyncService_ShouldReplaceJob_WhenConcurrencyPolicyChangesJobType()
    {
        var dbName = nameof(SchedulerSyncService_ShouldReplaceJob_WhenConcurrencyPolicyChangesJobType);
        await using (var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(dbName).Options))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 10, Code = "A", Name = "A", Status = TaskStatusEnum.Enabled, ConcurrencyPolicy = ConcurrencyPolicyEnum.AllowParallel, PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 5 });
            await db.SaveChangesAsync();
        }

        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.GetJobDetail(It.Is<JobKey>(k => k.Name == "job:10"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JobBuilder.Create<NonConcurrentDynamicJob>().WithIdentity("job:10", "db-tasks").Build());
        scheduler.Setup(x => x.GetJobKeys(It.IsAny<GroupMatcher<JobKey>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<JobKey>{ new("job:10","db-tasks") });
                scheduler.Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = BuildService(dbName, scheduler);
        var method = typeof(SchedulerSyncService).GetMethod("ReconcileAllTasksAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(service, new object[] { scheduler.Object, CancellationToken.None })!;

        scheduler.Verify(x => x.DeleteJob(It.Is<JobKey>(k => k.Name == "job:10"), It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(x => x.ScheduleJob(It.Is<IJobDetail>(j => j.JobType == typeof(DynamicJob)), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DynamicJob_And_NonConcurrentDynamicJob_ShouldDelegateToSameExecutor()
    {
        typeof(DynamicJob).GetConstructors().Single().GetParameters().Single().ParameterType.Should().Be(typeof(DynamicJobExecutor));
        typeof(NonConcurrentDynamicJob).GetConstructors().Single().GetParameters().Single().ParameterType.Should().Be(typeof(DynamicJobExecutor));
    }
}
