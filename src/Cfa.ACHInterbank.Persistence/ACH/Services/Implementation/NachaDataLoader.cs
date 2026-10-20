using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaDataLoader : INachaDataLoader
{
    private readonly AchDbContext _context;

    public NachaDataLoader(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AchBatch>> LoadBatchesByIdsAsync(IEnumerable<int> batchIds, CancellationToken ct = default)
    {
        var requestedIds = batchIds.Distinct().ToList();
        var requestedOrder = requestedIds
            .Select((id, index) => new { id, index })
            .ToDictionary(item => item.id, item => item.index);
        var batches = await _context.AchBatches
            .AsNoTracking()
            .Include(b => b.AchCycle)
                .ThenInclude(c => c!.ClearingHouse)
            .Include(b => b.Transactions)
                .ThenInclude(t => t.Addendas)
            .Include(b => b.Transactions)
                .ThenInclude(t => t.SourceInstitution)
            .Include(b => b.Transactions)
                .ThenInclude(t => t.DestinationInstitution)
            .Where(b => requestedIds.Contains(b.Id))
            .ToListAsync(ct);

        return batches.OrderBy(batch => requestedOrder[batch.Id]).ToList();
    }

    public async Task<NachaBuildContext> LoadByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException($"No existe el ciclo {cycleId}.");

        var cycleBatches = await _context.AchBatches
            .AsNoTracking()
            .Where(b => b.AchCycleId == cycleId)
            .ToListAsync(ct);

        var transactions = await _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.Addendas)
            .Include(t => t.AchBatch)
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Where(t => t.AchCycleId == cycleId
                        && NachaExportEligibility.ExportableStates.Contains(t.State))
            .ToListAsync(ct);

        var transactionBatches = transactions
            .Where(t => t.AchBatch is not null)
            .Select(t => t.AchBatch!)
            .GroupBy(b => b.Id)
            .Select(g => g.First())
            .ToList();

        var batches = cycleBatches
            .Concat(transactionBatches)
            .GroupBy(b => b.Id)
            .Select(g => g.First())
            .OrderBy(batch => batch.EffectiveEntryDate)
            .ThenBy(batch => batch.CompanyIdentification)
            .ThenBy(batch => batch.OriginOrOdfi)
            .ThenBy(batch => batch.CompanyEntryDescription)
            .ThenBy(batch => batch.Id)
            .ToList();

        var transactionsByBatchId = transactions
            .Where(t => t.AchBatchId > 0)
            .GroupBy(t => t.AchBatchId)
            .ToDictionary(g => g.Key, g => (ICollection<AchTransaction>)g.OrderBy(t => t.Id).ToList());

        foreach (var batch in batches)
        {
            batch.Transactions = transactionsByBatchId.TryGetValue(batch.Id, out var batchTransactions)
                ? batchTransactions
                : [];
        }

        return new NachaBuildContext
        {
            Cycle = cycle,
            Batches = batches,
            Transactions = transactions
        };
    }

    public async Task<NachaHeader?> LoadHeaderAsync(string cycleId, CancellationToken ct = default)
    {
        return await _context.NachaHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.AchCycleId == cycleId, ct);
    }

    public async Task<IReadOnlyDictionary<string, NachaRecordLayout>> LoadLayoutsAsync(CancellationToken ct = default)
    {
        return await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .ToDictionaryAsync(l => l.RecordCode!, ct);
    }

    public async Task<IReadOnlyList<NachaRecordDefinition>> LoadDefinitionsAsync(CancellationToken ct = default)
    {
        var definitions = await _context.NachaRecordDefinitions
            .AsNoTracking()
            .Where(d => d.IsEnabled)
            .OrderBy(d => d.Sequence)
            .ToListAsync(ct);

        if (definitions.Count > 0)
        {
            return definitions;
        }

        return BuildDefaultDefinitions();
    }

    public async Task<IReadOnlyList<(string Term, string StandardEntryClassCode)>> LoadCompanyEntryDescriptionCatalogAsync(CancellationToken ct = default)
    {
        return await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new ValueTuple<string, string>(item.Term, item.StandardEntryClassCode))
            .ToListAsync(ct);
    }

    private static IReadOnlyList<NachaRecordDefinition> BuildDefaultDefinitions()
    {
        return new List<NachaRecordDefinition>
        {
            new() { RecordCode = "1", Sequence = 10, SourceType = NachaRecordSourceType.Custom, IsEnabled = true },
            new() { RecordCode = "5", Sequence = 20, SourceType = NachaRecordSourceType.Custom, IsEnabled = true },
            new() { RecordCode = "6", Sequence = 30, SourceType = NachaRecordSourceType.Custom, SourceName = nameof(AchTransaction), FilterKey = "BatchId", IsEnabled = true },
            new() { RecordCode = "7", Sequence = 40, SourceType = NachaRecordSourceType.Custom, SourceName = nameof(AchTransactionAddenda), FilterKey = "BatchId", IsEnabled = true },
            new() { RecordCode = "8", Sequence = 50, SourceType = NachaRecordSourceType.Custom, IsEnabled = true },
            new() { RecordCode = "9", Sequence = 60, SourceType = NachaRecordSourceType.Custom, IsEnabled = true }
        };
    }
}
