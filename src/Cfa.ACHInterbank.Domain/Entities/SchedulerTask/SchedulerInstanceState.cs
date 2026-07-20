namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

public sealed class SchedulerInstanceState
{
    public long Id { get; set; }
    public string SchedulerName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset LastHeartbeatUtc { get; set; }
    public DateTimeOffset? StoppedAtUtc { get; set; }
    public string Status { get; set; } = "Iniciando";
    public int CurrentlyExecutingJobs { get; set; }
    public string Version { get; set; } = string.Empty;
}
