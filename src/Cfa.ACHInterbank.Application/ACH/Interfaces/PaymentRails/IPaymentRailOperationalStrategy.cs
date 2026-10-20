using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;

public interface IPaymentRailOperationalStrategy
{
    string RailCode { get; }
    string DisplayName { get; }
    bool IsAdministrativelySelectable { get; }
    PaymentRailCapabilityDescriptor Capabilities { get; }
    IReadOnlyCollection<PaymentRailCapabilityStatus> CapabilityStatuses { get; }

    bool CanHandle(string railCode);
    PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request);
    PaymentRailWrapperCallResult EvaluateCapabilityWrapper(PaymentRailWrapperCallRequest request);
    PaymentRailShadowCompareSnapshot BuildCapabilityShadowSnapshot(PaymentRailWrapperCallRequest request, PaymentRailWrapperCallResult wrapperResult);
}
