using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public class QuartzTaskCalendarEvaluatorTests
{
    private static DbContextOptions<AchDbContext> BuildOptions(string name)
        => new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

    [Fact]
    public void QuartzTaskCalendarEvaluator_ShouldCalculateNextBusinessDaySkippingWeekendAndHoliday()
    {
        var options = BuildOptions(nameof(QuartzTaskCalendarEvaluator_ShouldCalculateNextBusinessDaySkippingWeekendAndHoliday));
        using var db = new AchDbContext(options);
        db.BankHolidays.Add(new BankHolidayModel { Date = new DateOnly(2026, 5, 11), Description = "Festivo" });
        db.SaveChanges();

        var evaluator = new QuartzTaskCalendarEvaluator();
        var tz = evaluator.ResolveTimeZone("America/Bogota");
        var next = evaluator.GetNextBusinessDateTime(db, new DateOnly(2026, 5, 8), new TimeOnly(8, 30), tz);

        var local = TimeZoneInfo.ConvertTime(next, tz);
        local.Date.Should().Be(new DateTime(2026, 5, 12));
        local.Hour.Should().Be(8);
        local.Minute.Should().Be(30);
    }

    [Fact]
    public void QuartzTaskCalendarEvaluator_ShouldUseTaskPreferredTime_WhenAvailable()
    {
        var options = BuildOptions(nameof(QuartzTaskCalendarEvaluator_ShouldUseTaskPreferredTime_WhenAvailable));
        using var db = new AchDbContext(options);
        var evaluator = new QuartzTaskCalendarEvaluator();
        var tz = evaluator.ResolveTimeZone("America/Bogota");

        var next = evaluator.GetNextBusinessDateTime(db, new DateOnly(2026, 5, 12), new TimeOnly(14, 45), tz);
        var local = TimeZoneInfo.ConvertTime(next, tz);

        local.Date.Should().Be(new DateTime(2026, 5, 13));
        local.Hour.Should().Be(14);
        local.Minute.Should().Be(45);
    }

    [Fact]
    public void DynamicJob_ShouldFallbackToBogota_WhenTimeZoneInvalid()
    {
        var options = BuildOptions(nameof(DynamicJob_ShouldFallbackToBogota_WhenTimeZoneInvalid));
        using var db = new AchDbContext(options);
        var evaluator = new QuartzTaskCalendarEvaluator();
        var task = new TaskDefinition { TimeZoneId = "Invalid/TZ", CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar };

        var result = evaluator.Evaluate(task, db, DateTimeOffset.UtcNow, NullLogger.Instance);

        result.ShouldRun.Should().BeTrue();
        result.LocalDate.Should().NotBe(default);
    }

    [Fact]
    public async Task DynamicJob_ShouldSkipHoliday_WhenCalendarPolicySkipHolidays()
    {
        var databaseName = nameof(DynamicJob_ShouldSkipHoliday_WhenCalendarPolicySkipHolidays);
        var options = BuildOptions(databaseName);
        var bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, bogota).DateTime);

        using (var seedDb = new AchDbContext(options))
        {
            seedDb.TaskDefinitions.Add(new TaskDefinition
            {
                Id = 101,
                Code = "NoHandler",
                Name = "Task",
                CalendarPolicy = CalendarPolicyEnum.SkipHolidays,
                TimeZoneId = "America/Bogota"
            });
            seedDb.BankHolidays.Add(new BankHolidayModel { Date = localDate, Description = "Festivo" });
            seedDb.SaveChanges();
        }

        var services = new ServiceCollection()
            .AddDbContext<AchDbContext>(o => o.UseInMemoryDatabase(databaseName), ServiceLifetime.Scoped)
            .BuildServiceProvider();

        using (var scope = services.CreateScope())
        {
            var job = new DynamicJob(
                scope.ServiceProvider,
                NullLogger<DynamicJob>.Instance,
                Array.Empty<ITaskHandler>(),
                new QuartzTaskCalendarEvaluator());

            var ctx = new Mock<IJobExecutionContext>();
            ctx.SetupGet(c => c.MergedJobDataMap).Returns(new JobDataMap { { "TaskId", 101 } });
            ctx.SetupGet(c => c.ScheduledFireTimeUtc).Returns(DateTimeOffset.UtcNow);
            ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

            await job.Execute(ctx.Object);
        }

        using var assertDb = new AchDbContext(options);
        var log = assertDb.TaskExecutionLogs.OrderByDescending(x => x.Id).First();
        log.Success.Should().BeTrue();
        log.Output.Should().Be("Saltada por política SkipHolidays.");
    }
}
