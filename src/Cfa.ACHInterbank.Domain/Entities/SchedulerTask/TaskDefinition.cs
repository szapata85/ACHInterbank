using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

public sealed class TaskDefinition : AuditableEntity
{
    

    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Enabled;
    public CalendarPolicyEnum CalendarPolicy { get; set; } = CalendarPolicyEnum.OnlyBusinessDays;
    public string? TimeZoneId { get; set; } = "America/Bogota";
    public ConcurrencyPolicyEnum ConcurrencyPolicy { get; set; } = ConcurrencyPolicyEnum.SkipIfRunning;
    public bool RetryOnFailure { get; set; } = true;
    public int? MaxRetries { get; set; }
    public int RetryBackoffSeconds { get; set; } = 60;

    public PeriodicityTypeEnum PeriodicityType { get; set; }
    public int? N { get; set; }
    public int? Minute { get; set; }
    public long? TimeOfDayTicks { get; set; }

    [NotMapped]
    public TimeOnly? TimeOfDay
    {
        get => TimeOfDayTicks.HasValue ? new TimeOnly(TimeOfDayTicks.Value) : null;
        set => TimeOfDayTicks = value?.Ticks;
    }
    public DayOfWeek? WeeklyDay { get; set; }
    public int? MonthDay { get; set; }
    public string? CronExpression { get; set; }

    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }

    public ICollection<TaskParameter> Parameters { get; set; } = new List<TaskParameter>();
    public ICollection<TaskExecutionLog> ExecutionLogs { get; set; } = new List<TaskExecutionLog>();
}