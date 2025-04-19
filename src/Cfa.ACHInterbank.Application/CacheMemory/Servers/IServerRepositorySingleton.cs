using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Entities.Token;

namespace Cfa.ACHInterbank.Application.CacheMemory.Servers;

public interface IServerRepositorySingleton
{
    void AddToken(ServerCache server);
    void UpdateToken(ServerCache server);
    ServerCache GetToken(string server_url);
    List<ServerCache> ListServer();
}
