using Cfa.ACHInterbank.Application.ACH.Interfaces;

namespace Cfa.ACHInterbank.Tests;

internal sealed class TestReturnGenerationLockService : IAchReturnGenerationLockService
{
    public Task<IAsyncDisposable> AcquireAsync(IReadOnlyCollection<int> transactionIds, CancellationToken cancellationToken)
        => Task.FromResult<IAsyncDisposable>(new NoOpForMock());

    public sealed class NoOpForMock : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
