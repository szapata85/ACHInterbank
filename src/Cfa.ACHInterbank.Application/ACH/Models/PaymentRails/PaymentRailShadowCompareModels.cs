namespace Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

public sealed record PaymentRailShadowCompareResult(
    bool IsEquivalent,
    string ComparisonCode,
    string LegacyDecisionCode,
    string WrapperDecisionCode,
    string RailCode,
    string Capability,
    string Notes,
    DateTime ComparedAtUtc);
