using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public static class ThirdPartyAccountShadowClassifier
{
    public static ThirdPartyAccountShadowDiscrepancyType Classify(ThirdPartyAccountShadowSnapshot snapshot)
    {
        if (!snapshot.LegacyExists && snapshot.CanonicalExists)
        {
            return ThirdPartyAccountShadowDiscrepancyType.CanonicalOnly;
        }

        if (snapshot.LegacyExists && !snapshot.IdentityComplete)
        {
            return ThirdPartyAccountShadowDiscrepancyType.IdentityIncomplete;
        }

        if (snapshot.LegacyExists && !snapshot.CanonicalExists)
        {
            return ThirdPartyAccountShadowDiscrepancyType.LegacyOnly;
        }

        if (!snapshot.LegacyExists || !snapshot.CanonicalExists)
        {
            return ThirdPartyAccountShadowDiscrepancyType.IdentityIncomplete;
        }

        if (!snapshot.PolicyContextComplete)
        {
            return ThirdPartyAccountShadowDiscrepancyType.PolicyContextMissing;
        }

        if (!ValidationStatusMatches(snapshot.LegacyStatus, snapshot.CanonicalValidationStatus))
        {
            return ThirdPartyAccountShadowDiscrepancyType.StatusMismatch;
        }

        if (snapshot.CanonicalEligibilityDecision.HasValue
            && !EligibilityMatches(snapshot.LegacyStatus, snapshot.CanonicalEligibilityDecision.Value))
        {
            return ThirdPartyAccountShadowDiscrepancyType.EligibilityMismatch;
        }

        return ThirdPartyAccountShadowDiscrepancyType.Match;
    }

    private static bool ValidationStatusMatches(
        CustomerThirdPartyStatusEnum? legacy,
        ThirdPartyAccountValidationStatus? canonical)
        => legacy switch
        {
            CustomerThirdPartyStatusEnum.Pending => canonical is ThirdPartyAccountValidationStatus.PendingSend
                or ThirdPartyAccountValidationStatus.PendingResponse,
            CustomerThirdPartyStatusEnum.Active => canonical is ThirdPartyAccountValidationStatus.Accepted
                or ThirdPartyAccountValidationStatus.TacitlyAccepted,
            CustomerThirdPartyStatusEnum.Rejected => canonical == ThirdPartyAccountValidationStatus.Rejected,
            _ => false
        };

    private static bool EligibilityMatches(
        CustomerThirdPartyStatusEnum? legacy,
        ThirdPartyAccountEligibilityDecision canonical)
        => legacy switch
        {
            CustomerThirdPartyStatusEnum.Pending => canonical == ThirdPartyAccountEligibilityDecision.PrenotificationPending,
            CustomerThirdPartyStatusEnum.Active => canonical == ThirdPartyAccountEligibilityDecision.Allowed,
            CustomerThirdPartyStatusEnum.Rejected => canonical == ThirdPartyAccountEligibilityDecision.Rejected,
            _ => false
        };
}
