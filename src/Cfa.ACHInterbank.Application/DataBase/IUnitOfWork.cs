namespace Cfa.ACHInterbank.Application.DataBase;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task<int> CommitIdempotentAsync(CancellationToken cancellationToken = default) => CommitAsync(cancellationToken);
}

public sealed class IdempotentWriteConflictException : Exception
{
    public IdempotentWriteConflictException(Exception innerException)
        : base("Una escritura idempotente concurrente ya fue confirmada.", innerException) { }
}

public sealed class ConcurrentStateWriteConflictException : Exception
{
    public ConcurrentStateWriteConflictException(Exception innerException)
        : base("El estado cambió durante el procesamiento concurrente.", innerException) { }
}
