using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchReturnsService
{
    Task<IReadOnlyList<ReturnEligibleTransactionDto>> GetTransactionsByCycleAsync(string cycleId, CancellationToken ct = default);
    Task<GenerateReturnsFileResponse> GenerateReturnsFileAsync(GenerateReturnsFileRequest request, CancellationToken ct = default);
}
