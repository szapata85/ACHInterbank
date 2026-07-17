using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface ITransactionIntegrationResultService
{
    Task<TransactionIntegrationResultDto?> GetAsync(int transactionId, CancellationToken ct = default);
}
