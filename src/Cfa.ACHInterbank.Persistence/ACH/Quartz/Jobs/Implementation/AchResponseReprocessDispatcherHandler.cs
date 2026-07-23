using Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[DisallowConcurrentExecution]
[Scoped]
public sealed class AchResponseReprocessDispatcherHandler : ISchedulerContextAwareTaskHandler
{
    public const string TaskCode = "ach-response-reprocess-dispatcher";
    private readonly IAchResponseReprocessDispatcher _dispatcher;
    public AchResponseReprocessDispatcherHandler(IAchResponseReprocessDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public string Code => TaskCode;

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
        => await ExecuteAsync(task, new SchedulerTaskExecutionContext(Guid.Empty, "scheduler:unknown", "manual", Environment.MachineName, false, 0), cancellationToken);

    public async Task<string> ExecuteAsync(TaskDefinition task, SchedulerTaskExecutionContext context, CancellationToken cancellationToken)
    {
        var batch = ReadPositive(task, "BatchSize", 50);
        var leaseSeconds = ReadPositive(task, "LeaseSeconds", 120);
        var instance = string.IsNullOrWhiteSpace(context.SchedulerInstanceId) ? Environment.MachineName : context.SchedulerInstanceId;
        return (await _dispatcher.DispatchAsync(batch, TimeSpan.FromSeconds(leaseSeconds), instance, cancellationToken)).Summary;
    }

    private static int ReadPositive(TaskDefinition task, string key, int fallback)
        => int.TryParse(task.Parameters.FirstOrDefault(x => x.Key == key)?.Value, out var value) && value > 0 ? value : fallback;
}
