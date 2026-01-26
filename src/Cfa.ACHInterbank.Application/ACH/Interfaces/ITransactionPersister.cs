using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ITransactionPersister
{
    Task<TransactionPersistResult> PersistAsync(AchTransactionRequestData request, TransactionBatchContext context, CancellationToken ct = default);
    Task UpdateBatchTotalsAsync(AchBatch batch, CancellationToken ct = default);
    Task UpdateBatchServiceClassCodeAsync(AchBatch batch, CancellationToken ct = default);
}
