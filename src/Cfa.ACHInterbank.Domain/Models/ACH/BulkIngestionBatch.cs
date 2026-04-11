using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BulkIngestionBatch : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BatchReference { get; set; } = string.Empty;
    public BulkIngestionFileTypeEnum FileType { get; set; } = BulkIngestionFileTypeEnum.Unknown;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
    public string? ClientRequestId { get; set; }

    public int TotalRecords { get; set; }
    public int TotalValid { get; set; }
    public int TotalInvalid { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalSucceeded { get; set; }
    public int TotalFailed { get; set; }
    public BulkIngestionBatchStatusEnum Status { get; set; } = BulkIngestionBatchStatusEnum.Uploaded;

    public string SummaryErrorsJson { get; set; } = string.Empty;
    public DateTime? ParsedAtUtc { get; set; }
    public DateTime? ValidatedAtUtc { get; set; }
    public DateTime? QueuedAtUtc { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? ProcessingFinishedAtUtc { get; set; }
    public string? LastJobId { get; set; }
    public string LastJobMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }

    public ICollection<BulkIngestionItem> Items { get; set; } = new List<BulkIngestionItem>();
    public ICollection<BulkIngestionAttempt> Attempts { get; set; } = new List<BulkIngestionAttempt>();
}

public enum BulkIngestionFileTypeEnum
{
    Unknown = 0,
    Json = 1,
    Csv = 2,
    Excel = 3
}

public enum BulkIngestionBatchStatusEnum
{
    Uploaded = 1,
    Parsed = 2,
    Validated = 3,
    Queued = 4,
    Processing = 5,
    PartiallyProcessed = 6,
    Completed = 7,
    Failed = 8,
    Retrying = 9,
    Cancelled = 10
}
