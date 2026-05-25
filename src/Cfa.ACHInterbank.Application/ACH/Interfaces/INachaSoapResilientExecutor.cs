using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapResilientExecutor
{
    Task<NachaSoapResilienceExecutionResult> ExecuteAsync(
        NachaSoapExecutionRequest request,
        NachaSoapExecutionContext context,
        NachaSoapRetryPolicy policy,
        CancellationToken cancellationToken = default);
}
