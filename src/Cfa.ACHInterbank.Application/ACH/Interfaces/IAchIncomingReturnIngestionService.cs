using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchIncomingReturnIngestionService
{
    Task<AchIncomingReturnIngestionResult> IngestAsync(AchIncomingReturnIngestionRequest request, CancellationToken cancellationToken);
}
