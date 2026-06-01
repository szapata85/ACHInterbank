using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapSimulatedGateway
{
    Task<NachaSoapExecutionResult> ExecuteAsync(
        NachaSoapExecutionRequest request,
        NachaSoapExecutionContext context,
        CancellationToken cancellationToken = default);
}
