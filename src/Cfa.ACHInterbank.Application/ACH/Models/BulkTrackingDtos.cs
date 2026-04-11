using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class BulkBatchStatusDto
{
    public Guid BatchId { get; set; }
    public string BatchReference { get; set; } = string.Empty;
    public BulkIngestionBatchStatusEnum Status { get; set; }
    public int TotalRecords { get; set; }
    public int TotalValid { get; set; }
    public int TotalInvalid { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalSucceeded { get; set; }
    public int TotalFailed { get; set; }
    public decimal ProgressPercent { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? ProcessingFinishedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastJobId { get; set; }
    public string LastJobMessage { get; set; } = string.Empty;
    public IReadOnlyList<string> ErrorSummary { get; set; } = [];
}

public sealed class BulkBatchItemDto
{
    public long ItemId { get; set; }
    public int ItemIndex { get; set; }
    public string Reference { get; set; } = string.Empty;
    public BulkIngestionItemStatusEnum Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TransactionId { get; set; }
}

public sealed class BulkBatchItemsPageDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public IReadOnlyList<BulkBatchItemDto> Items { get; set; } = [];
}

public sealed class BulkBatchAttemptDto
{
    public long AttemptId { get; set; }
    public int AttemptNumber { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string TriggeredBy { get; set; } = string.Empty;
    public DateTime TriggeredAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalSucceeded { get; set; }
    public int TotalFailed { get; set; }
    public string ResultMessage { get; set; } = string.Empty;
}

public sealed class BulkBatchProcessingSummaryDto
{
    public Guid BatchId { get; set; }
    public BulkBatchStatusDto Status { get; set; } = new();
    public IReadOnlyList<BulkBatchAttemptDto> Attempts { get; set; } = [];
}

public sealed class RetryBatchRequest
{
    public BulkIngestionRetryScopeEnum Scope { get; set; } = BulkIngestionRetryScopeEnum.FailedOnly;
}

public sealed class RetryBatchResponse
{
    public Guid BatchId { get; set; }
    public long AttemptId { get; set; }
    public int AttemptNumber { get; set; }
    public string JobId { get; set; } = string.Empty;
    public BulkIngestionBatchStatusEnum Status { get; set; }
}
