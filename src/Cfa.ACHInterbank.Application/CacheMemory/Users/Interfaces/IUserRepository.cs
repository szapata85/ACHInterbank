using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Application.CacheMemory.Users.Interfaces;

public interface IUserRepository
{
    void AddUser(User user);
    User GetUser(Guid userId);
    List<User> ListUsers();
}
