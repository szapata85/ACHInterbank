using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchBatchService : IAchBatchService
{
    private readonly AchDbContext _context;

    public AchBatchService(AchDbContext context) => _context = context;

    public async Task<AchBatch> CreateBatchAsync(
        int clearingHouseId,
        string companyName,
        string companyId,
        DateTime effectiveEntryDate,
        IEnumerable<int> transactionIds,
        CancellationToken ct = default)
    {
        var transactions = await _context.AchTransactions
            .Where(t => transactionIds.Contains(t.Id))
            .ToListAsync(ct);

        var batch = new AchBatch
        {
            ClearingHouseId = clearingHouseId,
            CompanyName = companyName,
            CompanyIdentification = companyId,
            EffectiveEntryDate = effectiveEntryDate,
            Transactions = transactions
        };

        _context.AchBatches.Add(batch);
        await _context.SaveChangesAsync(ct);
        return batch;
    }
}

