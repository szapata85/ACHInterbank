using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public class DynamicJobRetryTests
{
    private static DbContextOptions<AchDbContext> BuildOptions(string name)
        => new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

    private static ServiceProvider BuildProvider(string databaseName)
        => new ServiceCollection()
            .AddDbContext<AchDbContext>(o => o.UseInMemoryDatabase(databaseName), ServiceLifetime.Scoped)
            .BuildServiceProvider();

    private static async Task<TaskExecutionLog> ExecuteJobAndGetLogAsync(string databaseName, int taskId, params ITaskHandler[] handlers)
    {
        using var services = BuildProvider(databaseName);
        using (var scope = services.CreateScope())
        {
            var now = new DateTimeOffset(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);
            var clock = new FixedTimeProvider(now, TimeZoneInfo.Utc);
            var executor = new DynamicJobExecutor(scope.ServiceProvider, NullLogger<DynamicJobExecutor>.Instance, handlers, new QuartzTaskCalendarEvaluator(), new ConfigurationBuilder().Build(), clock);
            var job = new DynamicJob(executor);
            var ctx = new Mock<IJobExecutionContext>();
            ctx.SetupGet(c => c.MergedJobDataMap).Returns(new JobDataMap { { "TaskId", taskId } });
            ctx.SetupGet(c => c.ScheduledFireTimeUtc).Returns(now);
            ctx.SetupGet(c => c.FireTimeUtc).Returns(now);
            ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
            await job.Execute(ctx.Object);
        }

        await using var assertDb = new AchDbContext(BuildOptions(databaseName));
        return assertDb.TaskExecutionLogs.OrderByDescending(x => x.Id).First();
    }

    [Fact]
    public async Task DynamicJob_ShouldNotRetry_WhenHandlerMissing()
    {
        var dbName = nameof(DynamicJob_ShouldNotRetry_WhenHandlerMissing);
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 1, Code = "Missing", Name = "Missing", CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar, RetryOnFailure = true, MaxRetries = 3 });
            await db.SaveChangesAsync();
        }

        var log = await ExecuteJobAndGetLogAsync(dbName, 1);
        log.Success.Should().BeFalse();
        log.Error.Should().Be("No hay handler implementado para Missing");
    }

    [Fact]
    public async Task DynamicJob_ShouldNotRetry_WhenRetryOnFailureFalse()
    {
        var dbName = nameof(DynamicJob_ShouldNotRetry_WhenRetryOnFailureFalse);
        var handler = new AlwaysFailingHandler("RetryFalse", new InvalidOperationException("fail"));

        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 2, Code = "RetryFalse", Name = "RetryFalse", CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar, RetryOnFailure = false, MaxRetries = 3, RetryBackoffSeconds = 0 });
            await db.SaveChangesAsync();
        }

        var log = await ExecuteJobAndGetLogAsync(dbName, 2, handler);
        handler.Attempts.Should().Be(1);
        log.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DynamicJob_ShouldRetryUntilSuccess_WhenRetryEnabled()
    {
        var dbName = nameof(DynamicJob_ShouldRetryUntilSuccess_WhenRetryEnabled);
        var handler = new FailingThenSuccessHandler("RetrySuccess", failuresBeforeSuccess: 2, successOutput: "ok");

        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 3, Code = "RetrySuccess", Name = "RetrySuccess", CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar, RetryOnFailure = true, MaxRetries = 2, RetryBackoffSeconds = 0 });
            await db.SaveChangesAsync();
        }

        var log = await ExecuteJobAndGetLogAsync(dbName, 3, handler);
        handler.Attempts.Should().Be(3);
        log.Success.Should().BeTrue();
        log.Output.Should().Contain("intento 3/3");
    }

    [Fact]
    public async Task DynamicJob_ShouldFail_WhenRetriesExhausted()
    {
        var dbName = nameof(DynamicJob_ShouldFail_WhenRetriesExhausted);
        var handler = new AlwaysFailingHandler("RetryFail", new InvalidOperationException("boom"));

        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 4, Code = "RetryFail", Name = "RetryFail", CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar, RetryOnFailure = true, MaxRetries = 2, RetryBackoffSeconds = 0 });
            await db.SaveChangesAsync();
        }

        var log = await ExecuteJobAndGetLogAsync(dbName, 4, handler);
        handler.Attempts.Should().Be(3);
        log.Success.Should().BeFalse();
        log.Error.Should().Contain("Reintentos agotados tras 3 intento(s)");
        log.Error.Should().Contain("boom");
    }

    [Fact]
    public async Task DynamicJob_ShouldPassOperationalTriggerMetadataToContextAwareHandler()
    {
        var dbName = nameof(DynamicJob_ShouldPassOperationalTriggerMetadataToContextAwareHandler);
        var handler = new CapturingContextHandler("AchColombiaManagedMftOutbound");

        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition
            {
                Id = 41,
                Code = "AchColombiaManagedMftOutbound",
                Name = "ACHCOL outbound",
                CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar,
                RetryOnFailure = false
            });
            await db.SaveChangesAsync();
        }

        using var services = BuildProvider(dbName);
        using (var scope = services.CreateScope())
        {
            var scheduled = new DateTimeOffset(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);
            var clock = new FixedTimeProvider(scheduled, TimeZoneInfo.Utc);
            var executor = new DynamicJobExecutor(scope.ServiceProvider, NullLogger<DynamicJobExecutor>.Instance, new ITaskHandler[] { handler }, new QuartzTaskCalendarEvaluator(), new ConfigurationBuilder().Build(), clock);
            var job = new DynamicJob(executor);
            var ctx = new Mock<IJobExecutionContext>();
            ctx.SetupGet(c => c.MergedJobDataMap).Returns(new JobDataMap
            {
                { "TaskId", 41 },
                { "TaskCode", "AchColombiaManagedMftOutbound" },
                { "OperationalScheduleId", "901" },
                { "ClearingHouseId", "73" },
                { "CycleConfigId", "701" },
                { "Flow", "outbound" },
                { "ScheduleFingerprint", "fingerprint" },
                { "TriggerType", "AchColombiaOperationalOutbound" }
            });
            ctx.SetupGet(c => c.ScheduledFireTimeUtc).Returns(scheduled);
            ctx.SetupGet(c => c.FireTimeUtc).Returns(scheduled);
            ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

            await job.Execute(ctx.Object);

            handler.Captured.Should().NotBeNull();
            handler.Captured!.ScheduledFireTimeUtc.Should().Be(scheduled);
            handler.Captured.JobData!["OperationalScheduleId"].Should().Be("901");
            handler.Captured.JobData["ClearingHouseId"].Should().Be("73");
            handler.Captured.JobData["CycleConfigId"].Should().Be("701");
            handler.Captured.JobData["Flow"].Should().Be("outbound");
            handler.Captured.JobData["ScheduleFingerprint"].Should().Be("fingerprint");
        }
    }

    [Theory]
    [InlineData(599999999, false)]
    [InlineData(600000000, false)]
    [InlineData(600000001, true)]
    public async Task DynamicJob_MisfireDetection_UsesInjectedTimeAtExactThreshold(long latenessTicks, bool expectedMisfire)
    {
        var dbName = $"misfire-{latenessTicks}-{Guid.NewGuid():N}";
        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition
            {
                Id = 51,
                Code = "MisfireBoundary",
                Name = "MisfireBoundary",
                CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar,
                RetryOnFailure = false
            });
            await db.SaveChangesAsync();
        }

        var now = new DateTimeOffset(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);
        var scheduled = now.AddTicks(-latenessTicks);
        var clock = new FixedTimeProvider(now, TimeZoneInfo.Utc);
        var handler = new CapturingContextHandler("MisfireBoundary");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Quartz:JobStore:MisfireThresholdMilliseconds"] = "60000" })
            .Build();

        using var services = BuildProvider(dbName);
        using (var scope = services.CreateScope())
        {
            var executor = new DynamicJobExecutor(scope.ServiceProvider, NullLogger<DynamicJobExecutor>.Instance, new ITaskHandler[] { handler }, new QuartzTaskCalendarEvaluator(), configuration, clock);
            var ctx = new Mock<IJobExecutionContext>();
            ctx.SetupGet(c => c.MergedJobDataMap).Returns(new JobDataMap { { "TaskId", 51 } });
            ctx.SetupGet(c => c.ScheduledFireTimeUtc).Returns(scheduled);
            ctx.SetupGet(c => c.FireTimeUtc).Returns(now);
            ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

            await new DynamicJob(executor).Execute(ctx.Object);
        }

        await using var assertDb = new AchDbContext(BuildOptions(dbName));
        var log = await assertDb.TaskExecutionLogs.SingleAsync();
        log.MisfireDetected.Should().Be(expectedMisfire);
        handler.Captured.Should().NotBeNull();
        handler.Captured!.MisfireDetected.Should().Be(expectedMisfire);
    }

    [Fact]
    public async Task DynamicJob_ShouldTreatNullMaxRetriesAsZero()
    {
        var dbName = nameof(DynamicJob_ShouldTreatNullMaxRetriesAsZero);
        var handler = new AlwaysFailingHandler("RetryNull", new InvalidOperationException("fail"));

        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 5, Code = "RetryNull", Name = "RetryNull", CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar, RetryOnFailure = true, MaxRetries = null, RetryBackoffSeconds = 0 });
            await db.SaveChangesAsync();
        }

        await ExecuteJobAndGetLogAsync(dbName, 5, handler);
        handler.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task DynamicJob_ShouldTreatNegativeMaxRetriesAsZero()
    {
        var dbName = nameof(DynamicJob_ShouldTreatNegativeMaxRetriesAsZero);
        var handler = new AlwaysFailingHandler("RetryNegative", new InvalidOperationException("fail"));

        await using (var db = new AchDbContext(BuildOptions(dbName)))
        {
            db.TaskDefinitions.Add(new TaskDefinition { Id = 6, Code = "RetryNegative", Name = "RetryNegative", CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar, RetryOnFailure = true, MaxRetries = -1, RetryBackoffSeconds = 0 });
            await db.SaveChangesAsync();
        }

        await ExecuteJobAndGetLogAsync(dbName, 6, handler);
        handler.Attempts.Should().Be(1);
    }

    private sealed class FailingThenSuccessHandler : ITaskHandler
    {
        private readonly int _failuresBeforeSuccess;
        private readonly string _successOutput;
        public string Code { get; }
        public int Attempts { get; private set; }

        public FailingThenSuccessHandler(string code, int failuresBeforeSuccess, string successOutput)
        {
            Code = code;
            _failuresBeforeSuccess = failuresBeforeSuccess;
            _successOutput = successOutput;
        }

        public Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
        {
            Attempts++;
            if (Attempts <= _failuresBeforeSuccess)
            {
                throw new InvalidOperationException($"forced-failure-{Attempts}");
            }

            return Task.FromResult(_successOutput);
        }
    }

    private sealed class CapturingContextHandler : ISchedulerContextAwareTaskHandler
    {
        public CapturingContextHandler(string code) => Code = code;
        public string Code { get; }
        public SchedulerTaskExecutionContext? Captured { get; private set; }

        public Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Context-aware overload expected.");

        public Task<string> ExecuteAsync(TaskDefinition task, SchedulerTaskExecutionContext context, CancellationToken cancellationToken)
        {
            Captured = context;
            return Task.FromResult("ok");
        }
    }

    private sealed class AlwaysFailingHandler : ITaskHandler
    {
        private readonly Exception _exception;
        public string Code { get; }
        public int Attempts { get; private set; }

        public AlwaysFailingHandler(string code, Exception exception)
        {
            Code = code;
            _exception = exception;
        }

        public Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
        {
            Attempts++;
            throw _exception;
        }
    }
}
