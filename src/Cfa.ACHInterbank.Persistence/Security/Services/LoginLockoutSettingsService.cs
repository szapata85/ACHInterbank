using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class LoginLockoutSettingsService : ILoginLockoutSettingsService
{
    private static readonly LoginLockoutSettingsDto DefaultSettings = new()
    {
        MaxFailedAttempts = 5,
        LockoutMinutes = 5
    };

    private readonly AchDbContext _dbContext;

    public LoginLockoutSettingsService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LoginLockoutSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await _dbContext.LoginLockoutSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        return settings is null ? DefaultSettings : MapToDto(settings);
    }

    public async Task<LoginLockoutSettingsDto> SaveAsync(LoginLockoutSettingsDto request, CancellationToken ct = default)
    {
        var settings = await _dbContext.LoginLockoutSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new LoginLockoutSetting();
            _dbContext.LoginLockoutSettings.Add(settings);
        }

        var normalized = Normalize(request);
        settings.MaxFailedAttempts = normalized.MaxFailedAttempts;
        settings.LockoutMinutes = normalized.LockoutMinutes;

        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(settings);
    }

    private static LoginLockoutSettingsDto Normalize(LoginLockoutSettingsDto request) => new()
    {
        MaxFailedAttempts = Math.Clamp(request.MaxFailedAttempts, 1, 20),
        LockoutMinutes = Math.Clamp(request.LockoutMinutes, 1, 60)
    };

    private static LoginLockoutSettingsDto MapToDto(LoginLockoutSetting settings) => new()
    {
        MaxFailedAttempts = settings.MaxFailedAttempts,
        LockoutMinutes = settings.LockoutMinutes
    };
}
