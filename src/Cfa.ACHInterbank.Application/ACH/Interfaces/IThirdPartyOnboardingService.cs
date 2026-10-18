using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IThirdPartyOnboardingService
{
    Task<ThirdPartyRef> EnsureThirdPartyAsync(
        ThirdPartyMasterRequest request,
        CancellationToken cancellationToken = default);

    Task<ThirdPartyAccountRef> EnsureAccountAsync(
        ThirdPartyOnboardingRequest request,
        CancellationToken cancellationToken = default);
}
