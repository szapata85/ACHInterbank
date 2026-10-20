using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitIncomingReturnPolicy
{
    CenitIncomingReturnPolicyResult Evaluate(CenitIncomingReturnPolicyRequest request);
}
