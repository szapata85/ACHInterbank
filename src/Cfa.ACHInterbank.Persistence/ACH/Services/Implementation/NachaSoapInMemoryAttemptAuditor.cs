using System.Collections.Concurrent;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapInMemoryAttemptAuditor : INachaSoapAttemptAuditor
{
    private readonly ConcurrentDictionary<string, List<NachaSoapAttemptAudit>> _attempts = new(StringComparer.OrdinalIgnoreCase);

    public Task RecordAttemptAsync(
        NachaSoapAttemptAudit attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        var attempts = _attempts.GetOrAdd(attempt.IdempotencyKey, _ => []);
        lock (attempts)
        {
            attempts.Add(attempt);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NachaSoapAttemptAudit>> GetAttemptsAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (!_attempts.TryGetValue(idempotencyKey, out var attempts))
        {
            return Task.FromResult<IReadOnlyList<NachaSoapAttemptAudit>>([]);
        }

        lock (attempts)
        {
            return Task.FromResult<IReadOnlyList<NachaSoapAttemptAudit>>(attempts.ToList());
        }
    }
}
