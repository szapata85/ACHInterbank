using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;

public interface IPaymentRailOperationalStrategyResolver
{
    PaymentRailResolveResult ResolveRail(PaymentRailResolveRequest request);
    IPaymentRailOperationalStrategy ResolveStrategy(PaymentRailResolveRequest request);
}
