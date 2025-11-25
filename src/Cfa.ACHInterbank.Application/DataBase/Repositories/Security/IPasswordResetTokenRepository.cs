using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Application.DataBase.Repositories.Security;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> GetValidTokenAsync(string token, CancellationToken cancellationToken = default);
    Task MarkAsUsedAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
}
