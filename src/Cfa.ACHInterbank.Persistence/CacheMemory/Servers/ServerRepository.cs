using Cfa.ACHInterbank.Application.CacheMemory.Servers;
using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.CacheMemory.Servers;

[Singleton]
public class ServerRepository : IServerRepository
{
    private readonly Dictionary<string, ServerCache> _servers = new();
    public void AddToken(ServerCache server)
    {
        if (!_servers.ContainsKey(server.Url!))
            _servers[server.Url!] = server;
    }

    public ServerCache GetToken(string server_url)
    {
        _servers.TryGetValue(server_url, out var server);
        return server!;
    }

    public void UpdateToken(ServerCache server)
    {
        _servers[server.Url!] = server;
    }

    public List<ServerCache> ListServer()
    {
        return _servers.Values.ToList();
    }


}
