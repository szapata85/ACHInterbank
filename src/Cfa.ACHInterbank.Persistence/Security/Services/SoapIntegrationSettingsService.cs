using System.Text.Json;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class SoapIntegrationSettingsService : ISoapIntegrationSettingsService
{
    private readonly AchDbContext _dbContext;
    private readonly AppSettings _appSettings = AppSettings.Settings;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public SoapIntegrationSettingsService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SoapIntegrationSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await _dbContext.Set<SoapIntegrationSetting>()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            var defaults = Normalize(BuildDefaultSettings());
            settings = new SoapIntegrationSetting
            {
                WscfaachMappingsJson = JsonSerializer.Serialize(defaults.WscfaachMappings, JsonOptions),
                WsAxonRespuestaTransaccionesMappingsJson = JsonSerializer.Serialize(defaults.WsAxonRespuestaTransaccionesMappings, JsonOptions)
            };

            _dbContext.Set<SoapIntegrationSetting>().Add(settings);
            await _dbContext.SaveChangesAsync(ct);

            return defaults;
        }

        return MapToDto(settings);
    }

    public async Task<SoapIntegrationSettingsDto> SaveAsync(SoapIntegrationSettingsDto request, CancellationToken ct = default)
    {
        var settings = await _dbContext.Set<SoapIntegrationSetting>()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new SoapIntegrationSetting();
            _dbContext.Set<SoapIntegrationSetting>().Add(settings);
        }

        var normalized = Normalize(request);

        settings.WscfaachMappingsJson = JsonSerializer.Serialize(normalized.WscfaachMappings, JsonOptions);
        settings.WsAxonRespuestaTransaccionesMappingsJson = JsonSerializer.Serialize(normalized.WsAxonRespuestaTransaccionesMappings, JsonOptions);

        await _dbContext.SaveChangesAsync(ct);

        return normalized;
    }

    private SoapIntegrationSettingsDto BuildDefaultSettings()
    {
        var urlAch = _appSettings.Integrations?.UrlAch ?? string.Empty;

        return new SoapIntegrationSettingsDto
        {
            WscfaachMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "PLValidarUsuarioBV",
                    Endpoint = urlAch,
                    SoapAction = "http://tempuri.org/IWSCFAACH/PLValidarUsuarioBV",
                    Enabled = true
                },
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Contrapartidas",
                    Endpoint = urlAch,
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
                    Enabled = true
                },
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Transacciones",
                    Endpoint = urlAch,
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
                    Enabled = true
                }
            ],
            WsAxonRespuestaTransaccionesMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "RegistrarRespuestaTransaccion",
                    Endpoint = urlAch,
                    SoapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
                    Enabled = true
                }
            ]
        };
    }

    private SoapIntegrationSettingsDto MapToDto(SoapIntegrationSetting settings)
    {
        var defaults = BuildDefaultSettings();
        var wscfaach = Deserialize(settings.WscfaachMappingsJson, defaults.WscfaachMappings);
        var wsAxon = Deserialize(settings.WsAxonRespuestaTransaccionesMappingsJson, defaults.WsAxonRespuestaTransaccionesMappings);

        return Normalize(new SoapIntegrationSettingsDto
        {
            WscfaachMappings = wscfaach,
            WsAxonRespuestaTransaccionesMappings = wsAxon
        });
    }

    private static List<SoapEndpointMethodMappingDto> Deserialize(string json, List<SoapEndpointMethodMappingDto> fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

        var values = JsonSerializer.Deserialize<List<SoapEndpointMethodMappingDto>>(json, JsonOptions);
        return values is null || values.Count == 0 ? fallback : values;
    }

    private static SoapIntegrationSettingsDto Normalize(SoapIntegrationSettingsDto request)
    {
        return new SoapIntegrationSettingsDto
        {
            WscfaachMappings = NormalizeMappings(request.WscfaachMappings),
            WsAxonRespuestaTransaccionesMappings = NormalizeMappings(request.WsAxonRespuestaTransaccionesMappings)
        };
    }

    private static List<SoapEndpointMethodMappingDto> NormalizeMappings(IEnumerable<SoapEndpointMethodMappingDto> mappings)
    {
        return mappings
            .Where(m => !string.IsNullOrWhiteSpace(m.MethodName))
            .Select(m => new SoapEndpointMethodMappingDto
            {
                MethodName = m.MethodName.Trim(),
                Endpoint = m.Endpoint?.Trim() ?? string.Empty,
                SoapAction = m.SoapAction?.Trim() ?? string.Empty,
                Enabled = m.Enabled
            })
            .ToList();
    }
}
