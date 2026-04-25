using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class UnknownPaymentRailOperationalStrategy : PaymentRailOperationalStrategyBase
{
    public override string RailCode => PaymentRailCodes.Unknown;

    public override PaymentRailCapabilityDescriptor Capabilities { get; } = new(
        SupportsCycleOperations: false,
        SupportsDispatchOperations: false,
        SupportsReturnsOperations: false,
        SupportsNettingOperations: false,
        SupportsLiquidityOperations: false,
        SupportsObservability: true,
        Notes: "Fail-closed: riel desconocido o mapping no resuelto.");

    public override IReadOnlyCollection<PaymentRailCapabilityStatus> CapabilityStatuses { get; } =
    [
        new(PaymentRailCapabilityKind.Cycle, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Riel desconocido."),
        new(PaymentRailCapabilityKind.Dispatch, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Riel desconocido."),
        new(PaymentRailCapabilityKind.Return, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Riel desconocido."),
        new(PaymentRailCapabilityKind.Netting, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Riel desconocido."),
        new(PaymentRailCapabilityKind.Liquidity, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Riel desconocido.")
    ];

    public override PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request)
    {
        return new PaymentRailBridgeResult(
            IsAllowed: false,
            RailCode,
            ResultCode: "PAYMENT_RAIL_UNKNOWN_FAIL_CLOSED",
            Message: "No existe estrategia operacional para el riel solicitado. Operación denegada por fail-closed.");
    }

    public override PaymentRailWrapperCallResult EvaluateCapabilityWrapper(PaymentRailWrapperCallRequest request)
    {
        return new PaymentRailWrapperCallResult(
            RailCode,
            request.Capability,
            IsCapabilitySupported: false,
            UseLegacyDecision: true,
            BehaviorChanged: false,
            ShadowCompareReady: false,
            WrapperDecisionCode: "PAYMENT_RAIL_WRAPPER_UNKNOWN_FAIL_CLOSED",
            Message: "No existe estrategia de wrapper para riel desconocido; se conserva decisión legacy por seguridad.");
    }
}
