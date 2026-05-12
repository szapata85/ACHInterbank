using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
            var job = new DynamicJob(scope.ServiceProvider, NullLogger<DynamicJob>.Instance, handlers, new QuartzTaskCalendarEvaluator());
            var ctx = new Mock<IJobExecutionContext>();
            ctx.SetupGet(c => c.MergedJobDataMap).Returns(new JobDataMap { { "TaskId", taskId } });
            ctx.SetupGet(c => c.ScheduledFireTimeUtc).Returns(DateTimeOffset.UtcNow);
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
