using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICycleTransactionPolicy
{
    CycleTransactionPolicyResult Evaluate(CycleTransactionPolicyRequest request);
}

