using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;

public interface ILoadBalancerSingleton
{
    Task<ServerCache> GetNextServer(string Type);
}
