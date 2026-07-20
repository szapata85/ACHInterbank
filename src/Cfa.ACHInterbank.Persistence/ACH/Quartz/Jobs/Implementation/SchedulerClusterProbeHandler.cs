using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[Scoped]
public sealed class SchedulerClusterProbeHandler : ISchedulerContextAwareTaskHandler
{
    private readonly AchDbContext _db;

    public SchedulerClusterProbeHandler(AchDbContext db)
    {
        _db = db;
    }

    public string Code => "SCHEDULER_CLUSTER_PROBE";

    public Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
        => throw new InvalidOperationException("La sonda de clúster requiere contexto Quartz.");

    public async Task<string> ExecuteAsync(
        TaskDefinition task,
        SchedulerTaskExecutionContext context,
        CancellationToken cancellationToken)
    {
        var probeKey = $"probe:{context.ExecutionId:N}";
        var probe = await _db.SchedulerProbeExecutions
            .SingleOrDefaultAsync(x => x.ProbeKey == probeKey, cancellationToken);

        if (probe is null)
        {
            probe = new SchedulerProbeExecution
            {
                ProbeKey = probeKey,
                ExecutionId = context.ExecutionId,
                SchedulerInstanceId = context.SchedulerInstanceId,
                StartedAtUtc = DateTimeOffset.UtcNow,
                EffectAppliedAtUtc = DateTimeOffset.UtcNow,
                Status = context.IsRecovery ? "RecoveredRunning" : "Running"
            };
            _db.SchedulerProbeExecutions.Add(probe);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _db.Entry(probe).State = EntityState.Detached;
                probe = await _db.SchedulerProbeExecutions
                    .SingleAsync(x => x.ProbeKey == probeKey, cancellationToken);
            }
        }

        probe.SchedulerInstanceId = context.SchedulerInstanceId;
        probe.Status = context.IsRecovery ? "Recovering" : "Running";
        await _db.SaveChangesAsync(cancellationToken);

        var configuredSeconds = GetParameter(task, context.IsRecovery ? "RecoveryDurationSeconds" : "DurationSeconds", context.IsRecovery ? 2 : 15);
        await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(configuredSeconds, 1, 30)), cancellationToken);

        probe.CompletedAtUtc = DateTimeOffset.UtcNow;
        probe.Status = context.IsRecovery ? "Recovered" : "Completed";
        await _db.SaveChangesAsync(cancellationToken);

        return context.IsRecovery
            ? "Sonda recuperada; el efecto idempotente existente fue conservado."
            : "Sonda completada con una única afectación funcional.";
    }

    private static int GetParameter(TaskDefinition task, string key, int fallback)
        => int.TryParse(task.Parameters.FirstOrDefault(x => x.Key == key)?.Value, out var value)
            ? value
            : fallback;
}
