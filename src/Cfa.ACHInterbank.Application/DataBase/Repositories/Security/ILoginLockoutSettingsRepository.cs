using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Application.DataBase.Repositories.Security;

public interface ILoginLockoutSettingsRepository
{
    Task<LoginLockoutSetting?> GetSettingsAsync(CancellationToken cancellationToken = default);
}
