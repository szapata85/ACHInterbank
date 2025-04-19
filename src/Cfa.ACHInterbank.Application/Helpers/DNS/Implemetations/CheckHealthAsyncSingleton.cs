using AutoMapper;
using Cfa.ACHInterbank.Application.CacheMemory.Servers;
using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cfa.ACHInterbank.Application.Helpers.DNS.Implemetations;

public class CheckHealthAsyncSingleton : ICheckHealthAsyncSingleton
{
    private readonly AppSettings _appSettings = AppSettings.Settings;
    private readonly int _maxFailedChecks = 3;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(30);
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly IServerRepositorySingleton _serverRepository;
    private readonly ILoggerManagerTransient _loggerManager;
    private readonly IMapper _mapper = MapperBootstrapper.Instance;

    public CheckHealthAsyncSingleton(IServerRepositorySingleton serverRepository, ILoggerManagerTransient loggerManager)
    {
        _serverRepository = serverRepository;
        _loggerManager = loggerManager;
    }
    public async Task CheckHealthAsync(string Type)
    {
        var jsonresult = JsonConvert.SerializeObject(_appSettings.Servers);

        JObject? _servicesIntegration = JObject.Parse(jsonresult);
        var model = JsonConvert.DeserializeObject<List<ServicesIntegration>>(_servicesIntegration[Type]!.ToString());

        foreach (var healthCheck in model!)
        {
            if (DateTime.UtcNow - healthCheck.LastHealthCheck < _healthCheckInterval)
                continue;

            var server = _mapper.Map<ServerCache>(healthCheck);
            _serverRepository!.AddToken(server);

            try
            {
                // Intentar realizar una solicitud al servidor
                var response = await _httpClient.GetAsync(healthCheck.Url);
                healthCheck.IsHealthy = response.IsSuccessStatusCode;
                healthCheck.FailedChecks = 0; // Restablecer fallos si la solicitud tiene éxito
                break;
            }
            catch (Exception ex)
            {
                healthCheck.IsHealthy = false;
                healthCheck.FailedChecks++;
                if (healthCheck.FailedChecks >= _maxFailedChecks)
                {
                    healthCheck.IsHealthy = false;
                }
                var serverUpdate = _mapper.Map<ServerCache>(healthCheck);
                if (_serverRepository!.GetToken(healthCheck.Url!) != null)
                    _serverRepository.UpdateToken(serverUpdate);
                _loggerManager.LogError($"Error en la evalaución del servicio: {JsonConvert.SerializeObject(healthCheck)} Mensaje Respuesta: {ex.Message}");
            }
            finally
            {
                healthCheck.LastHealthCheck = DateTime.UtcNow;
            }
        }

    }
}
