using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICfaOperationalTransactionRestrictionResolver
{
    Task<CfaOperationalTransactionRestrictionResolution> ResolveAsync(
        CfaOperationalTransactionRestrictionRequest request,
        CancellationToken ct = default);
}
