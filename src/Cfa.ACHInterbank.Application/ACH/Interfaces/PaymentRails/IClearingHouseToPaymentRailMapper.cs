using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;

public interface IClearingHouseToPaymentRailMapper
{
    PaymentRailResolveResult ResolveRail(PaymentRailResolveRequest request);
}
