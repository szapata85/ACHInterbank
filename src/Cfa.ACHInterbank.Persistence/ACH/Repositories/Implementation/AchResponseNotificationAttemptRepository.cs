using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class AchResponseNotificationAttemptRepository : IAchResponseNotificationAttemptRepository
{
    private readonly AchDbContext _context;

    public AchResponseNotificationAttemptRepository(AchDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AchResponseNotificationAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _context.AchResponseNotificationAttempts.AddAsync(attempt, cancellationToken);
    }

    public async Task<int> GetNextAttemptNumberAsync(Guid achResponseId, CancellationToken cancellationToken = default)
    {
        var max = await _context.AchResponseNotificationAttempts
            .AsNoTracking()
            .Where(x => x.AchResponseId == achResponseId)
            .Select(x => (int?)x.NumeroIntento)
            .MaxAsync(cancellationToken);

        return (max ?? 0) + 1;
    }

    public async Task<IReadOnlyList<AchResponseNotificationAttempt>> FindByResponseIdAsync(Guid achResponseId, CancellationToken cancellationToken = default)
    {
        return await _context.AchResponseNotificationAttempts
            .AsNoTracking()
            .Where(x => x.AchResponseId == achResponseId)
            .OrderBy(x => x.NumeroIntento)
            .ToListAsync(cancellationToken);
    }

    public Task<AchResponseNotificationAttempt?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.AchResponseNotificationAttempts
            .Include(x => x.AchResponse)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task UpdateAsync(AchResponseNotificationAttempt attempt, CancellationToken cancellationToken = default)
    {
        _context.AchResponseNotificationAttempts.Update(attempt);
        return Task.CompletedTask;
    }

}