using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ITransactionThirdPartyAccountSelectorService
{
    Task<TransactionThirdPartyAccountSelectorResponse> GetAsync(
        TransactionThirdPartyAccountSelectorRequest request,
        CancellationToken ct = default);
}
