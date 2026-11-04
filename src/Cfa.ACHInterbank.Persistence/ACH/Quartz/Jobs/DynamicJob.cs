using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;

public class DynamicJob : IJob
{
    private readonly DynamicJobExecutor _executor;

    public DynamicJob(DynamicJobExecutor executor)
    {
        _executor = executor;
    }

    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        => new(_executor.ExecuteAsync(context, cancellationToken));
}
