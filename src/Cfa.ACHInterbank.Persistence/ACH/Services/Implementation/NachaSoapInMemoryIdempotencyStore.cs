using System.Collections.Concurrent;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapInMemoryIdempotencyStore : INachaSoapIdempotencyStore
{
    private readonly ConcurrentDictionary<string, NachaSoapIdempotencyRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    public Task<NachaSoapIdempotencyBeginResult> TryBeginAsync(
        NachaSoapIdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        record.Status = NachaSoapIdempotencyStatus.InProgress;
        var stored = _records.GetOrAdd(record.IdempotencyKey, record);
        var canExecute = ReferenceEquals(stored, record);

        return Task.FromResult(new NachaSoapIdempotencyBeginResult
        {
            CanExecute = canExecute,
            Record = stored
        });
    }

    public Task CompleteAsync(
        string idempotencyKey,
        NachaSoapResilienceExecutionResult result,
        CancellationToken cancellationToken = default)
    {
        Update(idempotencyKey, result, NachaSoapIdempotencyStatus.Completed);
        return Task.CompletedTask;
    }

    public Task FailAsync(
        string idempotencyKey,
        NachaSoapResilienceExecutionResult result,
        CancellationToken cancellationToken = default)
    {
        Update(idempotencyKey, result, result.FinalStatus);
        return Task.CompletedTask;
    }

    public Task<NachaSoapIdempotencyRecord?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(idempotencyKey, out var record);
        return Task.FromResult(record);
    }

    private void Update(
        string idempotencyKey,
        NachaSoapResilienceExecutionResult result,
        NachaSoapIdempotencyStatus fallbackStatus)
    {
        if (!_records.TryGetValue(idempotencyKey, out var record))
        {
            return;
        }

        record.Status = result.FinalStatus == NachaSoapIdempotencyStatus.New ? fallbackStatus : result.FinalStatus;
        record.LastAttemptAt = DateTime.UtcNow;
        record.AttemptCount = result.AttemptCount;
        record.FinalResult = result.FinalMessage;
    }
}
