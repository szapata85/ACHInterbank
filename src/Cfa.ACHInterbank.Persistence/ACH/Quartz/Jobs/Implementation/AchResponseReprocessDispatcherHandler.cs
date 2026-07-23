using Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[DisallowConcurrentExecution]
[Scoped]
public sealed class AchResponseReprocessDispatcherHandler : ITaskHandler
{
    public const string TaskCode = "ach-response-reprocess-dispatcher";
    private readonly IAchResponseReprocessDispatcher _dispatcher;
    private readonly IConfiguration _configuration;

    public AchResponseReprocessDispatcherHandler(IAchResponseReprocessDispatcher dispatcher, IConfiguration configuration)
    {
        _dispatcher = dispatcher;
        _configuration = configuration;
    }

    public string Code => TaskCode;

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var batch = ReadPositive(task, "BatchSize", 50);
        var leaseSeconds = ReadPositive(task, "LeaseSeconds", 120);
        var instance = _configuration["Quartz:InstanceId"]
            ?? Environment.MachineName;
        return (await _dispatcher.DispatchAsync(batch, TimeSpan.FromSeconds(leaseSeconds), instance, cancellationToken)).Summary;
    }

    private static int ReadPositive(TaskDefinition task, string key, int fallback)
        => int.TryParse(task.Parameters.FirstOrDefault(x => x.Key == key)?.Value, out var value) && value > 0 ? value : fallback;
}
