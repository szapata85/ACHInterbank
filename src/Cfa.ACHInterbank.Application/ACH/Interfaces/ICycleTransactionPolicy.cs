using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICycleTransactionPolicy
{
    Task<CycleTransactionPolicyResult> EvaluateAsync(CycleTransactionPolicyRequest request, CancellationToken ct = default);
}

