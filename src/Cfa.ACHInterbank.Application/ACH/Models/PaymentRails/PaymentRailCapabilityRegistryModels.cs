namespace Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

/// <summary>Estado publicado para una capacidad de un riel de pago.</summary>
public enum PaymentRailCapabilityRegistryState
{
    /// <summary>Capacidad habilitada.</summary>
    Enabled = 1,

    /// <summary>Capacidad deshabilitada.</summary>
    Disabled = 2,

    /// <summary>Capacidad disponible únicamente en modo shadow/pasivo.</summary>
    ShadowOnly = 3,

    /// <summary>Capacidad no soportada por el riel.</summary>
    NotSupported = 4,

    /// <summary>Capacidad prevista pero no asumida como habilitada por la estrategia actual.</summary>
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

/// <summary>Riel publicado por el catálogo de Capability Registry.</summary>
/// <param name="RailCode">Código del riel usado en las rutas de consulta de capacidades.</param>
/// <param name="DisplayName">Nombre visible definido por el catálogo del servicio.</param>
/// <param name="IsKnownRail">Indica si el catálogo considera el riel conocido.</param>
/// <param name="IsOperational">Indica si el catálogo marca el riel como operacional.</param>
/// <param name="Source">Origen del elemento de catálogo; el servicio actual publica StrategyCatalog.</param>
/// <param name="Version">Versión informativa del catálogo publicada por el servicio.</param>
public sealed record PaymentRailRegistryRailItem(
    string RailCode,
    string DisplayName,
    bool IsKnownRail,
    bool IsOperational,
    string Source,
    string Version);

/// <summary>Estado efectivo de una capacidad para un riel en el instante evaluado.</summary>
/// <param name="RailCode">Código normalizado del riel evaluado.</param>
/// <param name="CapabilityCode">Código de capacidad perteneciente a PaymentRailCapabilityRegistryCodes.</param>
/// <param name="State">Estado efectivo calculado para la capacidad.</param>
/// <param name="Source">Origen del estado: RegistryOverride cuando existe un override vigente o StrategyDefault cuando se deriva de la estrategia operacional.</param>
/// <param name="Notes">Notas del override persistido o texto descriptivo generado por la estrategia cuando no existe override.</param>
/// <param name="EvaluatedAtUtc">Marca UTC en la que el servicio construyó el resultado.</param>
/// <param name="EffectiveFromUtc">Inicio UTC de vigencia cuando el resultado proviene de un override; null para el valor derivado de estrategia.</param>
/// <param name="EffectiveToUtc">Fin UTC de vigencia del override cuando existe; puede ser null para una vigencia abierta o para el valor derivado de estrategia.</param>
/// <param name="Version">Identificador informativo de la fuente efectiva, derivado del registro persistido o de la estrategia del riel.</param>
/// <param name="ChangeSource">Origen del cambio persistido cuando el resultado proviene de un override.</param>
/// <param name="ChangeTicket">Referencia de cambio asociada al override cuando fue registrada.</param>
/// <param name="ChangedBy">Identidad almacenada como responsable del override cuando aplica.</param>
/// <param name="ChangedAtUtc">Marca temporal persistida del último cambio del override cuando aplica.</param>
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
