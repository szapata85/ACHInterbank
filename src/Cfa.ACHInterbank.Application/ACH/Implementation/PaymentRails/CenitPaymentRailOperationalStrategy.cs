using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class CenitPaymentRailOperationalStrategy : IPaymentRailOperationalStrategy
{
    public string RailCode => PaymentRailCodes.Cenit;

    public PaymentRailCapabilityDescriptor Capabilities { get; } = new(
        SupportsCycleOperations: true,
        SupportsDispatchOperations: true,
        SupportsReturnsOperations: true,
        SupportsNettingOperations: true,
        SupportsLiquidityOperations: true,
        SupportsObservability: true,
        Notes: "Bridge Fase 1: sin mover lógica existente de CENIT netting/liquidez.");

    public bool CanHandle(string railCode) => string.Equals(railCode, RailCode, StringComparison.OrdinalIgnoreCase);

    public PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request)
    {
        return new PaymentRailBridgeResult(
            IsAllowed: true,
            RailCode,
            ResultCode: "PAYMENT_RAIL_BRIDGE_READY_CENIT",
            Message: "Estrategia CENIT registrada en modo bridge (fase 1, no-op operacional).");
    }
}
