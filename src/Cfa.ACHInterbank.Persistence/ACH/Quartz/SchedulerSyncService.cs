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
                    await scheduler.ScheduleJob(job, new[] { trigger }, true, stoppingToken);

                    _logger.LogInformation("Resincronizada tarea {Code} ({Id}) modificada en {UpdatedAt}",
                        task.Code, task.Id, task.UpdatedAt);
                }

                _lastSync = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resincronizando tareas");
            }

            // ⏱ cada minuto (puedes ajustar a segundos o más tiempo)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private ITrigger BuildTrigger(TaskDefinition task, TriggerKey triggerKey)
    {
        var builder = TriggerBuilder.Create()
            .WithIdentity(triggerKey);

        if (task.StartAt.HasValue)
            builder.StartAt(task.StartAt.Value);
        else
            builder.StartNow();

        if (task.EndAt.HasValue)
            builder.EndAt(task.EndAt.Value);

        switch (task.PeriodicityType)
        {
            case PeriodicityTypeEnum.DailyAtTime:
                if (task.TimeOfDay.HasValue)
                {
                    builder.WithSchedule(CronScheduleBuilder
                        .DailyAtHourAndMinute(task.TimeOfDay.Value.Hour, task.TimeOfDay.Value.Minute)
                        .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(task.TimeZoneId ?? "America/Bogota")));
                }
                break;

            case PeriodicityTypeEnum.EveryNMinutes:
                if (task.N.HasValue)
                {
                    builder.WithSimpleSchedule(s =>
                        s.WithIntervalInMinutes(task.N.Value)
                         .RepeatForever());
                }
                break;

            case PeriodicityTypeEnum.Cron:
                if (!string.IsNullOrEmpty(task.CronExpression))
                {
                    builder.WithCronSchedule(task.CronExpression);
                }
                break;
        }

        return builder.Build();
    }
}
