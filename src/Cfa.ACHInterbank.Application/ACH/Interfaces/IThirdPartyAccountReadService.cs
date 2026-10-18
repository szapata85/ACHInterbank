using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Common;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IThirdPartyAccountReadService
{
    Task<PagedResponse<ThirdPartyAccountReadDto>> GetAsync(
        ThirdPartyAccountReadQuery query,
        CancellationToken ct = default);

    Task<ThirdPartyAccountReadDto?> GetByAccountIdAsync(
        long thirdPartyAccountId,
        ThirdPartyAccountEligibilityReadContext? eligibilityContext = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<ThirdPartyAccountValidationHistoryDto>?> GetValidationHistoryAsync(
        long thirdPartyAccountId,
        int? customerId = null,
        long? thirdPartyId = null,
        CancellationToken ct = default);
}
