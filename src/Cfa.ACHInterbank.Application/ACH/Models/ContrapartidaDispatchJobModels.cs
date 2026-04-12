namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum ContrapartidaDispatchRetryScope
{
    Full = 1,
    FailedOnly = 2
}

public sealed record ContrapartidaDispatchRetryRequest(
    Guid SourceBatchId,
    ContrapartidaDispatchRetryScope Scope,
    string TriggeredBy,
    int ChunkSize = 300,
    bool AllowReplaySucceeded = false);

public sealed record ContrapartidaCycleDispatchResult(
    string CycleId,
    int ClearingHouseId,
    int Processed,
    int Succeeded,
    int Failed,
    int Partial,
    int Chunks,
    string Summary);

public sealed record ContrapartidaBatchRetryResult(
    Guid SourceBatchId,
    Guid NewBatchId,
    string CycleId,
    int ClearingHouseId,
    int Selected,
    int Processed,
    int Succeeded,
    int Failed,
    int Partial,
    string Summary);
