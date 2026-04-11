using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchBulkJobScheduler : IAchBulkJobScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;

    public AchBulkJobScheduler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task<string> EnqueueBatchAsync(Guid batchId, long? attemptId = null, CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var correlationId = $"bulk-batch:{batchId}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var job = JobBuilder.Create<ProcessBulkIngestionBatchJob>()
            .WithIdentity(correlationId, "bulk-ingestion")
            .UsingJobData("BatchId", batchId.ToString())
            .UsingJobData("AttemptId", attemptId?.ToString() ?? string.Empty)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trg:{correlationId}", "bulk-ingestion")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger, ct);
        return correlationId;
    }
}
