namespace Cfa.ACHInterbank.Application.ACH.Services;

/// <summary>I05: regla temporal pura; no cambia la fecha del sistema ni persiste estados.</summary>
public static class PrenotificationTacitAcceptanceDecision
{
    public static bool IsDue(DateTimeOffset? canonicalDeadlineAtUtc, DateTimeOffset evaluationTimeUtc,
        bool commonEligibility, bool effectivePolicyEnabled, bool hasGeneratedReturn)
        => canonicalDeadlineAtUtc.HasValue && evaluationTimeUtc >= canonicalDeadlineAtUtc.Value
           && commonEligibility && effectivePolicyEnabled && !hasGeneratedReturn;
    /// <summary>
    /// APN-I09. A persisted deadline is a lower bound only: an amended chamber
    /// calendar cannot make an old prenotification tacitly accepted earlier.
    /// Lack of confirmed transmission, an outstanding inbound file or response
    /// evidence always blocks the transition (fail closed).
    /// </summary>
    public static bool IsOperationallySafe(
        DateTimeOffset? storedDeadlineAtUtc,
        DateTimeOffset? recalculatedDeadlineAtUtc,
        DateTimeOffset evaluationTimeUtc,
        bool validCurrentAttempt,
        bool confirmedTransmission,
        bool inboundProcessingPending,
        bool hasAnyResponseEvidence)
    {
        if (!validCurrentAttempt || !confirmedTransmission
            || inboundProcessingPending || hasAnyResponseEvidence
            || !recalculatedDeadlineAtUtc.HasValue)
            return false;

        var authoritativeDeadline = storedDeadlineAtUtc.HasValue
            && storedDeadlineAtUtc.Value > recalculatedDeadlineAtUtc.Value
            ? storedDeadlineAtUtc.Value
            : recalculatedDeadlineAtUtc.Value;
        return evaluationTimeUtc >= authoritativeDeadline;
    }
}
