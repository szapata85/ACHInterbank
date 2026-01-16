using Cfa.ACHInterbank.Application.DataBase.Repositories.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase.Repositories.Security;

[Scoped]
public class LoginLockoutSettingsRepository : ILoginLockoutSettingsRepository
{
    private readonly AchDbContext _context;

    public LoginLockoutSettingsRepository(AchDbContext context)
    {
        _context = context;
    }

    public async Task<LoginLockoutSetting?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LoginLockoutSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
