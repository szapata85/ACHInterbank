using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class UnknownPaymentRailOperationalStrategy : IPaymentRailOperationalStrategy
{
    public string RailCode => PaymentRailCodes.Unknown;

    public PaymentRailCapabilityDescriptor Capabilities { get; } = new(
        SupportsCycleOperations: false,
        SupportsDispatchOperations: false,
        SupportsReturnsOperations: false,
        SupportsNettingOperations: false,
        SupportsLiquidityOperations: false,
        SupportsObservability: true,
        Notes: "Fail-closed: riel desconocido o mapping no resuelto.");

    public bool CanHandle(string railCode) => string.Equals(railCode, RailCode, StringComparison.OrdinalIgnoreCase);

    public PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request)
    {
        return new PaymentRailBridgeResult(
            IsAllowed: false,
            RailCode,
            ResultCode: "PAYMENT_RAIL_UNKNOWN_FAIL_CLOSED",
            Message: "No existe estrategia operacional para el riel solicitado. Operación denegada por fail-closed.");
    }
}
