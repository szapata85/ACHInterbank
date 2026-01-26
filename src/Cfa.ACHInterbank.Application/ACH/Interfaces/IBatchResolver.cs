using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBatchResolver
{
    Task<TransactionBatchContext> ResolveAsync(AchTransactionRequestData request, CancellationToken ct = default);
}
