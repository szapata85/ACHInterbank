namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

public enum SchedulerExecutionStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Recovered = 4,
    Skipped = 5,
    Rejected = 6,
    Misfired = 7
}
