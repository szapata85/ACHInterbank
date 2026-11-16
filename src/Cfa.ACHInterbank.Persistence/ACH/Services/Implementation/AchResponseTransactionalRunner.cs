using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>NPT-07: trace, state event, prenote state, response and Axon attempt commit atomically.</summary>
public sealed class AchResponseTransactionalRunner : IAchResponseTransactionalRunner
{
    private readonly AchDbContext _db;
    public AchResponseTransactionalRunner(AchDbContext db) => _db = db;

    public async Task<T> RunAsync<T>(Func<Task<T>> databaseWork, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsRelational())
            return await databaseWork(); // in-memory unit tests, not a relational guarantee

        // The providers have transient-retry execution strategies configured. EF requires
        // the whole transaction to run inside CreateExecutionStrategy, never around SOAP.
        return await _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var result = await databaseWork();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
