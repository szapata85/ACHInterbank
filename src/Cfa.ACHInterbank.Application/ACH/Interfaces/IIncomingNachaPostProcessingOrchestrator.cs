using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaPostProcessingOrchestrator
{
    Task<IncomingNachaPostProcessingRunResult> ExecuteAsync(
        int chunkSize,
        string triggeredBy,
        CancellationToken ct = default);
}
