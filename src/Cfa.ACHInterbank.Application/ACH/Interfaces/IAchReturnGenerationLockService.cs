namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchReturnGenerationLockService
{
    Task<IAsyncDisposable> AcquireAsync(IReadOnlyCollection<int> transactionIds, CancellationToken cancellationToken);
}
