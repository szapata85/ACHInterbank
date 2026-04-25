using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class CenitPaymentRailOperationalStrategy : PaymentRailOperationalStrategyBase
{
    public override string RailCode => PaymentRailCodes.Cenit;

    public override PaymentRailCapabilityDescriptor Capabilities { get; } = new(
        SupportsCycleOperations: true,
        SupportsDispatchOperations: true,
        SupportsReturnsOperations: true,
        SupportsNettingOperations: true,
        SupportsLiquidityOperations: true,
        SupportsObservability: true,
        Notes: "Wrapper fase 4: capacidades CENIT activas en modo pasivo con owner legacy.");

    public override IReadOnlyCollection<PaymentRailCapabilityStatus> CapabilityStatuses { get; } =
    [
        new(PaymentRailCapabilityKind.Cycle, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy-AchCycleScheduler/Routing", "Resolución de ciclo sigue legacy; wrapper sólo transporta contexto."),
        new(PaymentRailCapabilityKind.Dispatch, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy-IncomingNachaDispatch", "Dispatch sigue legacy; wrapper preparado para shadow compare."),
        new(PaymentRailCapabilityKind.Return, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy-AchReturns/ReturnOfReturn", "Returns/return-of-return sigue legacy."),
        new(PaymentRailCapabilityKind.Netting, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy-CenitNetting", "Neteo CENIT sigue legacy; wrapper sólo expone contrato."),
        new(PaymentRailCapabilityKind.Liquidity, true, PaymentRailCapabilityExecutionMode.WrapperPassive, true, "Legacy-LiquidityOptimization", "Liquidez CENIT sigue legacy; wrapper sólo expone contrato.")
    ];

    public override PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request)
    {
        return new PaymentRailBridgeResult(
            IsAllowed: true,
            RailCode,
            ResultCode: "PAYMENT_RAIL_BRIDGE_READY_CENIT",
            Message: "Wrapper CENIT activo en modo pasivo (owner legacy)."
        );
    }
}
