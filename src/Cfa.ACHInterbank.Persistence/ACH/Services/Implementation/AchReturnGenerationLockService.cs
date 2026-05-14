using Cfa.ACHInterbank.Application.ACH.Interfaces;
using System.Collections.Concurrent;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchReturnGenerationLockService : IAchReturnGenerationLockService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Locks = new();

    public async Task<IAsyncDisposable> AcquireAsync(IReadOnlyCollection<int> transactionIds, CancellationToken cancellationToken)
    {
        var ordered = transactionIds.Distinct().OrderBy(x => x).ToArray();
        var acquired = new List<(int Id, SemaphoreSlim Semaphore)>(ordered.Length);

        try
        {
            foreach (var id in ordered)
            {
                var semaphore = Locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
                await semaphore.WaitAsync(cancellationToken);
                acquired.Add((id, semaphore));
            }

            return new Releaser(acquired);
        }
        catch
        {
            foreach (var item in acquired.AsEnumerable().Reverse())
            {
                item.Semaphore.Release();
            }
            throw;
        }
    }

    private sealed class Releaser(List<(int Id, SemaphoreSlim Semaphore)> acquired) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            foreach (var item in acquired.AsEnumerable().Reverse())
            {
                item.Semaphore.Release();
            }
            return ValueTask.CompletedTask;
        }
    }
}
