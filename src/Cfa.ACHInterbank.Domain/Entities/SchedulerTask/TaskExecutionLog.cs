using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

public sealed class TaskExecutionLog
{
    public long Id { get; set; }
    public int TaskDefinitionId { get; set; }
    public TaskDefinition TaskDefinition { get; set; } = default!;
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Output { get; set; }
    public string ExecutionKey { get; set; } = string.Empty;
    public Guid? ExecutionId { get; set; }
    public string TaskCode { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = string.Empty;
    public string TriggerName { get; set; } = string.Empty;
    public string TriggerType { get; set; } = "Programada";
    public string FireInstanceId { get; set; } = string.Empty;
    public string SchedulerInstanceId { get; set; } = string.Empty;
    public string SchedulerInstanceName { get; set; } = string.Empty;
    public string? RequestedByUserId { get; set; }
    public string? RequestedByUserName { get; set; }
    public string? RequestReason { get; set; }
    public string? RequestId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset? ActualFireTimeUtc { get; set; }
    public long? DurationMilliseconds { get; set; }
    public SchedulerExecutionStatus Status { get; set; } = SchedulerExecutionStatus.Pending;
    public bool IsRecovery { get; set; }
    public int RefireCount { get; set; }
    public bool MisfireDetected { get; set; }
    public string? OriginalFireInstanceId { get; set; }
    public string? RecoveredByInstanceId { get; set; }
    public DateTimeOffset? RecoveryStartedAtUtc { get; set; }
    public string? RecoveryResult { get; set; }
    public string? ErrorCode { get; set; }
    public string? ManualConcurrencyKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
