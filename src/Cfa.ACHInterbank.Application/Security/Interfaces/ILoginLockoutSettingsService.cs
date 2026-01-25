using Cfa.ACHInterbank.Application.Security.Dtos;

namespace Cfa.ACHInterbank.Application.Security.Interfaces;

public interface ILoginLockoutSettingsService
{
    Task<LoginLockoutSettingsDto> GetAsync(CancellationToken ct = default);
    Task<LoginLockoutSettingsDto> SaveAsync(LoginLockoutSettingsDto request, CancellationToken ct = default);
}
