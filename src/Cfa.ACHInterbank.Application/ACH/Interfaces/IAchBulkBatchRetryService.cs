using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchBulkBatchRetryService
{
    Task<RetryBatchResponse> RetryAsync(Guid batchId, RetryBatchRequest request, string triggeredBy, CancellationToken ct = default);
}
