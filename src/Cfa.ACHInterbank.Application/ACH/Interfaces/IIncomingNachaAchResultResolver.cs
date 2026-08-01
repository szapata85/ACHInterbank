using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaAchResultResolver
{
    Task<IncomingNachaAchResultResolution> ResolveAsync(
        IncomingNachaAchResultRequest request,
        CancellationToken ct = default);
}
