using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapUatOrchestrator
{
    Task<NachaSoapUatReadinessResult> ExecuteReadinessAsync(
        NachaSoapUatReadinessRequest request,
        CancellationToken cancellationToken = default);
}
