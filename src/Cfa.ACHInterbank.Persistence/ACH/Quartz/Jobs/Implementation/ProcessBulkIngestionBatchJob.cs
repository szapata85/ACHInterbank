using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[DisallowConcurrentExecution]
public class ProcessBulkIngestionBatchJob : IJob
{
    private readonly IAchBulkBatchProcessingService _processingService;
    private readonly ILogger<ProcessBulkIngestionBatchJob> _logger;

    public ProcessBulkIngestionBatchJob(
        IAchBulkBatchProcessingService processingService,
        ILogger<ProcessBulkIngestionBatchJob> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var batchIdRaw = context.MergedJobDataMap.GetString("BatchId");
        var attemptIdRaw = context.MergedJobDataMap.GetString("AttemptId");
        if (!Guid.TryParse(batchIdRaw, out var batchId))
        {
            _logger.LogWarning("Job de lote ejecutado sin BatchId válido. BatchId={BatchId}", batchIdRaw);
            return;
        }

        long? attemptId = long.TryParse(attemptIdRaw, out var parsedAttemptId) ? parsedAttemptId : null;
        await _processingService.ProcessBatchAsync(batchId, attemptId, context.FireInstanceId, context.CancellationToken);
    }
}
