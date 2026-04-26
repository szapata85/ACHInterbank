namespace Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

public sealed record PaymentRailCapabilityDescriptor(
    bool SupportsCycleOperations,
    bool SupportsDispatchOperations,
    bool SupportsReturnsOperations,
    bool SupportsNettingOperations,
    bool SupportsLiquidityOperations,
    bool SupportsObservability,
    string Notes);
