using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IContrapartidaDispatchJobService
{
    Task<ContrapartidaCycleDispatchResult> ProcessCycleAsync(
        string cycleId,
        int clearingHouseId,
        string triggeredBy,
        int chunkSize,
        CancellationToken ct = default);

    Task<ContrapartidaCycleDispatchResult> ProcessTransactionAsync(
        string cycleId,
        int clearingHouseId,
        int transactionId,
        string triggeredBy,
        CancellationToken ct = default);

    Task<ContrapartidaBatchRetryResult> RetryBatchAsync(
        ContrapartidaDispatchRetryRequest request,
        CancellationToken ct = default);
}
