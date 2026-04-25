namespace Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

public enum PaymentRailCapabilityKind
{
    Cycle = 1,
    Dispatch = 2,
    Return = 3,
    Netting = 4,
    Liquidity = 5
}

public enum PaymentRailCapabilityExecutionMode
{
    NotSupported = 0,
    LegacyOwner = 1,
    WrapperPassive = 2,
    StrategyOwnerPlanned = 3
}

public sealed record PaymentRailCapabilityStatus(
    PaymentRailCapabilityKind Capability,
    bool IsSupported,
    PaymentRailCapabilityExecutionMode ExecutionMode,
    bool ShadowCompareReady,
    string LegacyOwner,
    string Notes);

public sealed record PaymentRailWrapperCallRequest(
    PaymentRailOperationalContext Context,
    PaymentRailCapabilityKind Capability,
    string LegacyDecisionCode,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record PaymentRailWrapperCallResult(
    string RailCode,
    PaymentRailCapabilityKind Capability,
    bool IsCapabilitySupported,
    bool UseLegacyDecision,
    bool BehaviorChanged,
    bool ShadowCompareReady,
    string WrapperDecisionCode,
    string Message);
