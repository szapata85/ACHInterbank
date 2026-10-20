using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IThirdPartyAccountEligibilityService
{
    Task<ThirdPartyAccountEligibilityResult> EvaluateAsync(
        ThirdPartyAccountEligibilityRequest request,
        CancellationToken ct = default);
}
