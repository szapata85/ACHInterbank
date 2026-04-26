using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public abstract class PaymentRailOperationalStrategyBase : IPaymentRailOperationalStrategy
{
    public abstract string RailCode { get; }
    public abstract PaymentRailCapabilityDescriptor Capabilities { get; }
    public abstract IReadOnlyCollection<PaymentRailCapabilityStatus> CapabilityStatuses { get; }

    public bool CanHandle(string railCode) => string.Equals(railCode, RailCode, StringComparison.OrdinalIgnoreCase);

    public abstract PaymentRailBridgeResult EvaluateBridge(PaymentRailBridgeRequest request);

    public virtual PaymentRailWrapperCallResult EvaluateCapabilityWrapper(PaymentRailWrapperCallRequest request)
    {
        var capabilityStatus = CapabilityStatuses.FirstOrDefault(x => x.Capability == request.Capability)
            ?? new PaymentRailCapabilityStatus(request.Capability, false, PaymentRailCapabilityExecutionMode.NotSupported, false, "N/A", "Capability no configurada para el riel.");

        if (!capabilityStatus.IsSupported)
        {
            return new PaymentRailWrapperCallResult(
                RailCode,
                request.Capability,
                IsCapabilitySupported: false,
                UseLegacyDecision: true,
                BehaviorChanged: false,
                ShadowCompareReady: capabilityStatus.ShadowCompareReady,
                WrapperDecisionCode: "PAYMENT_RAIL_WRAPPER_NOT_SUPPORTED",
                Message: $"Capability {request.Capability} no soportada por riel {RailCode}.");
        }

        return new PaymentRailWrapperCallResult(
            RailCode,
            request.Capability,
            IsCapabilitySupported: true,
            UseLegacyDecision: true,
            BehaviorChanged: false,
            ShadowCompareReady: capabilityStatus.ShadowCompareReady,
            WrapperDecisionCode: "PAYMENT_RAIL_WRAPPER_PASSIVE",
            Message: $"Wrapper pasivo para {request.Capability} en riel {RailCode}; se mantiene decisión legacy '{request.LegacyDecisionCode}'.");
    }

    public virtual PaymentRailShadowCompareSnapshot BuildCapabilityShadowSnapshot(PaymentRailWrapperCallRequest request, PaymentRailWrapperCallResult wrapperResult)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.Context.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.Context.CorrelationId;

        return new PaymentRailShadowCompareSnapshot(
            LegacySource: request.Capability.ToString(),
            LegacyValue: request.LegacyDecisionCode,
            RailCode: wrapperResult.RailCode,
            IsKnownRail: !string.Equals(wrapperResult.RailCode, PaymentRailCodes.Unknown, StringComparison.OrdinalIgnoreCase),
            StrategyRailCode: RailCode,
            CorrelationId: correlationId,
            CreatedAtUtc: DateTime.UtcNow);
    }
}
