using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;

public interface IPaymentRailContextService
{
    PaymentRailResolvedContext ResolveContext(
        int? clearingHouseId,
        string? clearingHouseCode,
        string? achCycleId,
        DateTime? operationalDate,
        string? requestedRailCode = null,
        string? correlationId = null);

    PaymentRailShadowCompareSnapshot BuildShadowSnapshot(
        PaymentRailResolvedContext resolvedContext,
        string legacySource,
        string legacyValue);
}
