using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz;

public class SchedulerSyncService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<SchedulerSyncService> _logger;
    private readonly QuartzTaskCalendarEvaluator _calendarEvaluator;

    private DateTimeOffset _lastSync = DateTimeOffset.MinValue;

    public SchedulerSyncService(IServiceProvider sp, ISchedulerFactory schedulerFactory, ILogger<SchedulerSyncService> logger, QuartzTaskCalendarEvaluator calendarEvaluator)
    {
        _sp = sp;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
        _calendarEvaluator = calendarEvaluator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.Start(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();

                var changedTasks = await db.TaskDefinitions
                    .Include(t => t.Parameters)
                    .Where(t => t.UpdatedAt > _lastSync)
                    .ToListAsync(stoppingToken);

                foreach (var task in changedTasks)
                {
                    var jobKey = new JobKey($"job:{task.Id}", "db-tasks");
                    var triggerKey = new TriggerKey($"trg:{task.Id}", "db-tasks");

                    // 1) STATUS
                    if (task.Status == TaskStatusEnum.Disabled)
                    {
                        await scheduler.DeleteJob(jobKey, stoppingToken);
                        _logger.LogInformation("Task {Id}/{Code} deshabilitada. Eliminada del scheduler.", task.Id, task.Code);
                        continue;
                    }

                    // 2) END DATE vencido
                    if (task.EndAt.HasValue && task.EndAt.Value.ToUniversalTime() <= DateTimeOffset.UtcNow)
                    {
                        await scheduler.DeleteJob(jobKey, stoppingToken);
                        _logger.LogInformation("Task {Id}/{Code} vencida. Eliminada del scheduler.", task.Id, task.Code);
                        continue;
                    }

                    // 3) Crear job con política de concurrencia
                    var jobBuilder = JobBuilder.Create<DynamicJob>()
                        .WithIdentity(jobKey)
                        .UsingJobData("TaskId", task.Id);

                    // ConcurrencyPolicy
                    if (task.ConcurrencyPolicy == ConcurrencyPolicyEnum.SkipIfRunning)
                    {
                        // Quartz respeta esto si el job tiene [DisallowConcurrentExecution]
                        jobBuilder.StoreDurably();
                    }

                    var job = jobBuilder.Build();

                    // 4) Crear trigger según periodicidad
                    var trigger = BuildTrigger(task, triggerKey);
                    if (trigger is null)
                    {
                        await scheduler.DeleteJob(jobKey, stoppingToken);
                        continue;
                    }

                    await scheduler.ScheduleJob(job, new[] { trigger }, true, stoppingToken);
                    _logger.LogInformation("Task {Id}/{Code} sincronizada con éxito.", task.Id, task.Code);
                }

                _lastSync = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resincronizando tareas.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private ITrigger? BuildTrigger(TaskDefinition task, TriggerKey triggerKey)
    {
        if (task.StartAt.HasValue && task.EndAt.HasValue && task.StartAt >= task.EndAt)
        {
            _logger.LogWarning("La tarea {Id} tiene StartAt >= EndAt, no se programará", task.Id);
            return null!;
        }

        // Validar fechas inválidas
        if (task.EndAt.HasValue && task.EndAt.Value <= DateTimeOffset.UtcNow)
        {
            // Ya expiró, no devolver trigger
            _logger.LogWarning("La tarea {Code} ({Id}) no se programará porque EndAt ya venció ({EndAt})",
                task.Code, task.Id, task.EndAt);
            return null;
        }

        var tz = _calendarEvaluator.ResolveTimeZone(task.TimeZoneId, _logger);

        var tb = TriggerBuilder.Create().WithIdentity(triggerKey);

        if (task.StartAt.HasValue)
            tb.StartAt(task.StartAt.Value);
        else
            tb.StartNow();

        if (task.EndAt.HasValue)
            tb.EndAt(task.EndAt.Value);

        switch (task.PeriodicityType)
        {
            case PeriodicityTypeEnum.Once:
                return tb.WithSimpleSchedule(s => s.WithRepeatCount(0)).Build();

            case PeriodicityTypeEnum.EveryNMinutes:
                return tb.WithSimpleSchedule(s => s
                    .WithIntervalInMinutes(task.N ?? 1)
                    .RepeatForever()).Build();

            case PeriodicityTypeEnum.HourlyAtMinute:
                return tb.WithSchedule(CronScheduleBuilder
                        .CronSchedule($"0 {task.Minute ?? 0} * * * ?")
                        .InTimeZone(tz))
                    .Build();

            case PeriodicityTypeEnum.DailyAtTime:
                return tb.WithSchedule(CronScheduleBuilder
                        .DailyAtHourAndMinute(task.TimeOfDay!.Value.Hour, task.TimeOfDay.Value.Minute)
                        .InTimeZone(tz))
                    .Build();

            case PeriodicityTypeEnum.Weekly:
                return tb.WithSchedule(CronScheduleBuilder
                        .WeeklyOnDayAndHourAndMinute(task.WeeklyDay!.Value, task.TimeOfDay!.Value.Hour, task.TimeOfDay.Value.Minute)
                        .InTimeZone(tz))
                    .Build();

            case PeriodicityTypeEnum.Monthly:
                return tb.WithSchedule(CronScheduleBuilder
                        .MonthlyOnDayAndHourAndMinute(task.MonthDay!.Value, task.TimeOfDay!.Value.Hour, task.TimeOfDay.Value.Minute)
                        .InTimeZone(tz))
                    .Build();

            case PeriodicityTypeEnum.Cron:
                return tb.WithSchedule(CronScheduleBuilder
                        .CronSchedule(task.CronExpression!)
                        .InTimeZone(tz))
                    .Build();

            default:
                return null;
        }
    }
}
