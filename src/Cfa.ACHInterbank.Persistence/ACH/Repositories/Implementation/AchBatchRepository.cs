using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class AchBatchRepository : IAchBatchRepository
{
    private readonly AchDbContext _context;

    public AchBatchRepository(AchDbContext context)
    {
        _context = context;
    }

    public Task<AchBatch?> FindForTransactionAsync(string achCycleId, string companyName, string companyIdentification, string companyEntryDescription, DateTime effectiveEntryDate, CancellationToken ct = default)
    {
        return _context.AchBatches
            .FirstOrDefaultAsync(b =>
                b.AchCycleId == achCycleId &&
                b.CompanyName == companyName &&
                b.CompanyIdentification == companyIdentification &&
                b.CompanyEntryDescription == companyEntryDescription &&
                b.EffectiveEntryDate == effectiveEntryDate, ct);
    }

    public Task AddAsync(AchBatch batch, CancellationToken ct = default)
    {
        _context.AchBatches.Add(batch);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<AchCycle>> GetUpcomingCyclesAsync(int clearingHouseId, DateTime processingDate, TimeSpan cutoffTime, int take, CancellationToken ct = default)
    {
        var buffered = await _context.AchCycles
            .AsNoTracking()
            .Where(c => c.ClearingHouseId == clearingHouseId)
            .OrderBy(c => c.ProcessingDate)
            .ToListAsync(ct);

        return buffered
            .Where(c => c.ProcessingDate > processingDate
                        || (c.ProcessingDate == processingDate && c.CutoffTime >= cutoffTime))
            .OrderBy(c => c.ProcessingDate)
            .ThenBy(c => c.CutoffTime)
            .Take(take)
            .ToList();
    }

    public Task UpdateAsync(AchBatch batch, CancellationToken ct = default)
    {
        var entry = _context.Entry(batch);

        if (batch.Id <= 0)
        {
            if (entry.State == EntityState.Detached)
            {
                _context.AchBatches.Add(batch);
            }

            return Task.CompletedTask;
        }

        if (entry.State == EntityState.Detached)
        {
            _context.AchBatches.Attach(batch);
        }

        entry.State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
