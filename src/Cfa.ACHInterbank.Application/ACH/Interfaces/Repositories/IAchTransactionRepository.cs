using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;

public interface IAchTransactionRepository
{
    Task<int> AllocateNextTraceSequenceAsync(DateOnly processingDate, string traceOriginatingDfi, DateTime allocatedAtUtc, CancellationToken ct = default);
    Task AddAsync(AchTransaction transaction, CancellationToken ct = default);
    Task<IReadOnlyList<(TransactionTypeEnum Type, decimal Sum)>> GetTotalsByBatchAsync(AchBatch batch, CancellationToken ct = default);
    Task<IReadOnlyList<TransactionTypeEnum>> GetTypesByBatchAsync(AchBatch batch, CancellationToken ct = default);
}
