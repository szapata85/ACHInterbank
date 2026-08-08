using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase;

[Scoped]
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AchDbContext _dbContext;

    public UnitOfWork(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CommitIdempotentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
        {
            _dbContext.ChangeTracker.Clear();
            throw new IdempotentWriteConflictException(ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _dbContext.ChangeTracker.Clear();
            throw new ConcurrentStateWriteConflictException(ex);
        }
    }
}
