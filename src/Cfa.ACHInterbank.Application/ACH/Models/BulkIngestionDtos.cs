namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum BulkIngestionSourceType
{
    InlineTransactions = 1,
    JsonFile = 2,
    CsvFile = 3,
    ExcelFile = 4
}

public enum BulkIngestionProcessingMode
{
    Synchronous = 1,
    AsynchronousJob = 2
}

public sealed class BulkIngestionRequest
{
    public string BatchReference { get; set; } = string.Empty;
    public BulkIngestionSourceType SourceType { get; set; } = BulkIngestionSourceType.InlineTransactions;
    public BulkIngestionProcessingMode ProcessingMode { get; set; } = BulkIngestionProcessingMode.Synchronous;
    public int? ChunkSize { get; set; }

    // Escenario actual
    public List<BulkAchTransactionItemRequest>? Transactions { get; set; }

    // Escenarios futuros (file-based)
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? ContentBase64 { get; set; }

    // Escenario futuro (tracking/reintentos)
    public string? ClientRequestId { get; set; }
    public int? RetryCount { get; set; }
}

public sealed class BulkIngestionResponse
{
    public BulkIngestionProcessingMode ProcessingMode { get; set; } = BulkIngestionProcessingMode.Synchronous;
    public string? JobId { get; set; }
    public string? Status { get; set; }
    public BulkAchTransactionResponse? ImmediateResult { get; set; }
}
