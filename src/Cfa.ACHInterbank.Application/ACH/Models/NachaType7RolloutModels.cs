namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaType7RolloutDecision
{
    public bool AllowLegacyFallback { get; init; } = true;
    public bool EligibleToDisableFallback { get; init; }
    public decimal EquivalenceRatePercent { get; init; }
    public int QualifiedRuns { get; init; }
    public List<string> Reasons { get; init; } = [];
}
