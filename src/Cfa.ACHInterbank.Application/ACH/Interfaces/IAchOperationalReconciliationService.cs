using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchOperationalReconciliationService
{
    Task<AchOperationalReconciliationResult> ReconcileAsync(AchOperationalReconciliationRequest request, CancellationToken ct = default);
    Task<AchOperationalReconciliationSnapshot?> GetLatestAsync(int clearingHouseId, DateOnly operationalDate, string achCycleId, CancellationToken ct = default);
}
