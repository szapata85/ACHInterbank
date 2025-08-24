using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
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
        AchDbContext db = scope.ServiceProvider.GetRequiredService<AchDbContext>();

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
}
