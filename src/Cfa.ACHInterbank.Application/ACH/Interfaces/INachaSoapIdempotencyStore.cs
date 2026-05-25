using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapIdempotencyStore
{
    Task<NachaSoapIdempotencyBeginResult> TryBeginAsync(
        NachaSoapIdempotencyRecord record,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        string idempotencyKey,
        NachaSoapResilienceExecutionResult result,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        string idempotencyKey,
        NachaSoapResilienceExecutionResult result,
        CancellationToken cancellationToken = default);

    Task<NachaSoapIdempotencyRecord?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
