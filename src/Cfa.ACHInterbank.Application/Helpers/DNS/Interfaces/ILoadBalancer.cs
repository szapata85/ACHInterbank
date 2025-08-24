using Cfa.ACHInterbank.Domain.Entities.Servers;

namespace Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;

public interface ILoadBalancer
{
    Task<ServerCache> GetNextServer(string Type);
}
