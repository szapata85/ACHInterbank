using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;

[DisallowConcurrentExecution] // respeta ConcurrencyPolicyEnum.SkipIfRunning
public class DynamicJob : IJob
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DynamicJob> _logger;
    private readonly IEnumerable<ITaskHandler> _handlers;

    public DynamicJob(IServiceProvider sp, ILogger<DynamicJob> logger, IEnumerable<ITaskHandler> handlers)
    {
        _sp = sp;
        _logger = logger;
        _handlers = handlers;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var taskId = context.MergedJobDataMap.GetInt("TaskId");

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();

        var task = await db.TaskDefinitions
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == taskId, context.CancellationToken);

        if (task is null)
        {
            _logger.LogWarning("No se encontró la tarea con Id {TaskId}", taskId);
            return;
        }

        var log = new TaskExecutionLog
        {
            TaskDefinitionId = task.Id,
            ScheduledAt = context.ScheduledFireTimeUtc ?? DateTimeOffset.Now,
            StartedAt = DateTimeOffset.UtcNow
        };

        db.TaskExecutionLogs.Add(log);
        await db.SaveChangesAsync(context.CancellationToken);

        try
        {
            // 🔎 Evaluar política de calendario antes de ejecutar
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            bool isWeekend = today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            bool isHoliday = db.BankHolidays.Any(h => h.Date == today);

            switch (task.CalendarPolicy)
            {
                case CalendarPolicyEnum.OnlyBusinessDays when (isWeekend || isHoliday):
                    log.Success = true;
                    log.Output = "Saltada por política OnlyBusinessDays.";
                    await db.SaveChangesAsync(context.CancellationToken);
                    _logger.LogInformation("Task {Id}/{Code} saltada (no es día hábil).", task.Id, task.Code);
                    return;

                case CalendarPolicyEnum.OnlyWeekends when !isWeekend:
                    log.Success = true;
                    log.Output = "Saltada por política OnlyWeekends.";
                    await db.SaveChangesAsync(context.CancellationToken);
                    _logger.LogInformation("Task {Id}/{Code} saltada (no es fin de semana).", task.Id, task.Code);
                    return;

                case CalendarPolicyEnum.SkipHolidays when isHoliday:
                    log.Success = true;
                    log.Output = "Saltada por política SkipHolidays.";
                    await db.SaveChangesAsync(context.CancellationToken);
                    _logger.LogInformation("Task {Id}/{Code} saltada (es festivo).", task.Id, task.Code);
                    return;

                case CalendarPolicyEnum.ShiftToNextBusinessDay when (isWeekend || isHoliday):
                    {
                        var nextDate = GetNextBusinessDay(db, DateTimeOffset.Now);

                        // ⚡ reprogrmar trigger dentro de Quartz
                        var scheduler = context.Scheduler;
                        var jobKey = context.JobDetail.Key;

                        var newTrigger = TriggerBuilder.Create()
                            .ForJob(jobKey)
                            .WithIdentity($"shifted:{jobKey.Name}", jobKey.Group)
                            .StartAt(nextDate)
                            .Build();

                        await scheduler.RescheduleJob(context.Trigger.Key, newTrigger, context.CancellationToken);

                        log.Success = true;
                        log.Output = $"Reprogramada automáticamente para {nextDate}.";
                        await db.SaveChangesAsync(context.CancellationToken);

                        _logger.LogInformation("Task {Id}/{Code} reprogramada automáticamente para {Next}.",
                            task.Id, task.Code, nextDate);
                        return;
                    }

                    // CalendarPolicyEnum.IgnoreCalendar -> no se filtra nada
            }

            // ✅ Ejecutar normalmente con su handler
            var handler = _handlers.FirstOrDefault(h => h.Code == task.Code);

            if (handler is null)
            {
                log.Success = false;
                log.Error = $"No hay handler implementado para {task.Code}";
            }
            else
            {
                var output = await handler.ExecuteAsync(task, context.CancellationToken);
                log.Success = true;
                log.Output = output;
            }
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.Error = ex.ToString();
        }
        finally
        {
            log.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(context.CancellationToken);
        }
    }

    // 🔧 Utilidad para calcular próximo día hábil
    private static DateTimeOffset GetNextBusinessDay(AchDbContext db, DateTimeOffset fromDate)
    {
        var next = fromDate.Date.AddDays(1);

        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
               db.BankHolidays.Any(h => h.Date == DateOnly.FromDateTime(next)))
        {
            next = next.AddDays(1);
        }

        // en este ejemplo lo fijo a 9 AM, puedes usar task.TimeOfDay si quieres respetar la hora
        return new DateTimeOffset(next, fromDate.Offset).AddHours(9);
    }
}
