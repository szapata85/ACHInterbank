namespace Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;

/// <summary>Consumes persisted reprocess attempts; scheduling is deliberately outside this boundary.</summary>
public interface IAchResponseReprocessDispatcher
{
    Task<AchResponseReprocessDispatchResult> DispatchAsync(int batchSize, TimeSpan leaseDuration,
        string instanceId, CancellationToken cancellationToken = default);
}
