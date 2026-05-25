using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapOperationAdapter
{
    string AdapterName { get; }
    bool CanHandle(NachaSoapOperationCandidate operationCandidate);

    Task<NachaSoapAdapterExecutionResult> ExecuteAsync(
        NachaSoapExecutionRequest request,
        NachaSoapExecutionContext context,
        CancellationToken cancellationToken = default);
}
