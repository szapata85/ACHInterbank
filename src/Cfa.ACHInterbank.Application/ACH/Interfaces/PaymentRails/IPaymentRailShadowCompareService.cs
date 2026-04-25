using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;

public interface IPaymentRailShadowCompareService
{
    PaymentRailShadowCompareResult CompareCycleResolution(
        PaymentRailResolvedContext resolvedContext,
        PaymentRailWrapperCallResult wrapperResult,
        string legacyDecisionCode,
        bool legacyResolved);

    PaymentRailShadowCompareResult CompareDispatchPlanning(
        PaymentRailResolvedContext resolvedContext,
        PaymentRailWrapperCallResult wrapperResult,
        string legacyDecisionCode,
        bool legacyEligible,
        bool legacyWaitingWindow,
        int legacyPriority);
}
