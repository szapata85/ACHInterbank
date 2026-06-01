using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapResiliencePolicyEvaluator
{
    NachaSoapFailureClassification ClassifyFailure(
        NachaSoapExecutionResult result,
        NachaSoapRetryPolicy policy);

    bool ShouldRetry(
        NachaSoapExecutionResult result,
        NachaSoapRetryPolicy policy,
        int attemptNumber);

    int CalculateDelayMs(
        NachaSoapRetryPolicy policy,
        int attemptNumber);
}
