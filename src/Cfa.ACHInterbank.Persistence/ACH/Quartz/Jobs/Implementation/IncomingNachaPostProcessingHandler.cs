using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public class IncomingNachaPostProcessingHandler : ITaskHandler
{
    private readonly IIncomingNachaPostProcessingOrchestrator _orchestrator;

    public string Code => "IncomingNachaPostProcessing";

    public IncomingNachaPostProcessingHandler(IIncomingNachaPostProcessingOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var chunkSize = ParsePositiveInt(task, "ChunkSize", 100);
        var result = await _orchestrator.ExecuteAsync(chunkSize, $"task:{Code}", cancellationToken);
        return result.Summary;
    }

    private static int ParsePositiveInt(TaskDefinition task, string key, int defaultValue)
    {
        var raw = task.Parameters.FirstOrDefault(p => p.Key == key)?.Value;
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }
}
