using Cfa.ACHInterbank.Application.DataBase.Repositories.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase.Repositories.Security;

[Scoped]
public class LoginLockoutSettingsRepository(AchDbContext context) : ILoginLockoutSettingsRepository
{
    private readonly AchDbContext _context = context;

    public async Task<LoginLockoutSetting?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LoginLockoutSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
