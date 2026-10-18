using AutoMapper;
using Cfa.ACHInterbank.Application.CacheMemory.Servers;
using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Application.Helpers.DNS.Implemetations;

[Singleton]
public class CheckHealthAsync : ICheckHealthAsync
{
    private readonly ServesSettings _serverSettings;
    private readonly int _maxFailedChecks = 3;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(30);
    private readonly IHealthProbeClient _healthProbeClient;
    private readonly IServerRepository _serverRepository;
    private readonly ILoggerManager _loggerManager;
    private readonly IMapper _mapper = MapperBootstrapper.Instance;

    public CheckHealthAsync(
        IServerRepository serverRepository,
        ILoggerManager loggerManager,
        IOptions<ServesSettings> serverOptions,
        IHealthProbeClient healthProbeClient)
    {
        _serverRepository = serverRepository;
        _loggerManager = loggerManager;
        _serverSettings = serverOptions?.Value ?? throw new ArgumentNullException(nameof(serverOptions));
        _healthProbeClient = healthProbeClient;
    }


    public async Task CheckHealthAsy(string Type)
    {
        var model = Type switch
        {
            "services" => _serverSettings.services,
            "servicesWCF" => _serverSettings.servicesWCF,
            _ => null
        };

        foreach (var healthCheck in model!)
        {
            if (DateTime.UtcNow - healthCheck.LastHealthCheck < _healthCheckInterval)
                continue;

            var server = _mapper.Map<ServerCache>(healthCheck);
            _serverRepository!.AddToken(server);

            try
            {
                // Intentar realizar una solicitud al servidor
                healthCheck.IsHealthy = await _healthProbeClient
                    .IsHealthyAsync(healthCheck.Url ?? throw new InvalidOperationException("Health endpoint is required."))
                    .ConfigureAwait(false);
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
                _loggerManager.LogError(
                    $"Health probe failed; service={healthCheck.Name ?? "unknown"}; exception={ex.GetType().Name}.");
            }
            finally
            {
                healthCheck.LastHealthCheck = DateTime.UtcNow;
            }
        }

    }
}
