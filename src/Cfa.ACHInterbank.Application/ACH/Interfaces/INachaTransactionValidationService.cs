using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaTransactionValidationService
{
    Task ValidateTransactionsForSendAsync(IReadOnlyList<AchTransaction> transactions, CancellationToken ct = default);
}
