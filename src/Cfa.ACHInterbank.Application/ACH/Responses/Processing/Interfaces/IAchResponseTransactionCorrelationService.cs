using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;

public interface IAchResponseTransactionCorrelationService
{
    Task<AchResponseCorrelationResult> CorrelateAsync(
        string transactionIdentifier,
        CancellationToken cancellationToken = default);
}
