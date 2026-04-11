using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BulkIngestionAttempt : AuditableEntity
{
    public long Id { get; set; }
    public Guid BatchId { get; set; }
    public BulkIngestionBatch Batch { get; set; } = null!;

    public int AttemptNumber { get; set; }
    public BulkIngestionTriggerTypeEnum TriggerType { get; set; } = BulkIngestionTriggerTypeEnum.Initial;
    public BulkIngestionRetryScopeEnum Scope { get; set; } = BulkIngestionRetryScopeEnum.FailedOnly;
    public string TriggeredBy { get; set; } = string.Empty;
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;

    public BulkIngestionAttemptStatusEnum Status { get; set; } = BulkIngestionAttemptStatusEnum.Queued;
    public string? JobId { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public int TotalProcessed { get; set; }
    public int TotalSucceeded { get; set; }
    public int TotalFailed { get; set; }
    public string ResultMessage { get; set; } = string.Empty;
}

public enum BulkIngestionTriggerTypeEnum
{
    Initial = 1,
    Retry = 2
}

public enum BulkIngestionRetryScopeEnum
{
    Full = 1,
    FailedOnly = 2
}

public enum BulkIngestionAttemptStatusEnum
{
    Queued = 1,
    Processing = 2,
    Completed = 3,
    PartiallyProcessed = 4,
    Failed = 5
}
