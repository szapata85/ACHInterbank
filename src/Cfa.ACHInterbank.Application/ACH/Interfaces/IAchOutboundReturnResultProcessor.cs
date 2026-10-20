using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchOutboundReturnResultProcessor
{
    Task<AchOutboundReturnResultProcessingResult> ProcessAsync(
        AchOutboundReturnResultRequest request,
        CancellationToken ct = default);
}
