namespace Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

public sealed record PaymentRailResolvedContext(
    string RailCode,
    bool IsKnownRail,
    string ResolutionSource,
    string ResolutionMessage,
    string StrategyRailCode,
    PaymentRailCapabilityDescriptor Capabilities,
    IReadOnlyCollection<PaymentRailCapabilityStatus> CapabilityStatuses,
    PaymentRailOperationalContext OperationalContext);

public sealed record PaymentRailShadowCompareSnapshot(
    string LegacySource,
    string LegacyValue,
    string RailCode,
    bool IsKnownRail,
    string StrategyRailCode,
    string CorrelationId,
    DateTime CreatedAtUtc);
