using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IContrapartidaDispatchPersistenceService
{
    Task<ContrapartidaDispatchItem> EnsurePendingDispatchAsync(
        AchTransaction transaction,
        int clearingHouseId,
        CancellationToken ct = default);
    Task<ContrapartidaDispatchBatch> CreateBatchAsync(ContrapartidaDispatchBatchCreateRequest request, CancellationToken ct = default);
    Task<ContrapartidaDispatchAttempt> RegisterAttemptAsync(ContrapartidaDispatchAttemptCreateRequest request, CancellationToken ct = default);
}
