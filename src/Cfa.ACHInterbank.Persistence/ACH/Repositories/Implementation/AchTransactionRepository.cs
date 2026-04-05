using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class AchTransactionRepository : IAchTransactionRepository
{
    private readonly AchDbContext _context;

    public AchTransactionRepository(AchDbContext context)
    {
        _context = context;
    }

    public async Task<int?> GetMaxTraceSequenceAsync(DateTime processingDate, string traceOriginatingDfi, CancellationToken ct = default)
    {
        return await _context.AchTransactions
            .Where(t => t.EffectiveEntryDate.Date == processingDate)
            .Where(t => t.TraceNumber.StartsWith(traceOriginatingDfi))
            .Select(t => (int?)t.TraceSequenceNumber)
            .MaxAsync(ct);
    }

    public async Task<bool> ExistsTraceSequenceAsync(DateTime processingDate, string traceOriginatingDfi, int sequence, CancellationToken ct = default)
    {
        return await _context.AchTransactions
            .AnyAsync(t => t.EffectiveEntryDate.Date == processingDate
                           && t.TraceSequenceNumber == sequence
                           && t.TraceNumber.StartsWith(traceOriginatingDfi), ct);
    }

    public Task AddAsync(AchTransaction transaction, CancellationToken ct = default)
    {
        _context.AchTransactions.Add(transaction);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<(TransactionTypeEnum Type, decimal Sum)>> GetTotalsByBatchAsync(AchBatch batch, CancellationToken ct = default)
    {
        var persisted = batch.Id > 0
            ? await _context.AchTransactions
                .AsNoTracking()
                .Where(t => t.AchBatchId == batch.Id)
                .Select(t => new { t.Id, t.Type, t.Amount })
                .ToListAsync(ct)
            : [];

        var tracked = _context.AchTransactions.Local
            .Where(t => ReferenceEquals(t.AchBatch, batch) || (batch.Id > 0 && t.AchBatchId == batch.Id))
            .Select(t => new { t.Id, t.Type, t.Amount, InstanceId = RuntimeHelpers.GetHashCode(t) })
            .ToList();

        return persisted
            .Select(t => new { Key = $"db:{t.Id}", t.Type, t.Amount })
            .Concat(tracked.Select(t => new
            {
                Key = t.Id > 0 ? $"db:{t.Id}" : $"mem:{t.InstanceId}",
                t.Type,
                t.Amount
            }))
            .GroupBy(t => t.Key)
            .Select(g => g.First())
            .GroupBy(t => t.Type)
            .Select(g => (Type: g.Key, Sum: g.Sum(x => x.Amount)))
            .ToList();
    }

    public async Task<IReadOnlyList<TransactionTypeEnum>> GetTypesByBatchAsync(AchBatch batch, CancellationToken ct = default)
    {
        var persistedTypes = batch.Id > 0
            ? await _context.AchTransactions
                .AsNoTracking()
                .Where(t => t.AchBatchId == batch.Id)
                .Select(t => new { t.Id, t.Type })
                .ToListAsync(ct)
            : [];

        var trackedTypes = _context.AchTransactions.Local
            .Where(t => ReferenceEquals(t.AchBatch, batch) || (batch.Id > 0 && t.AchBatchId == batch.Id))
            .Select(t => new { t.Id, t.Type, InstanceId = RuntimeHelpers.GetHashCode(t) })
            .ToList();

        return persistedTypes
            .Select(t => new { Key = $"db:{t.Id}", t.Type })
            .Concat(trackedTypes.Select(t => new
            {
                Key = t.Id > 0 ? $"db:{t.Id}" : $"mem:{t.InstanceId}",
                t.Type
            }))
            .GroupBy(t => t.Key)
            .Select(g => g.First().Type)
            .ToList();
    }
}
