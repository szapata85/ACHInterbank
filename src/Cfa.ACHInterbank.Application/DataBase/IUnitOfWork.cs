namespace Cfa.ACHInterbank.Application.DataBase;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
