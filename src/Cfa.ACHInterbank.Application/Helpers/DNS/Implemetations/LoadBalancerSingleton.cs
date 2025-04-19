using Cfa.ACHInterbank.Application.CacheMemory.Servers;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.DNS.Implemetations;

public class LoadBalancerSingleton : ILoadBalancerSingleton
{
    private readonly AppSettings _appSettings = AppSettings.Settings;
    private int _currentIndex = -1;
    private readonly ICheckHealthAsyncSingleton _checkHealth;
    private readonly IServerRepositorySingleton _serverRepository;
    private readonly ILoggerManagerTransient _loggerManager;
    private readonly object _lock = new();

    public LoadBalancerSingleton(ICheckHealthAsyncSingleton checkHealth, IServerRepositorySingleton serverRepository, ILoggerManagerTransient loggerManager)
    {
        _checkHealth = checkHealth;
        _serverRepository = serverRepository;
        _loggerManager = loggerManager;
    }

    public async Task<ServerCache> GetNextServer(string type)
    {
        await _checkHealth.CheckHealthAsync(type);
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
