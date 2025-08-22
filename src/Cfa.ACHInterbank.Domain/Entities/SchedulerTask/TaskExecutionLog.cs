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
    public string ExecutionKey { get; set; } = Guid.NewGuid().ToString("N"); // para idempotencia
}
