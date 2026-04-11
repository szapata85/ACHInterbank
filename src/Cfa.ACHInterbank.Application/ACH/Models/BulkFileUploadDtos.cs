using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class BulkFileUploadRequest
{
    public string? BatchReference { get; set; }
    public string? ClientRequestId { get; set; }
    public string? RequestedBy { get; set; }
}

public sealed class BulkFileUploadResponse
{
    public Guid BatchId { get; set; }
    public string? JobId { get; set; }
    public string BatchReference { get; set; } = string.Empty;
    public BulkIngestionBatchStatusEnum Status { get; set; } = BulkIngestionBatchStatusEnum.Uploaded;
    public BulkIngestionFileTypeEnum FileType { get; set; } = BulkIngestionFileTypeEnum.Unknown;
    public int TotalRecordsDetected { get; set; }
    public int TotalStructuralErrors { get; set; }
    public int TotalReadyForProcessing { get; set; }
    public IReadOnlyList<string> ErrorSummary { get; set; } = [];
}

public sealed class ParsedRawItem
{
    public int Index { get; set; }
    public IReadOnlyDictionary<string, string?> Fields { get; set; } = new Dictionary<string, string?>();
}

public sealed class ParsedFileResult
{
    public BulkIngestionFileTypeEnum FileType { get; set; } = BulkIngestionFileTypeEnum.Unknown;
    public List<ParsedRawItem> Items { get; set; } = [];
}

public sealed class StructuralValidationOutcome
{
    public int Index { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public BulkAchTransactionItemRequest? NormalizedItem { get; set; }
    public IReadOnlyDictionary<string, string?> Fields { get; set; } = new Dictionary<string, string?>();
}
