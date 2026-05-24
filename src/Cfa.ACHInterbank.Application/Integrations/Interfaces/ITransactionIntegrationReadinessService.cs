using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface ITransactionIntegrationReadinessService
{
    Task<TransactionIntegrationReadinessResult?> GetTransactionReadinessAsync(int transactionId, CancellationToken ct = default);
}
