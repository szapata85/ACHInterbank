using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;

public class DynamicJobExecutor
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DynamicJobExecutor> _logger;
    private readonly IEnumerable<ITaskHandler> _handlers;
    private readonly QuartzTaskCalendarEvaluator _calendarEvaluator;
    private readonly QuartzJobStoreOptions _options;

    public DynamicJobExecutor(
        IServiceProvider sp,
        ILogger<DynamicJobExecutor> logger,
        IEnumerable<ITaskHandler> handlers,
        QuartzTaskCalendarEvaluator calendarEvaluator)
        : this(sp, logger, handlers, calendarEvaluator, new ConfigurationBuilder().Build())
    {
    }

    public DynamicJobExecutor(
        IServiceProvider sp,
        ILogger<DynamicJobExecutor> logger,
        IEnumerable<ITaskHandler> handlers,
        QuartzTaskCalendarEvaluator calendarEvaluator,
        IConfiguration configuration)
    {
        _sp = sp;
        _logger = logger;
        _handlers = handlers;
        _calendarEvaluator = calendarEvaluator;
        _options = QuartzJobStoreOptionsFactory.Create(configuration);
    }

    public async Task ExecuteAsync(IJobExecutionContext context)
    {
        var taskId = Convert.ToInt32(context.MergedJobDataMap["TaskId"], CultureInfo.InvariantCulture);
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

        var executionId = ResolveExecutionId(context);
        var fireInstanceId = ResolveFireInstanceId(context, executionId);
        var now = DateTimeOffset.UtcNow;
        var functionalCode = SchedulerTaskCatalog.ByHandlerCode(task.Code)?.TaskCode ?? task.Code;
        var jobKey = ResolveJobKey(context);
        var schedulerInstanceId = ResolveSchedulerInstanceId(context);
        var existingLog = context.Recovering
            ? await db.TaskExecutionLogs
                .Where(x => x.TaskDefinitionId == task.Id
                    && x.JobName == jobKey.Name
                    && x.FinishedAt == null
                    && (x.Status == SchedulerExecutionStatus.Pending || x.Status == SchedulerExecutionStatus.Running))
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync(context.CancellationToken)
            : await db.TaskExecutionLogs
                .SingleOrDefaultAsync(x => x.ExecutionId == executionId, context.CancellationToken);

        if (context.Recovering && existingLog?.ExecutionId is Guid recoveredExecutionId)
        {
            executionId = recoveredExecutionId;
        }
        var log = existingLog ?? CreateExecutionLog(context, task, executionId, functionalCode, now);

        if (existingLog is null)
        {
            db.TaskExecutionLogs.Add(log);
        }
        else if (context.Recovering)
        {
            log.OriginalFireInstanceId = string.IsNullOrWhiteSpace(log.FireInstanceId)
                ? null
                : log.FireInstanceId;
        }

        log.FireInstanceId = fireInstanceId;
        log.SchedulerInstanceId = schedulerInstanceId;
        log.SchedulerInstanceName = _options.InstanceName;
        log.ActualFireTimeUtc = context.FireTimeUtc;
        log.StartedAt = now;
        log.Status = SchedulerExecutionStatus.Running;
        log.RefireCount = context.RefireCount;
        log.IsRecovery = context.Recovering;
        log.TriggerType = context.Recovering ? "Recuperación" : log.TriggerType;
        log.RecoveredByInstanceId = context.Recovering ? schedulerInstanceId : null;
        log.RecoveryStartedAtUtc = context.Recovering ? now : null;
        log.MisfireDetected = log.MisfireDetected || IsMisfire(context, now);

        await db.SaveChangesAsync(context.CancellationToken);

        Exception? unhandled = null;
        try
        {
            var calendarEvaluation = _calendarEvaluator.Evaluate(task, db, now, _logger);
            if (calendarEvaluation.ShouldSkip)
            {
                log.Success = true;
                log.Status = SchedulerExecutionStatus.Skipped;
                log.Output = Truncate(calendarEvaluation.Reason, 2000);
                return;
            }

            var handler = _handlers.FirstOrDefault(h => h.Code == task.Code);
            if (handler is null)
            {
                log.Success = false;
                log.Status = SchedulerExecutionStatus.Failed;
                log.ErrorCode = "HANDLER_NOT_FOUND";
                log.Error = $"No hay handler implementado para {task.Code}";
                return;
            }

            var executionContext = new SchedulerTaskExecutionContext(
                executionId,
                log.CorrelationId,
                fireInstanceId,
                schedulerInstanceId,
                context.Recovering,
                context.RefireCount);

            var result = handler is ISchedulerContextAwareTaskHandler contextAware
                ? await ExecuteContextAwareHandlerWithRetryAsync(contextAware, task, executionContext, context.CancellationToken)
                : await ExecuteHandlerWithRetryAsync(handler, task, context.CancellationToken);

            log.Success = result.Success;
            log.Output = Truncate(result.Output, 2000);
            log.Error = Truncate(result.Error, 2000);
            log.ErrorCode = result.Success ? null : "RETRIES_EXHAUSTED";
            log.Status = result.Success
                ? context.Recovering ? SchedulerExecutionStatus.Recovered : SchedulerExecutionStatus.Succeeded
                : SchedulerExecutionStatus.Failed;
            log.RecoveryResult = context.Recovering
                ? result.Success ? "Recuperación completada" : "Recuperación fallida"
                : null;
        }
        catch (OperationCanceledException ex)
        {
            log.Success = false;
            log.Status = SchedulerExecutionStatus.Failed;
            log.ErrorCode = "CANCELLED";
            log.Error = "La ejecución fue cancelada durante el apagado de la instancia.";
            unhandled = ex;
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.Status = SchedulerExecutionStatus.Failed;
            log.ErrorCode = ex.GetType().Name;
            log.Error = Truncate(ex.Message, 2000);
            unhandled = ex;
        }
        finally
        {
            log.FinishedAt = DateTimeOffset.UtcNow;
            log.DurationMilliseconds = Math.Max(0, (long)(log.FinishedAt.Value - log.StartedAt).TotalMilliseconds);
            log.ManualConcurrencyKey = null;
            await db.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation(
                "Finalizó tarea {TaskCode}; ExecutionId={ExecutionId}; CorrelationId={CorrelationId}; FireInstanceId={FireInstanceId}; SchedulerInstanceId={SchedulerInstanceId}; TriggerType={TriggerType}; Status={Status}",
                functionalCode,
                executionId,
                log.CorrelationId,
                fireInstanceId,
                schedulerInstanceId,
                log.TriggerType,
                log.Status);
        }

        if (unhandled is OperationCanceledException cancellation)
        {
            throw cancellation;
        }

        if (unhandled is not null)
        {
            throw new JobExecutionException(unhandled, refireImmediately: false);
        }
    }

    private TaskExecutionLog CreateExecutionLog(
        IJobExecutionContext context,
        TaskDefinition task,
        Guid executionId,
        string functionalCode,
        DateTimeOffset now)
    {
        var triggerType = GetStringOrNull(context.MergedJobDataMap, "TriggerType") ?? "Programada";
        var jobKey = ResolveJobKey(context);
        return new TaskExecutionLog
        {
            TaskDefinitionId = task.Id,
            ScheduledAt = context.ScheduledFireTimeUtc ?? now,
            StartedAt = now,
            CreatedAtUtc = now,
            ExecutionId = executionId,
            ExecutionKey = executionId.ToString("N"),
            TaskCode = functionalCode,
            JobName = jobKey.Name,
            JobGroup = jobKey.Group,
            TriggerName = context.Trigger?.Key.Name ?? "ContextTrigger",
            TriggerType = triggerType,
            FireInstanceId = ResolveFireInstanceId(context, executionId),
            SchedulerInstanceId = ResolveSchedulerInstanceId(context),
            SchedulerInstanceName = _options.InstanceName,
            RequestedByUserId = GetStringOrNull(context.MergedJobDataMap, "RequestedByUserId"),
            RequestedByUserName = GetStringOrNull(context.MergedJobDataMap, "RequestedByUserName"),
            RequestReason = GetStringOrNull(context.MergedJobDataMap, "RequestReason"),
            RequestId = GetStringOrNull(context.MergedJobDataMap, "RequestId"),
            IdempotencyKey = GetStringOrNull(context.MergedJobDataMap, "IdempotencyKey") ?? executionId.ToString("N"),
            CorrelationId = GetStringOrNull(context.MergedJobDataMap, "CorrelationId") ?? $"scheduler:{executionId:N}",
            Status = SchedulerExecutionStatus.Running,
            IsRecovery = context.Recovering,
            RefireCount = context.RefireCount
        };
    }

    private Guid ResolveExecutionId(IJobExecutionContext context)
    {
        var configured = GetStringOrNull(context.MergedJobDataMap, "ExecutionId");
        if (Guid.TryParse(configured, out var executionId))
        {
            return executionId;
        }

        var scheduled = context.ScheduledFireTimeUtc ?? context.FireTimeUtc;
        var jobKey = ResolveJobKey(context);
        var material = $"{jobKey.Group}/{jobKey.Name}:{scheduled.UtcTicks}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static JobKey ResolveJobKey(IJobExecutionContext context)
        => context.JobDetail?.Key ?? new JobKey(nameof(DynamicJob), "DEFAULT");

    private string ResolveSchedulerInstanceId(IJobExecutionContext context)
        => context.Scheduler?.SchedulerInstanceId ?? _options.InstanceId;

    private static string ResolveFireInstanceId(IJobExecutionContext context, Guid executionId)
        => string.IsNullOrWhiteSpace(context.FireInstanceId)
            ? $"context:{executionId:N}"
            : context.FireInstanceId;

    private bool IsMisfire(IJobExecutionContext context, DateTimeOffset actual)
        => context.ScheduledFireTimeUtc.HasValue
           && actual - context.ScheduledFireTimeUtc.Value > TimeSpan.FromMilliseconds(_options.MisfireThresholdMilliseconds);

    private static Task<(bool Success, string? Output, string? Error)> ExecuteHandlerWithRetryAsync(
        ITaskHandler handler,
        TaskDefinition task,
        CancellationToken cancellationToken)
        => ExecuteWithRetryAsync(ct => handler.ExecuteAsync(task, ct), task, cancellationToken);

    private static Task<(bool Success, string? Output, string? Error)> ExecuteContextAwareHandlerWithRetryAsync(
        ISchedulerContextAwareTaskHandler handler,
        TaskDefinition task,
        SchedulerTaskExecutionContext context,
        CancellationToken cancellationToken)
        => ExecuteWithRetryAsync(ct => handler.ExecuteAsync(task, context, ct), task, cancellationToken);

    private static async Task<(bool Success, string? Output, string? Error)> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<string>> execute,
        TaskDefinition task,
        CancellationToken cancellationToken)
    {
        var isSoapTask = SchedulerTaskCatalog.ByHandlerCode(task.Code)?.SoapService is not null;
        var maxRetries = task.RetryOnFailure && !isSoapTask ? Math.Max(task.MaxRetries ?? 0, 0) : 0;
        var maxAttempts = maxRetries + 1;
        var backoff = TimeSpan.FromSeconds(Math.Max(task.RetryBackoffSeconds, 0));
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var output = await execute(cancellationToken);
                return (true, $"Ejecutado exitosamente en intento {attempt}/{maxAttempts}. Resultado: {output}", null);
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

        return (false, null, $"Reintentos agotados tras {maxAttempts} intento(s). Último error: {lastException?.Message}");
    }

    private static string? Truncate(string? value, int maxLength)
        => string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];

    private static string? GetStringOrNull(JobDataMap map, string key)
        => map.ContainsKey(key) ? map.GetString(key) : null;
}
