namespace Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;

/// <summary>Re-evaluates an existing response. It never creates a new receipt.</summary>
public interface IAchResponseReprocessPipeline
{
    Task<AchResponseReprocessExecutionResult> ExecuteAsync(Guid responseId, long attemptId,
        CancellationToken cancellationToken = default);
}
