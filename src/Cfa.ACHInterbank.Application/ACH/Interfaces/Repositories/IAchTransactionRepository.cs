using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;

public interface IAchTransactionRepository
{
    Task<int?> GetMaxTraceSequenceAsync(DateTime processingDate, string traceOriginatingDfi, CancellationToken ct = default);
    Task<bool> ExistsTraceSequenceAsync(DateTime processingDate, string traceOriginatingDfi, int sequence, CancellationToken ct = default);
    Task AddAsync(AchTransaction transaction, CancellationToken ct = default);
    Task<IReadOnlyList<(TransactionTypeEnum Type, decimal Sum)>> GetTotalsByBatchAsync(AchBatch batch, CancellationToken ct = default);
    Task<IReadOnlyList<TransactionTypeEnum>> GetTypesByBatchAsync(AchBatch batch, CancellationToken ct = default);
}
