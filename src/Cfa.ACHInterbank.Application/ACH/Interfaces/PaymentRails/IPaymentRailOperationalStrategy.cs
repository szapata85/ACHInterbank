using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;

public interface IPaymentRailOperationalStrategy
{
    string RailCode { get; }
    PaymentRailCapabilityDescriptor Capabilities { get; }

    bool CanHandle(string railCode);
    PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request);
}
