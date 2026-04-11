namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Opciones de diseño evolutivo para escalar la ingestión masiva.
/// </summary>
public sealed class BulkIngestionEvolutionOptions
{
    public bool UseDistributedQueueDispatcher { get; set; }
    public bool UseExternalFileStorage { get; set; }
    public bool EnableChunkedLargeFileProcessing { get; set; } = true;
    public bool EnableProgressNotifications { get; set; } = true;
    public bool EnableBatchCancellation { get; set; }
    public int DefaultBatchItemsPageSize { get; set; } = 100;
    public int MaxBatchItemsPageSize { get; set; } = 500;
    public int ExpirationDays { get; set; } = 30;
    public int ArchiveAfterDays { get; set; } = 90;
}
