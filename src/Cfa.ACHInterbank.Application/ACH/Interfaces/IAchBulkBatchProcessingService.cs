namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchBulkBatchProcessingService
{
    Task ProcessBatchAsync(Guid batchId, long? attemptId = null, string? jobId = null, CancellationToken ct = default);
}
