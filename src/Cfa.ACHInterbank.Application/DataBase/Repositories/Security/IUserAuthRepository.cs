using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Application.DataBase.Repositories.Security;

public interface IUserAuthRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task UpdatePasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken = default);
}
