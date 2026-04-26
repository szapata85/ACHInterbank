using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class PaymentRailContextService : IPaymentRailContextService
{
    private readonly IPaymentRailOperationalStrategyResolver _resolver;

    public PaymentRailContextService(IPaymentRailOperationalStrategyResolver resolver)
    {
        _resolver = resolver;
    }

    public PaymentRailResolvedContext ResolveContext(
        int? clearingHouseId,
        string? clearingHouseCode,
        string? achCycleId,
        DateTime? operationalDate,
        string? requestedRailCode = null,
        string? correlationId = null)
    {
        var request = new PaymentRailResolveRequest(clearingHouseId, clearingHouseCode, requestedRailCode);
        var resolution = _resolver.ResolveRail(request);
        var strategy = _resolver.ResolveStrategy(request);

        var context = new PaymentRailOperationalContext(
            ClearingHouseId: clearingHouseId,
            ClearingHouseCode: clearingHouseCode,
            AchCycleId: achCycleId,
            OperationalDate: operationalDate,
            CorrelationId: string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId);

        return new PaymentRailResolvedContext(
            RailCode: resolution.RailCode,
            IsKnownRail: resolution.IsKnownRail,
            ResolutionSource: resolution.ResolutionSource,
            ResolutionMessage: resolution.Message,
            StrategyRailCode: strategy.RailCode,
            Capabilities: strategy.Capabilities,
            CapabilityStatuses: strategy.CapabilityStatuses,
            OperationalContext: context);
    }

    public PaymentRailShadowCompareSnapshot BuildShadowSnapshot(
        PaymentRailResolvedContext resolvedContext,
        string legacySource,
        string legacyValue)
    {
        return new PaymentRailShadowCompareSnapshot(
            LegacySource: legacySource,
            LegacyValue: legacyValue,
            RailCode: resolvedContext.RailCode,
            IsKnownRail: resolvedContext.IsKnownRail,
            StrategyRailCode: resolvedContext.StrategyRailCode,
            CorrelationId: resolvedContext.OperationalContext.CorrelationId,
            CreatedAtUtc: DateTime.UtcNow);
    }
}
