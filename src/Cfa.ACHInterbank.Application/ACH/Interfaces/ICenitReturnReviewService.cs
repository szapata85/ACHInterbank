using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitReturnReviewService
{
    Task<IReadOnlyList<CenitReturnReviewItem>> ListAsync(int maxItems = 100,
        CancellationToken ct = default);
    Task<CenitReturnReviewItem> DecideAsync(Guid preparationId, CenitReturnReviewDecision decision,
        string authenticatedActorId, CancellationToken ct = default);
}
