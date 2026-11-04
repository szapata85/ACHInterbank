using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;

[DisallowConcurrentExecution]
public class NonConcurrentDynamicJob : IJob
{
    private readonly DynamicJobExecutor _executor;

    public NonConcurrentDynamicJob(DynamicJobExecutor executor)
    {
        _executor = executor;
    }

    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        => new(_executor.ExecuteAsync(context, cancellationToken));
}
