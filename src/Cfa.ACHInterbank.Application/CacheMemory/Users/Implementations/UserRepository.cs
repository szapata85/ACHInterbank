using Cfa.ACHInterbank.Application.CacheMemory.Users.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.CacheMemory.Implementations;

[Singleton]
public class UserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();

    public void AddUser(User user)
    {
        _users[user.Id] = user;
    }

    public User GetUser(Guid userId)
    {
        _users.TryGetValue(userId, out var user);
        return user!;
    }

    public List<User> ListUsers()
    {
        return _users.Values.ToList();
    }
}
