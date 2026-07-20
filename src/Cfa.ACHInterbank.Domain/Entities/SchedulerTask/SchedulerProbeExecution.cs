namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

public sealed class SchedulerProbeExecution
{
    public long Id { get; set; }
    public string ProbeKey { get; set; } = string.Empty;
    public Guid ExecutionId { get; set; }
    public string SchedulerInstanceId { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? EffectAppliedAtUtc { get; set; }
    public string Status { get; set; } = "Running";
}
