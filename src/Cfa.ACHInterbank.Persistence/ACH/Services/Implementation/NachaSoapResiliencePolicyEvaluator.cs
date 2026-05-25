using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapResiliencePolicyEvaluator : INachaSoapResiliencePolicyEvaluator
{
    public NachaSoapFailureClassification ClassifyFailure(
        NachaSoapExecutionResult result,
        NachaSoapRetryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(policy);

        if (result.Status == NachaSoapExecutionStatus.SimulatedSuccess
            || result.Status == NachaSoapExecutionStatus.DryRunCompleted)
        {
            return NachaSoapFailureClassification.None;
        }

        if (result.Status == NachaSoapExecutionStatus.BlockedByNoGo)
        {
            return NachaSoapFailureClassification.SecurityBlocked;
        }

        if (result.Status == NachaSoapExecutionStatus.Skipped)
        {
            return NachaSoapFailureClassification.ValidationFailure;
        }

        if (result.IsTimeout || result.Status == NachaSoapExecutionStatus.SimulatedTimeout)
        {
            return NachaSoapFailureClassification.Timeout;
        }

        if (result.IsSoapFault || result.Status == NachaSoapExecutionStatus.SimulatedSoapFault)
        {
            return IsNonRetryable(result.SoapFaultCode, policy)
                ? NachaSoapFailureClassification.NonRetryableFailure
                : NachaSoapFailureClassification.SoapFault;
        }

        if (result.Status == NachaSoapExecutionStatus.Rejected)
        {
            return IsSecurityMessage(result.Message)
                ? NachaSoapFailureClassification.SecurityBlocked
                : NachaSoapFailureClassification.ValidationFailure;
        }

        return IsNonRetryable(result.ResponseCode, policy)
            ? NachaSoapFailureClassification.NonRetryableFailure
            : NachaSoapFailureClassification.TransientFailure;
    }

    public bool ShouldRetry(
        NachaSoapExecutionResult result,
        NachaSoapRetryPolicy policy,
        int attemptNumber)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(policy);

        var maxAttempts = Math.Max(1, policy.MaxAttempts);
        if (attemptNumber >= maxAttempts)
        {
            return false;
        }

        return ClassifyFailure(result, policy) switch
        {
            NachaSoapFailureClassification.Timeout => policy.RetryOnTimeout,
            NachaSoapFailureClassification.SoapFault => policy.RetryOnSoapFault && IsTransientCode(result.SoapFaultCode),
            NachaSoapFailureClassification.TransientFailure => policy.RetryOnTransientFailure,
            _ => false
        };
    }

    public int CalculateDelayMs(
        NachaSoapRetryPolicy policy,
        int attemptNumber)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var baseDelay = Math.Max(0, policy.BaseDelayMs);
        if (baseDelay == 0)
        {
            return 0;
        }

        var multiplier = policy.UseExponentialBackoff
            ? Math.Pow(2, Math.Max(0, attemptNumber - 1))
            : 1;
        var calculated = (int)Math.Min(int.MaxValue, baseDelay * multiplier);
        var maxDelay = Math.Max(0, policy.MaxDelayMs);
        return maxDelay == 0 ? calculated : Math.Min(calculated, maxDelay);
    }

    private static bool IsNonRetryable(string code, NachaSoapRetryPolicy policy)
        => !string.IsNullOrWhiteSpace(code)
           && policy.NonRetryableErrorCodes.Contains(code);

    private static bool IsTransientCode(string code)
        => code.Contains("TRANSIENT", StringComparison.OrdinalIgnoreCase)
           || code.Contains("TEMP", StringComparison.OrdinalIgnoreCase)
           || code.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase);

    private static bool IsSecurityMessage(string message)
        => message.Contains("ProductiveExecution", StringComparison.OrdinalIgnoreCase)
           || message.Contains("AllowExternalSoapInvocation", StringComparison.OrdinalIgnoreCase)
           || message.Contains("sensibles", StringComparison.OrdinalIgnoreCase);
}
