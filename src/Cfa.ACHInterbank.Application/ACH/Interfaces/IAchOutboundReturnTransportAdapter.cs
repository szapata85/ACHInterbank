using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchOutboundReturnTransportAdapter
{
    Task<AchOutboundReturnTransportResult> TransmitAsync(
        AchOutboundReturnTransportRequest request,
        CancellationToken ct = default);
}
