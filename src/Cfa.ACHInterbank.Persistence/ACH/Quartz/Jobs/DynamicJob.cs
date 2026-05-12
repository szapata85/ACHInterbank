using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
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
    private readonly QuartzTaskCalendarEvaluator _calendarEvaluator;

    public DynamicJob(IServiceProvider sp, ILogger<DynamicJob> logger, IEnumerable<ITaskHandler> handlers, QuartzTaskCalendarEvaluator calendarEvaluator)
    {
        _sp = sp;
        _logger = logger;
        _handlers = handlers;
        _calendarEvaluator = calendarEvaluator;
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
            ScheduledAt = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow,
            StartedAt = DateTimeOffset.UtcNow
        };

        db.TaskExecutionLogs.Add(log);
        await db.SaveChangesAsync(context.CancellationToken);

        try
        {
            var calendarEvaluation = _calendarEvaluator.Evaluate(task, db, DateTimeOffset.UtcNow, _logger);
            if (calendarEvaluation.ShouldSkip)
            {
                log.Success = true;
                log.Output = calendarEvaluation.Reason;
                await db.SaveChangesAsync(context.CancellationToken);

                if (calendarEvaluation.ShouldShift)
                {
                    _logger.LogInformation("Task {Id}/{Code} saltada por ShiftToNextBusinessDay. Próximo día hábil local sugerido: {NextBusinessUtc}.",
                        task.Id, task.Code, calendarEvaluation.NextBusinessDateTime);
                }
                else
                {
                    _logger.LogInformation("Task {Id}/{Code} saltada por CalendarPolicy {Policy}.", task.Id, task.Code, task.CalendarPolicy);
                }

                return;
            }

            var handler = _handlers.FirstOrDefault(h => h.Code == task.Code);
            if (handler is null)
            {
                log.Success = false;
                log.Error = $"No hay handler implementado para {task.Code}";
                return;
            }

            var result = await ExecuteHandlerWithRetryAsync(handler, task, context.CancellationToken);
            log.Success = result.Success;
            log.Output = result.Output;
            log.Error = result.Error;
        }
        catch (OperationCanceledException)
        {
            throw;
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

    private async Task<(bool Success, string? Output, string? Error)> ExecuteHandlerWithRetryAsync(
        ITaskHandler handler,
        TaskDefinition task,
        CancellationToken cancellationToken)
    {
        var maxRetries = task.RetryOnFailure ? Math.Max(task.MaxRetries ?? 0, 0) : 0;
        var maxAttempts = maxRetries + 1;
        var backoff = TimeSpan.FromSeconds(Math.Max(task.RetryBackoffSeconds, 0));
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var output = await handler.ExecuteAsync(task, cancellationToken);
                return (true, $"Ejecutado exitosamente en intento {attempt}/{maxAttempts}. Output: {output}", null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt >= maxAttempts)
                {
                    break;
                }

                if (backoff > TimeSpan.Zero)
                {
                    await Task.Delay(backoff, cancellationToken);
                }
            }
        }

        return (false, null,
            $"Reintentos agotados tras {maxAttempts} intento(s). Última excepción: {lastException}");
    }
}
