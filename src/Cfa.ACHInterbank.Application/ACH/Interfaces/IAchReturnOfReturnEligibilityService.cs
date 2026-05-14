using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchReturnOfReturnEligibilityService
{
    Task<AchReturnOfReturnEligibilityResult> EvaluateAsync(
        AchReturnOfReturnEligibilityRequest request,
        CancellationToken cancellationToken);
}
