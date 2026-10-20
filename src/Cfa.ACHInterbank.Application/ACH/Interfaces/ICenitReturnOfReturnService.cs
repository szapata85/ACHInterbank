using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitReturnOfReturnService
{
    Task<CenitReturnOfReturnResult> CreateOutgoingAsync(CenitReturnOfReturnOutRequest request, CancellationToken ct = default);
    Task<CenitReturnOfReturnResult> IngestIncomingAsync(CenitReturnOfReturnInRequest request, CancellationToken ct = default);
}
