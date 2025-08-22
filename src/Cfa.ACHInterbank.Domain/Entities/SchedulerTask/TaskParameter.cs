namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

public sealed class TaskParameter
{
    public int Id { get; set; }
    public int TaskDefinitionId { get; set; }
    public TaskDefinition TaskDefinition { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
}
