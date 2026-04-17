using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaIngestionAppService
{
    Task<IncomingNachaIngestionResponse> IngestAsync(IncomingNachaIngestionRequest request, CancellationToken ct = default);
}
