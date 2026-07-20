using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Dtos;

public class TaskDefinitionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Enabled;
    public CalendarPolicyEnum CalendarPolicy { get; set; } = CalendarPolicyEnum.OnlyBusinessDays;
    public string? TimeZoneId { get; set; }
    public ConcurrencyPolicyEnum ConcurrencyPolicy { get; set; } = ConcurrencyPolicyEnum.SkipIfRunning;
    public bool RetryOnFailure { get; set; } = true;
    public int? MaxRetries { get; set; }
    public int RetryBackoffSeconds { get; set; } = 60;
    public SchedulerMisfirePolicy MisfirePolicy { get; set; } = SchedulerMisfirePolicy.DoNothing;
    public bool RequestsRecovery { get; set; }
    public bool ManualExecutionEnabled { get; set; }
    public bool Paused { get; set; }

    public PeriodicityTypeEnum PeriodicityType { get; set; }
    public int? N { get; set; }
    public int? Minute { get; set; }
    public string? TimeOfDay { get; set; }
    public DayOfWeek? WeeklyDay { get; set; }
    public int? MonthDay { get; set; }
    public string? CronExpression { get; set; }

    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }

    public List<TaskParameterDto> Parameters { get; set; } = [];
}
