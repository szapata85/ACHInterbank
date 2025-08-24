using Cfa.ACHInterbank.Domain.Entities.Servers;

namespace Cfa.ACHInterbank.Application.CacheMemory.Servers;

public interface IServerRepository
{
    void AddToken(ServerCache server);
    void UpdateToken(ServerCache server);
    ServerCache GetToken(string server_url);
    List<ServerCache> ListServer();
}
