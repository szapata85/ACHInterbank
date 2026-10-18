using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IThirdPartyAccountValidationService
{
    Task<PrenotificationValidationStartResult> RegisterPrenotificationAttemptAsync(
        PrenotificationValidationStartRequest request,
        CancellationToken ct = default);

    Task<bool> ApplyTransactionResolutionAsync(
        PrenotificationValidationResolutionRequest request,
        CancellationToken ct = default);

    Task<bool> RecordMonetaryUseAsync(
        ThirdPartyAccountMonetaryUseRequest request,
        CancellationToken ct = default);
}
