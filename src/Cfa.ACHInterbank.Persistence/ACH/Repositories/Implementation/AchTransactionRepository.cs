using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IReadOnlyList<(TransactionTypeEnum Type, decimal Sum)>> GetTotalsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        var totals = await _context.AchTransactions
            .Where(t => t.AchBatchId == batchId)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Sum = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        return totals.Select(t => (t.Type, t.Sum)).ToList();
    }

    public async Task<IReadOnlyList<TransactionTypeEnum>> GetTypesByBatchAsync(int batchId, CancellationToken ct = default)
    {
        return await _context.AchTransactions
            .Where(t => t.AchBatchId == batchId)
            .Select(t => t.Type)
            .ToListAsync(ct);
    }
}
