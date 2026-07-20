using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

namespace Cfa.ACHInterbank.Application.Scheduler.Models;

public sealed record SchedulerOverviewDto(
    int TotalInstances,
    int ActiveInstances,
    int OfflineInstances,
    int RunningJobs,
    int UpcomingExecutions,
    int RecentFailures,
    int RecentMisfires,
    string SchedulerName,
    bool PersistentStore,
    bool Clustered);

public sealed record SchedulerTaskDto(
    string TaskCode,
    string Name,
    string Description,
    string Status,
    string? ClearingHouse,
    string ScheduleDescription,
    string? CronExpression,
    string TimeZoneId,
    SchedulerMisfirePolicy MisfirePolicy,
    string MisfireDescription,
    DateTimeOffset? LastExecutionUtc,
    DateTimeOffset? NextExecutionUtc,
    string? LastResult,
    long? LastDurationMilliseconds,
    string? LastSchedulerInstance,
    string CurrentState,
    bool ManualExecutionEnabled,
    bool RequestsRecovery,
    bool AllowsConcurrentExecution,
    int PeriodicityType,
    int? N,
    int? Minute,
    string? TimeOfDay,
    DayOfWeek? WeeklyDay,
    int? MonthDay,
    bool OnlyBusinessDays,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt);

public sealed record SchedulerExecutionDto(
    Guid ExecutionId,
    string TaskCode,
    string JobName,
    string JobGroup,
    string TriggerName,
    string TriggerType,
    string FireInstanceId,
    string SchedulerInstanceId,
    string SchedulerInstanceName,
    string? RequestedByUserId,
    string? RequestedByUserName,
    string? RequestReason,
    string? RequestId,
    string CorrelationId,
    DateTimeOffset ScheduledFireTimeUtc,
    DateTimeOffset? ActualFireTimeUtc,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    long? DurationMilliseconds,
    SchedulerExecutionStatus Status,
    bool IsRecovery,
    int RefireCount,
    bool MisfireDetected,
    string? ResultSummary,
    string? ErrorCode,
    string? ErrorSummary,
    string? OriginalFireInstanceId,
    string? RecoveredByInstanceId,
    DateTimeOffset? RecoveryStartedAtUtc,
    string? RecoveryResult);

public sealed record SchedulerInstanceDto(
    string InstanceId,
    string InstanceName,
    string HostName,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastHeartbeatUtc,
    string Status,
    bool IsCurrentInstance,
    int CurrentlyExecutingJobs,
    string Version);

public sealed record SchedulerPagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed class SchedulerHistoryQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public SchedulerExecutionStatus? Status { get; init; }
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
    public string? TriggerType { get; init; }
    public string? InstanceId { get; init; }
    public string? UserName { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed record ExecuteSchedulerTaskRequest(string Reason, Guid RequestId);

public sealed record ExecuteSchedulerTaskCommand(
    string TaskCode,
    string Reason,
    Guid RequestId,
    string? UserId,
    string UserName,
    string CorrelationId);

public enum ManualExecutionOutcome
{
    Accepted,
    Duplicate,
    Conflict,
    Rejected,
    NotFound
}

public sealed record ManualExecutionResult(
    ManualExecutionOutcome Outcome,
    Guid? ExecutionId,
    string Message,
    Guid? ActiveExecutionId = null);

public sealed record SchedulerScheduleUpdateRequest(
    int PeriodicityType,
    int? N,
    int? Minute,
    string? TimeOfDay,
    DayOfWeek? WeeklyDay,
    int? MonthDay,
    string? CronExpression,
    string TimeZoneId,
    SchedulerMisfirePolicy MisfirePolicy,
    bool OnlyBusinessDays,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt);

public sealed record SchedulerScheduleUpdateCommand(
    string TaskCode,
    SchedulerScheduleUpdateRequest Schedule,
    string? UserId,
    string UserName);

public sealed record SchedulerSchedulePreviewDto(string Description, IReadOnlyList<DateTimeOffset> NextExecutionsUtc);
