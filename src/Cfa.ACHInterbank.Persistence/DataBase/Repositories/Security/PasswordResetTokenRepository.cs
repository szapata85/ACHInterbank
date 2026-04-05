using Cfa.ACHInterbank.Application.DataBase.Repositories.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase.Repositories.Security;

[Scoped]
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AchDbContext _context;

    public PasswordResetTokenRepository(AchDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Add(token);
        return Task.CompletedTask;
    }

    public async Task<PasswordResetToken?> GetValidTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.Expiration >= now, cancellationToken);
    }

    public Task MarkAsUsedAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        token.IsUsed = true;
        _context.PasswordResetTokens.Update(token);
        return Task.CompletedTask;
    }
}
