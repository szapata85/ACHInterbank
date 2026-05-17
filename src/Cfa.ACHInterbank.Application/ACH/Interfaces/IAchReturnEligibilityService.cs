using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchReturnEligibilityService
{
    Task<AchReturnEligibilityResult> EvaluateOutgoingReturnAsync(
        AchReturnEligibilityRequest request,
        CancellationToken cancellationToken);
}
