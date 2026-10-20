using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz;

public sealed class SchedulerMisfireListener : ITriggerListener
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SchedulerMisfireListener> _logger;

    public SchedulerMisfireListener(IServiceProvider services, ILogger<SchedulerMisfireListener> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Name => "achinterbank-functional-misfire-listener";

    public Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<bool> VetoJobExecution(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public async Task TriggerMisfired(ITrigger trigger, CancellationToken cancellationToken)
    {
        try
        {
            var taskId = ParseTaskId(trigger.JobKey.Name);
            if (!taskId.HasValue)
            {
                return;
            }

            await using var scope = _services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            var task = await db.TaskDefinitions.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == taskId.Value, cancellationToken);
            if (task is null)
            {
                return;
            }

            var occurredAt = DateTimeOffset.UtcNow;
            var executionId = CreateDeterministicId($"misfire:{trigger.Key}:{trigger.GetNextFireTimeUtc():O}");
            if (await db.TaskExecutionLogs.AnyAsync(x => x.ExecutionId == executionId, cancellationToken))
            {
                return;
            }

            var taskCode = SchedulerTaskCatalog.ByHandlerCode(task.Code)?.TaskCode ?? task.Code;
            var policyDescription = task.MisfirePolicy == SchedulerMisfirePolicy.FireAndProceed
                ? "FireAndProceed: ejecutar una vez después de la recuperación y continuar normalmente."
                : "DoNothing: omitir la ejecución perdida y continuar con la siguiente programación.";

            db.TaskExecutionLogs.Add(new TaskExecutionLog
            {
                TaskDefinitionId = task.Id,
                ExecutionId = executionId,
                ExecutionKey = executionId.ToString("N"),
                TaskCode = taskCode,
                JobName = trigger.JobKey.Name,
                JobGroup = trigger.JobKey.Group,
                TriggerName = trigger.Key.Name,
                TriggerType = "Programada",
                FireInstanceId = string.Empty,
                SchedulerInstanceId = string.Empty,
                SchedulerInstanceName = string.Empty,
                IdempotencyKey = executionId.ToString("N"),
                CorrelationId = $"misfire:{executionId:N}",
                ScheduledAt = trigger.GetPreviousFireTimeUtc() ?? occurredAt,
                StartedAt = occurredAt,
                FinishedAt = occurredAt,
                DurationMilliseconds = 0,
                Status = SchedulerExecutionStatus.Misfired,
                MisfireDetected = true,
                Success = false,
                Output = policyDescription,
                CreatedAtUtc = occurredAt
            });
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Misfire registrado para {TaskCode}; Trigger={Trigger}; Policy={Policy}; ExecutionId={ExecutionId}",
                taskCode,
                trigger.Key,
                task.MisfirePolicy,
                executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible registrar el misfire funcional del trigger {TriggerKey}.", trigger.Key);
        }
    }

    public Task TriggerComplete(
        ITrigger trigger,
        IJobExecutionContext context,
        SchedulerInstruction triggerInstructionCode,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    private static int? ParseTaskId(string name)
        => name.StartsWith("job:", StringComparison.OrdinalIgnoreCase)
           && int.TryParse(name[4..], out var value)
            ? value
            : null;

    private static Guid CreateDeterministicId(string material)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return new Guid(hash.AsSpan(0, 16));
    }
}
