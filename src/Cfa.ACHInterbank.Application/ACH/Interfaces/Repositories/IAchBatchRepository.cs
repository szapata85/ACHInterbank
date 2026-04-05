using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;

public interface IAchBatchRepository
{
    Task<AchBatch?> FindForTransactionAsync(string achCycleId, string companyName, string companyIdentification, string companyEntryDescription, DateTime effectiveEntryDate, CancellationToken ct = default);
    Task AddAsync(AchBatch batch, CancellationToken ct = default);
    Task<IReadOnlyList<AchCycle>> GetUpcomingCyclesAsync(string clearingHouseId, DateTime processingDate, TimeSpan cutoffTime, int take, CancellationToken ct = default);
    Task UpdateAsync(AchBatch batch, CancellationToken ct = default);
}
