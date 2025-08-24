using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;

public class DynamicJob : IJob
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DynamicJob> _logger;

    public DynamicJob(IServiceProvider sp, ILogger<DynamicJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var taskId = context.MergedJobDataMap.GetInt("TaskId");
        var scheduledAt = context.ScheduledFireTimeUtc ?? DateTimeOffset.Now;

        using var scope = _sp.CreateScope();
        AchDbContext db = scope.ServiceProvider.GetRequiredService<AchDbContext>();

        var task = await db.Set<TaskDefinition>()
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
            ScheduledAt = scheduledAt.UtcDateTime,
            StartedAt = DateTimeOffset.UtcNow
        };

        db.TaskExecutionLogs.Add(log);
        await db.SaveChangesAsync(context.CancellationToken);

        try
        {
            _logger.LogInformation("Ejecutando tarea {Code} ({Name})", task.Code, task.Name);

            switch (task.Code)
            {
                case "CheckBankHolidays":
                    IBankHolidaySingleton repo = scope.ServiceProvider.GetRequiredService<IBankHolidaySingleton>();
                    int year = DateTime.Now.Year;
                    List<BankHoliday> holidays = repo.GetHolidays(year);
                    log.Success = true;
                    log.Output = $"{holidays.Count} festivos encontrados en {year}";
                    break;

                default:
                    log.Success = false;
                    log.Error = $"No hay handler implementado para {task.Code}";
                    break;
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
}
