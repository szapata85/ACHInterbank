namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Contrato de evolución para operaciones de ciclo de vida de lotes.
/// </summary>
public interface IBulkIngestionLifecycleService
{
    Task<bool> RequestCancellationAsync(Guid batchId, string requestedBy, CancellationToken ct = default);
    Task<int> ArchiveExpiredBatchesAsync(DateTime utcNow, CancellationToken ct = default);
}
