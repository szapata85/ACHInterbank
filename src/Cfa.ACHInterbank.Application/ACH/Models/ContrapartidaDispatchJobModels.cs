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

/// <summary>Resultado agregado del procesamiento de contrapartidas para un ciclo y cámara.</summary>
/// <param name="CycleId">Identificador del ciclo resuelto para la transacción procesada.</param>
/// <param name="ClearingHouseId">Identificador de la cámara asociada al ciclo.</param>
/// <param name="Processed">Cantidad de items procesados.</param>
/// <param name="Succeeded">Cantidad de items con resultado exitoso.</param>
/// <param name="Failed">Cantidad de items con resultado fallido.</param>
/// <param name="Partial">Cantidad de items con resultado parcial.</param>
/// <param name="Chunks">Cantidad de chunks procesados por el servicio.</param>
/// <param name="Summary">Resumen textual generado por el servicio para la ejecución.</param>
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
