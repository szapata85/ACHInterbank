using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class AchResponseRepository : IAchResponseRepository
{
    private readonly AchDbContext _context;

    public AchResponseRepository(AchDbContext context)
    {
        _context = context;
    }

    public Task<AchResponse?> FindByIdempotencyHashAsync(string hashIdempotencia, CancellationToken cancellationToken = default)
    {
        return _context.AchResponses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.HashIdempotencia == hashIdempotencia, cancellationToken);
    }

    public async Task AddAsync(AchResponse response, CancellationToken cancellationToken = default)
    {
        await _context.AchResponses.AddAsync(response, cancellationToken);
    }

    public Task UpdateAsync(AchResponse response, CancellationToken cancellationToken = default)
    {
        _context.AchResponses.Update(response);
        return Task.CompletedTask;
    }
}
