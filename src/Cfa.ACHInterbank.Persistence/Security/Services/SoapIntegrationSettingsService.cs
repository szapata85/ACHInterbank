using System.Text.Json;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class SoapIntegrationSettingsService : ISoapIntegrationSettingsService
{
    private const string DefaultWscfaachEndpoint = "http://esparta/WSCFAACH/WSCFAACH.svc";
    private const string DefaultWsAxonEndpoint = "http://esparta/WSCFAACH/WSAxonRespuestaTransacciones.svc";

    private readonly AchDbContext _dbContext;
    private readonly AppSettings? _appSettings = AppSettings.Settings;
    private readonly ProcTransaccionesDispatchOptions _procTransaccionesDispatchOptions;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public SoapIntegrationSettingsService(
        AchDbContext dbContext,
        IOptions<ProcTransaccionesDispatchOptions> procTransaccionesDispatchOptions)
    {
        _dbContext = dbContext;
        _procTransaccionesDispatchOptions = procTransaccionesDispatchOptions.Value;
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

            return WithEffectiveProcTransaccionesSettings(defaults);
        }

        var hydrated = MapToDto(settings);
        var currentWscfaachJson = JsonSerializer.Serialize(hydrated.WscfaachMappings, JsonOptions);
        var currentWsAxonJson = JsonSerializer.Serialize(hydrated.WsAxonRespuestaTransaccionesMappings, JsonOptions);

        if (settings.WscfaachMappingsJson != currentWscfaachJson
            || settings.WsAxonRespuestaTransaccionesMappingsJson != currentWsAxonJson)
        {
            settings.WscfaachMappingsJson = currentWscfaachJson;
            settings.WsAxonRespuestaTransaccionesMappingsJson = currentWsAxonJson;
            await _dbContext.SaveChangesAsync(ct);
        }

        return WithEffectiveProcTransaccionesSettings(hydrated);
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

        return WithEffectiveProcTransaccionesSettings(normalized);
    }

    private SoapIntegrationSettingsDto BuildDefaultSettings()
    {
        var fallback = _appSettings?.Integrations?.UrlAch;
        var defaultWscfaachEndpoint = string.IsNullOrWhiteSpace(fallback) ? DefaultWscfaachEndpoint : fallback;
        var defaultWsAxonEndpoint = string.IsNullOrWhiteSpace(fallback) ? DefaultWsAxonEndpoint : fallback;

        return new SoapIntegrationSettingsDto
        {
            WscfaachMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Contrapartidas",
                    Endpoint = defaultWscfaachEndpoint,
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
                    Enabled = true,
                    InputParameterMappings = BuildProcContrapartidasInputMappings()
                },
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Transacciones",
                    Endpoint = defaultWscfaachEndpoint,
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
                    Enabled = true,
                    InputParameterMappings = BuildProcTransaccionesInputMappings()
                }
            ],
            WsAxonRespuestaTransaccionesMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "RegistrarRespuestaTransaccion",
                    Endpoint = defaultWsAxonEndpoint,
                    SoapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
                    Enabled = true,
                    InputParameterMappings =
                    [
                        new SoapInputParameterMappingDto
                        {
                            InputName = "respuesta",
                            SoapParameterName = "Respuesta",
                            Required = true
                        }
                    ]
                }
            ]
        };
    }

    private static List<SoapInputParameterMappingDto> BuildProcContrapartidasInputMappings()
        =>
        [
            MapInput("OFNIT"), MapInput("OFEMP"), MapInput("OFCTA"), MapInput("OFDD"), MapInput("OFFECHEFEC"),
            MapInput("OFMONDEB"), MapInput("OFMONCRE"), MapInput("OFIDARCH"), MapInput("OFIDLOT"), MapInput("OFST"),
            MapInput("OFIDTX"), MapInput("OFIDREVER"), MapInput("OFIDEBAPLI"), MapInput("OFIDCAMCOMPE"), MapInput("OFDIRECCIONIP"),
            MapInput("OFLIBRE"), MapInput("OFLIBRE1"), MapInput("ANSIDLOTE", required: false), MapInput("ANSST", required: false),
            MapInput("ANCLC", required: false), MapInput("ANSIDTX", required: false), MapInput("ANSIDREVER", required: false)
        ];

    private static List<SoapInputParameterMappingDto> BuildProcTransaccionesInputMappings()
        =>
        [
            MapInput("TREG"), MapInput("TIPTRAN"), MapInput("BCORECEP"), MapInput("BCOORIG"), MapInput("NORIG"),
            MapInput("NCTAORIG", required: false), MapInput("IDORIG"), MapInput("DESTRAN"), MapInput("FECEFEC"), MapInput("NCTARECEP"),
            MapInput("MONTO"), MapInput("NRECEP"), MapInput("IDRECEP"), MapInput("DISCRE", required: false), MapInput("CONV"),
            MapInput("PROD"), MapInput("INFPAG"), MapInput("IDTRAN"), MapInput("IDLOTE"), MapInput("REGLOTE"),
            MapInput("IREVER"), MapInput("LIBRE"), MapInput("IDCAMCOMPE"), MapInput("DIRECCIONIP"), MapInput("LIBRE1"),
            MapInput("ILR", required: false)
        ];

    private static SoapInputParameterMappingDto MapInput(string name, bool required = true)
        => new()
        {
            InputName = name,
            SoapParameterName = name,
            Required = required
        };

    private SoapIntegrationSettingsDto MapToDto(SoapIntegrationSetting settings)
    {
        var defaults = BuildDefaultSettings();
        var wscfaach = Deserialize(settings.WscfaachMappingsJson, defaults.WscfaachMappings);
        var wsAxon = Deserialize(settings.WsAxonRespuestaTransaccionesMappingsJson, defaults.WsAxonRespuestaTransaccionesMappings);

        return Normalize(new SoapIntegrationSettingsDto
        {
            WscfaachMappings = MergeDefaults(wscfaach, defaults.WscfaachMappings),
            WsAxonRespuestaTransaccionesMappings = MergeDefaults(wsAxon, defaults.WsAxonRespuestaTransaccionesMappings)
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

    private static List<SoapEndpointMethodMappingDto> MergeDefaults(
        IEnumerable<SoapEndpointMethodMappingDto> current,
        IEnumerable<SoapEndpointMethodMappingDto> defaults)
    {
        var currentByMethod = current.ToDictionary(x => x.MethodName, StringComparer.OrdinalIgnoreCase);

        return defaults
            .Select(defaultValue =>
            {
                if (!currentByMethod.TryGetValue(defaultValue.MethodName, out var mapping))
                {
                    return defaultValue;
                }

                return mapping with
                {
                    Endpoint = string.IsNullOrWhiteSpace(mapping.Endpoint) ? defaultValue.Endpoint : mapping.Endpoint,
                    SoapAction = string.IsNullOrWhiteSpace(mapping.SoapAction) ? defaultValue.SoapAction : mapping.SoapAction,
                    InputParameterMappings = (mapping.InputParameterMappings is null || mapping.InputParameterMappings.Count == 0)
                        ? defaultValue.InputParameterMappings
                        : mapping.InputParameterMappings
                };
            })
            .ToList();
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
                Enabled = m.Enabled,
                InputParameterMappings = NormalizeParameterMappings(m.InputParameterMappings)
            })
            .ToList();
    }

    private static List<SoapInputParameterMappingDto> NormalizeParameterMappings(
        IEnumerable<SoapInputParameterMappingDto>? mappings)
    {
        if (mappings is null)
        {
            return [];
        }

        return mappings
            .Where(x => !string.IsNullOrWhiteSpace(x.InputName) || !string.IsNullOrWhiteSpace(x.SoapParameterName))
            .Select(x => new SoapInputParameterMappingDto
            {
                InputName = x.InputName?.Trim() ?? string.Empty,
                SoapParameterName = x.SoapParameterName?.Trim() ?? string.Empty,
                DefaultValue = string.IsNullOrWhiteSpace(x.DefaultValue) ? null : x.DefaultValue.Trim(),
                Required = x.Required
            })
            .ToList();
    }

    private SoapIntegrationSettingsDto WithEffectiveProcTransaccionesSettings(SoapIntegrationSettingsDto settings)
    {
        var mapping = settings.WscfaachMappings
            .FirstOrDefault(x => string.Equals(x.MethodName, "Proc_Transacciones", StringComparison.OrdinalIgnoreCase));
        var endpoint = mapping?.Endpoint?.Trim() ?? string.Empty;
        var enabled = mapping?.Enabled == true;
        var mappingReady = enabled
            && !string.IsNullOrWhiteSpace(endpoint)
            && mapping!.InputParameterMappings.Any(x => string.Equals(x.InputName, "IDTRAN", StringComparison.OrdinalIgnoreCase));

        return settings with
        {
            ProcTransaccionesEffectiveSettings = new ProcTransaccionesEffectiveSettingsDto
            {
                Operation = "Proc_Transacciones",
                EffectiveMode = _procTransaccionesDispatchOptions.NormalizedMode,
                Endpoint = endpoint,
                Enabled = enabled,
                MappingReady = mappingReady
            }
        };
    }

}
