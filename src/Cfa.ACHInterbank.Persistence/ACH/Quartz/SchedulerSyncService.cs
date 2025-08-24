using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz;

public class SchedulerSyncService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ISchedulerFactory _factory;
    private readonly ILogger<SchedulerSyncService> _logger;

    public SchedulerSyncService(IServiceProvider sp, ISchedulerFactory factory, ILogger<SchedulerSyncService> logger)
    {
        _sp = sp;
        _factory = factory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scheduler = await _factory.GetScheduler(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();

            var tasks = await db.Set<TaskDefinition>()
                .Include(t => t.Parameters)
                .Where(t => t.Status == TaskStatusEnum.Enabled)
                .ToListAsync(stoppingToken);

            foreach (var task in tasks)
            {
                try
                {
                    var jobKey = new JobKey($"job:{task.Id}", "db-tasks");
                    var triggerKey = new TriggerKey($"trg:{task.Id}", "db-tasks");

                    var job = JobBuilder.Create<DynamicJob>()
                        .WithIdentity(jobKey)
                        .UsingJobData("TaskId", task.Id)
                        .Build();

                    var trigger = BuildTrigger(task, triggerKey);

                    await scheduler.ScheduleJob(job, new[] { trigger }, true, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sincronizando tarea {Id} - {Code}", task.Id, task.Code);
                }
            }

            _logger.LogInformation("Sincronizadas {Count} tareas desde BD.", tasks.Count);

            // Re-sincroniza cada 60 segundos
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    private static ITrigger BuildTrigger(TaskDefinition def, TriggerKey key)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(def.TimeZoneId ?? "America/Bogota");
        var tb = TriggerBuilder.Create().WithIdentity(key).StartNow();

        return def.PeriodicityType switch
        {
            PeriodicityTypeEnum.Once =>
                tb.StartAt(def.StartAt ?? DateTimeOffset.Now)
                  .WithSimpleSchedule(x => x.WithRepeatCount(0))
                  .Build(),

            PeriodicityTypeEnum.EveryNMinutes =>
                tb.WithSimpleSchedule(x => x.WithIntervalInMinutes(def.N ?? 5).RepeatForever())
                  .Build(),

            PeriodicityTypeEnum.HourlyAtMinute =>
                tb.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, def.Minute ?? 0)
                    .InTimeZone(tz))
                  .Build(),

            PeriodicityTypeEnum.DailyAtTime =>
                tb.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(
                        def.TimeOfDay?.Hour ?? 0, def.TimeOfDay?.Minute ?? 0)
                    .InTimeZone(tz))
                  .Build(),

            PeriodicityTypeEnum.Weekly =>
                tb.WithSchedule(CronScheduleBuilder
                        .WeeklyOnDayAndHourAndMinute(def.WeeklyDay ?? DayOfWeek.Monday,
                            def.TimeOfDay?.Hour ?? 8, def.TimeOfDay?.Minute ?? 0)
                    .InTimeZone(tz))
                  .Build(),

            PeriodicityTypeEnum.Monthly =>
                tb.WithSchedule(CronScheduleBuilder
                        .MonthlyOnDayAndHourAndMinute(def.MonthDay ?? 1,
                            def.TimeOfDay?.Hour ?? 8, def.TimeOfDay?.Minute ?? 0)
                    .InTimeZone(tz))
                  .Build(),

            PeriodicityTypeEnum.Cron when !string.IsNullOrWhiteSpace(def.CronExpression) =>
                tb.WithSchedule(CronScheduleBuilder.CronSchedule(def.CronExpression)
                    .InTimeZone(tz))
                  .Build(),

            _ => tb.WithSimpleSchedule(x => x.WithIntervalInMinutes(15).RepeatForever()).Build()
        };
    }
}

