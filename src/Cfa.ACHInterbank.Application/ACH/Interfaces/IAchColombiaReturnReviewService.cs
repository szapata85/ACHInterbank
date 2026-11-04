using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchColombiaReturnReviewService
{
    Task<IReadOnlyList<AchColombiaReturnReviewItem>> ListAsync(int limit, CancellationToken ct = default);
    Task<AchColombiaReturnReviewResult> DecideAsync(long candidateId, AchColombiaReturnReviewDecision decision,
        CancellationToken ct = default);
    Task<GenerateReturnsFileResponse> GenerateApprovedFileAsync(long candidateId, CancellationToken ct = default);
    // Uso exclusivo del proceso interno, con doble habilitación y sin transporte.
    Task<GenerateReturnsFileResponse> GenerateAutomaticallyAsync(long candidateId, CancellationToken ct = default);
    Task<GenerateReturnsFileResponse> RecoverGeneratedFileAsync(long candidateId, CancellationToken ct = default);
}
