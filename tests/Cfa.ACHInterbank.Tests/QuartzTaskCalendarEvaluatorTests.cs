using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public class QuartzTaskCalendarEvaluatorTests
{
    private static AchDbContext BuildDb(string name)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AchDbContext(options);
    }

    [Fact]
    public void QuartzTaskCalendarEvaluator_ShouldCalculateNextBusinessDaySkippingWeekendAndHoliday()
    {
        using var db = BuildDb(nameof(QuartzTaskCalendarEvaluator_ShouldCalculateNextBusinessDaySkippingWeekendAndHoliday));
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
        using var db = BuildDb(nameof(QuartzTaskCalendarEvaluator_ShouldUseTaskPreferredTime_WhenAvailable));
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
        using var db = BuildDb(nameof(DynamicJob_ShouldFallbackToBogota_WhenTimeZoneInvalid));
        var evaluator = new QuartzTaskCalendarEvaluator();
        var task = new TaskDefinition { TimeZoneId = "Invalid/TZ", CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar };

        var result = evaluator.Evaluate(task, db, DateTimeOffset.UtcNow, NullLogger.Instance);

        result.ShouldRun.Should().BeTrue();
        result.LocalDate.Should().NotBe(default);
    }

    [Fact]
    public async Task DynamicJob_ShouldSkipHoliday_WhenCalendarPolicySkipHolidays()
    {
        using var db = BuildDb(nameof(DynamicJob_ShouldSkipHoliday_WhenCalendarPolicySkipHolidays));
        var bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        var utcNow = DateTimeOffset.UtcNow;
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, bogota).DateTime);

        db.TaskDefinitions.Add(new TaskDefinition { Id = 101, Code = "NoHandler", Name = "Task", CalendarPolicy = CalendarPolicyEnum.SkipHolidays, TimeZoneId = "America/Bogota" });
        db.BankHolidays.Add(new BankHolidayModel { Date = localDate, Description = "Festivo" });
        db.SaveChanges();

        var sp = new ServiceCollection()
            .AddScoped(_ => db)
            .BuildServiceProvider();

        var job = new DynamicJob(sp, NullLogger<DynamicJob>.Instance, Array.Empty<Cfa.ACHInterbank.Application.JobsQuartz.Interfaces.ITaskHandler>(), new QuartzTaskCalendarEvaluator());

        var ctx = new Mock<IJobExecutionContext>();
        ctx.SetupGet(c => c.MergedJobDataMap).Returns(new JobDataMap { { "TaskId", 101 } });
        ctx.SetupGet(c => c.ScheduledFireTimeUtc).Returns(DateTimeOffset.UtcNow);
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await job.Execute(ctx.Object);

        var log = db.TaskExecutionLogs.OrderByDescending(x => x.Id).First();
        log.Success.Should().BeTrue();
        log.Output.Should().Be("Saltada por política SkipHolidays.");
    }
}
