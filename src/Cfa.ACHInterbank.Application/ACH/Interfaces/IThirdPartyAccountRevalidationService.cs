using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IThirdPartyAccountRevalidationService
{
    Task<ThirdPartyAccountRevalidationResult?> RequestAsync(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        RequestThirdPartyAccountRevalidationCommand command,
        CancellationToken ct = default);
}
