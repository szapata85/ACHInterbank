using Cfa.ACHInterbank.Application.CacheMemory.Servers;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.DNS.Implemetations;

[Singleton] 
public class LoadBalancer : ILoadBalancer
{
    private readonly AppSettings _appSettings = AppSettings.Settings;
    private int _currentIndex = -1;
    private readonly ICheckHealthAsync _checkHealth;
    private readonly IServerRepository _serverRepository;
    private readonly ILoggerManager _loggerManager;
    private readonly object _lock = new();

    public LoadBalancer(ICheckHealthAsync checkHealth, IServerRepository serverRepository, ILoggerManager loggerManager)
    {
        _checkHealth = checkHealth;
        _serverRepository = serverRepository;
        _loggerManager = loggerManager;
    }

    public async Task<ServerCache> GetNextServer(string type)
    {
        await _checkHealth.CheckHealthAsy(type);
        //var servers = JsonConvert.DeserializeObject<List<ServicesIntegration>>(_servicesIntegration[type].ToString());
        var localServers = _serverRepository.ListServer();
        localServers = localServers.Where(x => x.IsHealthy).ToList();
        try
        {
            lock (_lock)
            {
                _currentIndex = (_currentIndex + 1) % localServers.Count();
            }

            while (localServers![_currentIndex] == null) ;
        }
        catch
        {

            _loggerManager.LogError($"No hay servicios disponibles, valide la comunicación de los servicios que desea integrar {JsonConvert.SerializeObject(localServers)}");
            throw new Exception("No hay servicios disponibles, valide la comunicación de los servicios que desea integrar");
        }
        return localServers![_currentIndex];
    }
}
