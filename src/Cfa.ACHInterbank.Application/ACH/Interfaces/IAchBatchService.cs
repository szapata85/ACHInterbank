using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchBatchService
{
    Task<AchBatch> CreateBatchAsync(
        int clearingHouseId,
        string companyName,
        string companyId,
        DateTime effectiveEntryDate,
        IEnumerable<int> transactionIds,
        CancellationToken ct = default);
}

