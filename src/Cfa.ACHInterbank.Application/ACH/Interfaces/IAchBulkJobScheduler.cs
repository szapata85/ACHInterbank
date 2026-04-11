namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchBulkJobScheduler
{
    Task<string> EnqueueBatchAsync(Guid batchId, long? attemptId = null, CancellationToken ct = default);
}
