using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapOperationExecutor
{
    Task<NachaSoapExecutionResult> ExecuteAsync(NachaSoapExecutionRequest request, CancellationToken ct = default);
}
