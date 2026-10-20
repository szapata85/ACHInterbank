using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchOutboundReturnDispatchService
{
    Task<AchOutboundReturnDispatchResult> GenerateAndDispatchAsync(
        AchOutboundReturnGenerateDispatchRequest request,
        CancellationToken ct = default);

    Task<AchOutboundReturnDispatchResult> DispatchAsync(
        AchOutboundReturnDispatchRequest request,
        CancellationToken ct = default);
}
