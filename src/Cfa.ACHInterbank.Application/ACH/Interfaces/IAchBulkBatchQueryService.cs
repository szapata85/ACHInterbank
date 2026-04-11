using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchBulkBatchQueryService
{
    Task<BulkBatchStatusDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<BulkBatchItemsPageDto> GetBatchItemsAsync(Guid batchId, int page, int pageSize, BulkIngestionItemStatusEnum? status, CancellationToken ct = default);
    Task<BulkBatchProcessingSummaryDto?> GetBatchSummaryAsync(Guid batchId, CancellationToken ct = default);
}
