using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface ITransactionIntegrationOperationResolver
{
    Task<TransactionIntegrationOperationResult> ResolveAsync(AchTransaction transaction, CancellationToken ct = default);

    TransactionIntegrationOperationResult ResolveDifferentialResponse(string? reference = null, int? transactionId = null);
}
