using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

namespace Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;

public sealed record SchedulerTaskExecutionContext(
    Guid ExecutionId,
    string CorrelationId,
    string FireInstanceId,
    string SchedulerInstanceId,
    bool IsRecovery,
    int RefireCount);

public interface ISchedulerContextAwareTaskHandler : ITaskHandler
{
    Task<string> ExecuteAsync(TaskDefinition task, SchedulerTaskExecutionContext context, CancellationToken cancellationToken);
}
