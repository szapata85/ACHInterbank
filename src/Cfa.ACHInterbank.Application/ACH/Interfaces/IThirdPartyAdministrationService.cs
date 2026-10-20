using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Common;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IThirdPartyAdministrationService
{
    Task<PagedResponse<ThirdPartyAdministrationDto>> GetThirdPartiesAsync(
        int customerId,
        ThirdPartyAdministrationQuery query,
        CancellationToken ct = default);

    Task<ThirdPartyAdministrationDto?> GetThirdPartyAsync(
        int customerId,
        long thirdPartyId,
        CancellationToken ct = default);

    Task<ThirdPartyCommandResult<ThirdPartyAdministrationDto>> CreateThirdPartyAsync(
        int customerId,
        CreateThirdPartyCommand command,
        CancellationToken ct = default);

    Task<ThirdPartyAdministrationDto?> UpdateThirdPartyAsync(
        int customerId,
        long thirdPartyId,
        UpdateThirdPartyCommand command,
        CancellationToken ct = default);

    Task<ThirdPartyAdministrationDto?> SetThirdPartyStatusAsync(
        int customerId,
        long thirdPartyId,
        SetThirdPartyAdministrativeStatusCommand command,
        CancellationToken ct = default);

    Task<ThirdPartyCommandResult<ThirdPartyAccountReadDto>?> CreateAccountAsync(
        int customerId,
        long thirdPartyId,
        CreateThirdPartyAccountCommand command,
        CancellationToken ct = default);

    Task<ThirdPartyAccountReadDto?> UpdateAccountAsync(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        UpdateThirdPartyAccountCommand command,
        CancellationToken ct = default);

    Task<ThirdPartyAccountReadDto?> SetAccountStatusAsync(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        SetThirdPartyAdministrativeStatusCommand command,
        CancellationToken ct = default);
}
