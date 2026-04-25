using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class AchColombiaPaymentRailOperationalStrategy : IPaymentRailOperationalStrategy
{
    public string RailCode => PaymentRailCodes.AchColombia;

    public PaymentRailCapabilityDescriptor Capabilities { get; } = new(
        SupportsCycleOperations: true,
        SupportsDispatchOperations: true,
        SupportsReturnsOperations: true,
        SupportsNettingOperations: false,
        SupportsLiquidityOperations: false,
        SupportsObservability: true,
        Notes: "Bridge Fase 1: sin mover lógica existente de ACH Colombia.");

    public bool CanHandle(string railCode) => string.Equals(railCode, RailCode, StringComparison.OrdinalIgnoreCase);

    public PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request)
    {
        return new PaymentRailBridgeResult(
            IsAllowed: true,
            RailCode,
            ResultCode: "PAYMENT_RAIL_BRIDGE_READY_ACH_COLOMBIA",
            Message: "Estrategia ACH Colombia registrada en modo bridge (fase 1, no-op operacional).");
    }
}
