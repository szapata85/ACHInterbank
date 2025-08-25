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
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<SchedulerSyncService> _logger;

    private DateTimeOffset _lastSync = DateTimeOffset.MinValue;

    public SchedulerSyncService(IServiceProvider sp, ISchedulerFactory schedulerFactory, ILogger<SchedulerSyncService> logger)
    {
        _sp = sp;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
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

                // 👇 Solo traer las tareas modificadas desde la última resincronización
                var changedTasks = await db.TaskDefinitions
                    .Include(t => t.Parameters)
                    .Where(t => t.UpdatedAt > _lastSync)
                    .ToListAsync(stoppingToken);

                foreach (var task in changedTasks)
                {
                    var jobKey = new JobKey($"job:{task.Id}", "db-tasks");
                    var triggerKey = new TriggerKey($"trg:{task.Id}", "db-tasks");

                    var job = JobBuilder.Create<DynamicJob>()
                        .WithIdentity(jobKey)
                        .UsingJobData("TaskId", task.Id)
                        .Build();

                    var trigger = BuildTrigger(task, triggerKey);

                    // 👇 si ya existe, Quartz reemplaza el trigger
                    if (trigger != null)
                    {
                        await scheduler.ScheduleJob(job, new[] { trigger }, true, stoppingToken);

                        _lastSync = DateTimeOffset.UtcNow;

                        _logger.LogInformation("Resincronizada tarea {Code} ({Id}) modificada en {UpdatedAt}",
                        task.Code, task.Id, task.UpdatedAt);
                    }
                }

                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resincronizando tareas");
            }

            // ⏱ cada minuto (puedes ajustar a segundos o más tiempo)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private ITrigger? BuildTrigger(TaskDefinition task, TriggerKey triggerKey)
    {
        // 1) Validaciones generales
        var nowUtc = DateTimeOffset.UtcNow;

        // Si EndAt ya venció, no programamos nada
        if (task.EndAt.HasValue && task.EndAt.Value.ToUniversalTime() <= nowUtc)
        {
            _logger.LogInformation("Task {Id}/{Code} omitida: EndAt ({EndAt}) ya expiró.",
                task.Id, task.Code, task.EndAt);
            return null;
        }

        // Zona horaria (default America/Bogota)
        var tz = TimeZoneInfo.FindSystemTimeZoneById(task.TimeZoneId ?? "America/Bogota");

        // 2) TriggerBuilder base con Start/End
        var tb = TriggerBuilder.Create().WithIdentity(triggerKey);

        if (task.StartAt.HasValue)
            tb.StartAt(task.StartAt.Value);
        else
            tb.StartNow();

        if (task.EndAt.HasValue)
            tb.EndAt(task.EndAt.Value);

        // 3) Selección de periodicidad
        switch (task.PeriodicityType)
        {
            case PeriodicityTypeEnum.Once:
                // Ejecuta una sola vez en StartAt; si no hay StartAt, ahora.
                return tb.WithSimpleSchedule(s => s.WithRepeatCount(0)).Build();

            case PeriodicityTypeEnum.EveryNMinutes:
                if (!(task.N is int n) || n <= 0)
                {
                    _logger.LogWarning("Task {Id}/{Code}: N inválido para EveryNMinutes.", task.Id, task.Code);
                    return null;
                }
                return tb.WithSimpleSchedule(s =>
                        s.WithIntervalInMinutes(n)
                         .RepeatForever())
                         .Build();

            case PeriodicityTypeEnum.HourlyAtMinute:
                // Dispara cada hora en el minuto indicado
                if (!(task.Minute is int minute) || minute < 0 || minute > 59)
                {
                    _logger.LogWarning("Task {Id}/{Code}: Minute inválido para HourlyAtMinute.", task.Id, task.Code);
                    return null;
                }
                // Cron: segundo 0, minuto = {minute}, todas las horas
                return tb.WithSchedule(CronScheduleBuilder
                        .CronSchedule($"0 {minute} * * * ?")
                        .InTimeZone(tz))
                    .Build();

            case PeriodicityTypeEnum.DailyAtTime:
                if (!(task.TimeOfDay is TimeOnly tod))
                {
                    _logger.LogWarning("Task {Id}/{Code}: TimeOfDay requerido para DailyAtTime.", task.Id, task.Code);
                    return null;
                }
                return tb.WithSchedule(CronScheduleBuilder
                        .DailyAtHourAndMinute(tod.Hour, tod.Minute)
                        .InTimeZone(tz))
                    .Build();

            case PeriodicityTypeEnum.Weekly:
                // Requiere WeeklyDay y TimeOfDay
                if (!(task.WeeklyDay is DayOfWeek wday) || !(task.TimeOfDay is TimeOnly wtod))
                {
                    _logger.LogWarning("Task {Id}/{Code}: WeeklyDay y TimeOfDay requeridos para Weekly.", task.Id, task.Code);
                    return null;
                }
                return tb.WithSchedule(CronScheduleBuilder
                        .WeeklyOnDayAndHourAndMinute(wday, wtod.Hour, wtod.Minute)
                        .InTimeZone(tz))
                    .Build();

            case PeriodicityTypeEnum.Monthly:
                // Requiere MonthDay (1..31) y TimeOfDay
                if (!(task.MonthDay is int mday) || mday < 1 || mday > 31 || !(task.TimeOfDay is TimeOnly mtod))
                {
                    _logger.LogWarning("Task {Id}/{Code}: MonthDay(1..31) y TimeOfDay requeridos para Monthly.", task.Id, task.Code);
                    return null;
                }
                return tb.WithSchedule(CronScheduleBuilder
                        .MonthlyOnDayAndHourAndMinute(mday, mtod.Hour, mtod.Minute)
                        .InTimeZone(tz))
                    .Build();

            case PeriodicityTypeEnum.Cron:
                if (string.IsNullOrWhiteSpace(task.CronExpression))
                {
                    _logger.LogWarning("Task {Id}/{Code}: CronExpression requerido para Cron.", task.Id, task.Code);
                    return null;
                }
                return tb.WithSchedule(CronScheduleBuilder
                        .CronSchedule(task.CronExpression)
                        .InTimeZone(tz))
                    .Build();

            default:
                _logger.LogWarning("Task {Id}/{Code}: PeriodicityType no soportado.", task.Id, task.Code);
                return null;
        }
    }
}
