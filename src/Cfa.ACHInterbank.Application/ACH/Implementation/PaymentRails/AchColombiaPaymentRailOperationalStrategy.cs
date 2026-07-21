using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class AchColombiaPaymentRailOperationalStrategy : PaymentRailOperationalStrategyBase
{
    public override string RailCode => PaymentRailCodes.AchColombia;
    public override string DisplayName => "ACH Colombia";

    public override PaymentRailCapabilityDescriptor Capabilities { get; } = new(
        SupportsCycleOperations: true,
        SupportsDispatchOperations: true,
        SupportsReturnsOperations: true,
        SupportsNettingOperations: false,
        SupportsLiquidityOperations: false,
        SupportsObservability: true,
        Notes: "Wrapper fase 4: capacidades ACH Colombia activas en modo pasivo con owner legacy.");

    public override IReadOnlyCollection<PaymentRailCapabilityStatus> CapabilityStatuses { get; } =
    [
        new(PaymentRailCapabilityKind.Cycle, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy-AchCycleScheduler/Routing", "Resolución de ciclo sigue legacy; wrapper sólo transporta contexto."),
        new(PaymentRailCapabilityKind.Dispatch, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy-IncomingNachaDispatch", "Dispatch sigue legacy; wrapper preparado para shadow compare."),
        new(PaymentRailCapabilityKind.Return, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy-AchReturns", "Returns sigue legacy."),
        new(PaymentRailCapabilityKind.Netting, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "ACH Colombia no opera netting dedicado en este alcance."),
        new(PaymentRailCapabilityKind.Liquidity, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "ACH Colombia no opera optimización de liquidez dedicada en este alcance.")
    ];

    public override PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request)
    {
        return new PaymentRailBridgeResult(
            IsAllowed: true,
            RailCode,
            ResultCode: "PAYMENT_RAIL_BRIDGE_READY_ACH_COLOMBIA",
            Message: "Wrapper ACH Colombia activo en modo pasivo (owner legacy).");
    }
}
