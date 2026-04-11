namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Punto de extensión para despachar procesamiento de lotes a diferentes backplanes
/// (Quartz local, colas distribuidas, workers externos, etc.).
/// </summary>
public interface IBulkIngestionWorkDispatcher
{
    Task<string> DispatchProcessingAsync(Guid batchId, long? attemptId = null, CancellationToken ct = default);
}
