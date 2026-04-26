namespace Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

public enum PaymentRailCapabilityRegistryState
{
    Enabled = 1,
    Disabled = 2,
    ShadowOnly = 3,
    NotSupported = 4,
    Planned = 5
}

public static class PaymentRailCapabilityRegistryCodes
{
    public const string CycleResolution = "CycleResolution";
    public const string DispatchEligibility = "DispatchEligibility";
    public const string DispatchPlanning = "DispatchPlanning";
    public const string Returns = "Returns";
    public const string ReturnOfReturn = "ReturnOfReturn";
    public const string Netting = "Netting";
    public const string Liquidity = "Liquidity";
    public const string NachaM = "NachaM";
    public const string InboundFiles = "InboundFiles";
    public const string OutboundFiles = "OutboundFiles";
    public const string Prenotifications = "Prenotifications";
    public const string CrossBorderPayments = "CrossBorderPayments";
    public const string FxValidation = "FxValidation";
    public const string SanctionsScreening = "SanctionsScreening";

    public static readonly IReadOnlyList<string> All =
    [
        CycleResolution,
        DispatchEligibility,
        DispatchPlanning,
        Returns,
        ReturnOfReturn,
        Netting,
        Liquidity,
        NachaM,
        InboundFiles,
        OutboundFiles,
        Prenotifications,
        CrossBorderPayments,
        FxValidation,
        SanctionsScreening
    ];
}

public static class PaymentRailCapabilityRegistrySources
{
    public const string RegistryOverride = "RegistryOverride";
    public const string StrategyDefault = "StrategyDefault";
}

public sealed record PaymentRailRegistryRailItem(
    string RailCode,
    string DisplayName,
    bool IsKnownRail,
    bool IsOperational,
    string Source,
    string Version);

public sealed record PaymentRailCapabilityRegistryItem(
    string RailCode,
    string CapabilityCode,
    PaymentRailCapabilityRegistryState State,
    string Source,
    string? Notes,
    DateTime EvaluatedAtUtc,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    string Version = "v1",
    string? ChangeSource = null,
    string? ChangeTicket = null,
    string? ChangedBy = null,
    DateTimeOffset? ChangedAtUtc = null);

public sealed record UpsertPaymentRailCapabilityRegistryRequest(
    string RailCode,
    string CapabilityCode,
    PaymentRailCapabilityRegistryState State,
    string ChangedBy,
    string? ChangeTicket,
    string? Notes,
    DateTime? EffectiveFromUtc = null);
