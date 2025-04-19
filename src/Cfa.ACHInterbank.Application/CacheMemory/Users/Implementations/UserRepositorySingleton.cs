using Cfa.ACHInterbank.Application.CacheMemory.Users.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Application.CacheMemory.Implementations;

public class UserRepositorySingleton : IUserRepositorySingleton
{
    private readonly Dictionary<string, User> _users = new();

    public void AddUser(User user)
    {
        _users[user.Id!] = user;
    }

    public User GetUser(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return user!;
    }

    public List<User> ListUsers()
    {
        return _users.Values.ToList();
    }
}
