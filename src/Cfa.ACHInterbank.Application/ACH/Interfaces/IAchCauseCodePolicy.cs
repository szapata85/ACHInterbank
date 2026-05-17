using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchCauseCodePolicy
{
    Task<AchCauseCodePolicyResult> EvaluateAsync(AchCauseCodePolicyRequest request, CancellationToken ct = default);
}
