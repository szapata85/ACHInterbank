using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapAttemptAuditor
{
    Task RecordAttemptAsync(
        NachaSoapAttemptAudit attempt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaSoapAttemptAudit>> GetAttemptsAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
