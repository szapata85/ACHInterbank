namespace Cfa.ACHInterbank.Application.Security.Dtos;

public record LoginLockoutSettingsDto
{
    public int MaxFailedAttempts { get; init; }
    public int LockoutMinutes { get; init; }
}
