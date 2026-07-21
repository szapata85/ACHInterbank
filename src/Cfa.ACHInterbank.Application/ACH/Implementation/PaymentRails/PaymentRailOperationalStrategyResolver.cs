using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;

public sealed class PaymentRailOperationalStrategyResolver : IPaymentRailOperationalStrategyResolver
{
    private readonly IClearingHouseToPaymentRailMapper _mapper;
    private readonly IReadOnlyDictionary<string, IPaymentRailOperationalStrategy> _strategyByRail;
    private readonly IPaymentRailOperationalStrategy _unknownStrategy;

    public PaymentRailOperationalStrategyResolver(
        IClearingHouseToPaymentRailMapper mapper,
        IEnumerable<IPaymentRailOperationalStrategy> strategies)
    {
        _mapper = mapper;
        var strategyList = strategies.ToList();
        _strategyByRail = strategyList
            .GroupBy(x => x.RailCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        _unknownStrategy = strategyList.FirstOrDefault(x => string.Equals(x.RailCode, PaymentRailCodes.Unknown, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Debe registrarse UnknownPaymentRailOperationalStrategy para fail-closed.");
    }

    public PaymentRailResolveResult ResolveRail(PaymentRailResolveRequest request)
    {
        var result = _mapper.ResolveRail(request);
        if (!result.IsKnownRail
            || string.Equals(result.RailCode, PaymentRailCodes.Unknown, StringComparison.OrdinalIgnoreCase)
            || !_strategyByRail.ContainsKey(result.RailCode))
        {
            return new PaymentRailResolveResult(
                PaymentRailCodes.Unknown,
                false,
                "Unknown",
                "No existe una estrategia operativa registrada para el código solicitado.");
        }

        return result;
    }

    public IPaymentRailOperationalStrategy ResolveStrategy(PaymentRailResolveRequest request)
    {
        var rail = ResolveRail(request);
        return _strategyByRail.TryGetValue(rail.RailCode, out var strategy)
            ? strategy
            : _unknownStrategy;
    }
}
