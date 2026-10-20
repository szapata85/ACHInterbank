using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseCyclePolicyResolver
{
    Task<ResolvedClearingHouseCyclePolicy> ResolveAsync(int clearingHouseId, DateTime operationalDate, CancellationToken ct = default);
    Task<ResolvedClearingHouseCyclePolicy> ResolveAtInstantAsync(int clearingHouseId, DateTimeOffset instant, CancellationToken ct = default);
}
