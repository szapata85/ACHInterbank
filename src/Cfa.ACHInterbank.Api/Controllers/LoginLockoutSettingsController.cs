using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/users/login-lockout")]
[Authorize]
public class LoginLockoutSettingsController : ControllerBase
{
    private static readonly LoginLockoutSettingsDto DefaultSettings = new()
    {
        MaxFailedAttempts = 5,
        LockoutMinutes = 5
    };

    private readonly AchDbContext _dbContext;

    public LoginLockoutSettingsController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<LoginLockoutSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.LoginLockoutSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            return Ok(DefaultSettings);
        }

        return Ok(MapToDto(settings));
    }

    [HttpPut]
    public async Task<ActionResult<LoginLockoutSettingsDto>> SaveAsync(
        [FromBody] LoginLockoutSettingsDto request,
        CancellationToken cancellationToken)
    {
        var settings = await _dbContext.LoginLockoutSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new LoginLockoutSetting();
            _dbContext.LoginLockoutSettings.Add(settings);
        }

        var normalized = Normalize(request);
        settings.MaxFailedAttempts = normalized.MaxFailedAttempts;
        settings.LockoutMinutes = normalized.LockoutMinutes;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(settings));
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

public record LoginLockoutSettingsDto
{
    public int MaxFailedAttempts { get; init; }
    public int LockoutMinutes { get; init; }
}
