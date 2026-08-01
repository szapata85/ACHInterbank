using System.Text.Json;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class SoapIntegrationSettingsService : ISoapIntegrationSettingsService
{
    private readonly AchDbContext _dbContext;
    private readonly IIntegrationMappingReadinessService? _mappingReadinessService;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public SoapIntegrationSettingsService(
        AchDbContext dbContext,
        IOptions<ProcTransaccionesDispatchOptions> procTransaccionesDispatchOptions,
        IIntegrationMappingReadinessService? mappingReadinessService = null,
        IOptions<ProcContrapartidasDispatchOptions>? procContrapartidasDispatchOptions = null)
    {
        _dbContext = dbContext;
        _ = procTransaccionesDispatchOptions;
        _ = procContrapartidasDispatchOptions;
        _mappingReadinessService = mappingReadinessService;
    }

    public async Task<SoapIntegrationSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settingsRows = await _dbContext.Set<SoapIntegrationSetting>()
            .OrderBy(x => x.Id)
            .Take(2)
            .ToListAsync(ct);

        EnsureUniqueSettingsRow(settingsRows);
        var settings = settingsRows.SingleOrDefault();

        if (settings is null)
        {
            return await WithEffectiveProcTransaccionesSettingsAsync(new SoapIntegrationSettingsDto(), ct);
        }

        var hydrated = MapToDto(settings);
        return await WithEffectiveProcTransaccionesSettingsAsync(hydrated, ct);
    }

    public async Task<SoapIntegrationSettingsDto> SaveAsync(SoapIntegrationSettingsDto request, CancellationToken ct = default)
    {
        var settingsRows = await _dbContext.Set<SoapIntegrationSetting>()
            .OrderBy(x => x.Id)
            .Take(2)
            .ToListAsync(ct);

        EnsureUniqueSettingsRow(settingsRows);
        var settings = settingsRows.SingleOrDefault();
        var persisted = settings is null ? new SoapIntegrationSettingsDto() : MapToDto(settings);

        if (settings is null)
        {
            settings = new SoapIntegrationSetting();
            _dbContext.Set<SoapIntegrationSetting>().Add(settings);
        }

        var normalized = PreservePersistedTimeouts(Normalize(request), persisted);

        settings.WscfaachMappingsJson = JsonSerializer.Serialize(normalized.WscfaachMappings, JsonOptions);
        settings.WsAxonRespuestaTransaccionesMappingsJson = JsonSerializer.Serialize(normalized.WsAxonRespuestaTransaccionesMappings, JsonOptions);

        await _dbContext.SaveChangesAsync(ct);

        return await WithEffectiveProcTransaccionesSettingsAsync(normalized, ct);
    }

    private SoapIntegrationSettingsDto MapToDto(SoapIntegrationSetting settings)
    {
        var wscfaach = Deserialize(settings.WscfaachMappingsJson);
        var wsAxon = Deserialize(settings.WsAxonRespuestaTransaccionesMappingsJson);

        return Normalize(new SoapIntegrationSettingsDto
        {
            WscfaachMappings = wscfaach,
            WsAxonRespuestaTransaccionesMappings = wsAxon
        });
    }

    private static List<SoapEndpointMethodMappingDto> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var values = JsonSerializer.Deserialize<List<SoapEndpointMethodMappingDto>>(json, JsonOptions);
        return values ?? [];
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
                OperatingMode = NormalizeOperatingMode(m.OperatingMode),
                TimeoutSeconds = m.TimeoutSeconds,
                Enabled = m.Enabled,
                InputParameterMappings = NormalizeParameterMappings(m.InputParameterMappings)
            })
            .ToList();
    }

    private static string NormalizeOperatingMode(string? mode)
    {
        if (string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase))
        {
            return "Live";
        }

        if (string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return "Disabled";
        }

        return "DryRun";
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

    private async Task<SoapIntegrationSettingsDto> WithEffectiveProcTransaccionesSettingsAsync(
        SoapIntegrationSettingsDto settings,
        CancellationToken ct)
    {
        var mapping = settings.WscfaachMappings
            .FirstOrDefault(x => string.Equals(x.MethodName, "Proc_Transacciones", StringComparison.OrdinalIgnoreCase));
        var endpoint = mapping?.Endpoint?.Trim() ?? string.Empty;
        var enabled = mapping?.Enabled == true;
        var effectiveMode = mapping is null
            ? "Disabled"
            : NormalizeOperatingMode(mapping.OperatingMode);
        var runtimeConfigurationReady = enabled
            && !string.IsNullOrWhiteSpace(endpoint)
            && string.Equals(effectiveMode, "Live", StringComparison.OrdinalIgnoreCase)
            && mapping!.TimeoutSeconds is >= 1 and <= 300;
        var readiness = _mappingReadinessService is null
            ? null
            : await _mappingReadinessService.EvaluateAsync(
                IntegrationGuaranteeConstants.Wscfaach,
                IntegrationGuaranteeConstants.ProcTransacciones,
                IntegrationGuaranteeConstants.MonetaryCreditRequest,
                IntegrationGuaranteeConstants.OutboundRequest,
                ct: ct);
        var mappingReady = runtimeConfigurationReady
            && readiness?.IsReady == true
            && readiness.CanBuildPayload;
        var blockingParameters = readiness is null
            ? Array.Empty<string>()
            : readiness.MissingRequiredMappings
                .Concat(readiness.InactiveRequiredMappings)
                .Concat(readiness.Errors.Select(TryReadBlockingParameter).OfType<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return settings with
        {
            ProcTransaccionesEffectiveSettings = new ProcTransaccionesEffectiveSettingsDto
            {
                Operation = "Proc_Transacciones",
                EffectiveMode = effectiveMode,
                Endpoint = endpoint,
                TimeoutSeconds = mapping?.TimeoutSeconds ?? 0,
                Enabled = enabled,
                MappingReady = mappingReady,
                MappingIssueCode = mappingReady
                    ? null
                    : runtimeConfigurationReady
                        ? readiness?.Code ?? "FUNCTIONAL_MAPPING_INVALID"
                        : "SOAP_RUNTIME_CONFIGURATION_INVALID",
                BlockingParameters = blockingParameters
            }
        };
    }

    private static SoapIntegrationSettingsDto PreservePersistedTimeouts(
        SoapIntegrationSettingsDto requested,
        SoapIntegrationSettingsDto persisted)
        => requested with
        {
            WscfaachMappings = PreservePersistedTimeouts(
                requested.WscfaachMappings,
                persisted.WscfaachMappings),
            WsAxonRespuestaTransaccionesMappings = PreservePersistedTimeouts(
                requested.WsAxonRespuestaTransaccionesMappings,
                persisted.WsAxonRespuestaTransaccionesMappings)
        };

    private static List<SoapEndpointMethodMappingDto> PreservePersistedTimeouts(
        IEnumerable<SoapEndpointMethodMappingDto> requested,
        IEnumerable<SoapEndpointMethodMappingDto> persisted)
    {
        var persistedByMethod = persisted
            .Where(x => !string.IsNullOrWhiteSpace(x.MethodName))
            .GroupBy(x => x.MethodName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        return requested
            .Select(mapping => mapping.TimeoutSeconds > 0
                ? mapping
                : persistedByMethod.TryGetValue(mapping.MethodName, out var previous)
                    ? mapping with { TimeoutSeconds = previous.TimeoutSeconds }
                    : mapping)
            .ToList();
    }

    private static void EnsureUniqueSettingsRow(IReadOnlyCollection<SoapIntegrationSetting> settingsRows)
    {
        if (settingsRows.Count > 1)
        {
            throw new InvalidOperationException(
                "SOAP_SETTINGS_NOT_UNIQUE: more than one SoapIntegrationSettings row exists.");
        }
    }

    private static string? TryReadBlockingParameter(string error)
    {
        const string prefix = "Proc_Transacciones.";
        var start = error.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        start += prefix.Length;
        var end = error.IndexOf(':', start);
        return end > start ? error[start..end].Trim() : null;
    }

}
