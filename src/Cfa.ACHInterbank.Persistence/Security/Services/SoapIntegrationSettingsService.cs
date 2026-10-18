using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Exceptions;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class SoapIntegrationSettingsService : ISoapIntegrationSettingsService
{
    private const string EmptyRevision = "0";
    private const string InvalidSanitizedEndpoint = "***invalid-endpoint***";

    private static readonly HashSet<string> SupportedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Proc_Transacciones",
        "Proc_Contrapartidas",
        "RegistrarRespuestaTransaccion"
    };

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
        var settings = await LoadSingleSettingsAsync(ct);
        var raw = settings is null ? new SoapIntegrationSettingsDto { Revision = EmptyRevision } : MapRawDto(settings);
        return await WithEffectiveProcTransaccionesSettingsAsync(raw, ct);
    }

    public async Task<SoapIntegrationSettingsDto> GetAdministrationAsync(CancellationToken ct = default)
    {
        var settings = await LoadSingleSettingsAsync(ct);
        var raw = settings is null ? new SoapIntegrationSettingsDto { Revision = EmptyRevision } : MapRawDto(settings);
        return await BuildClientResponseAsync(raw, ct);
    }

    public async Task<SoapIntegrationSettingsChangePreviewDto> PreviewChangeAsync(
        SoapIntegrationSettingsChangePreviewRequestDto request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = await LoadSingleSettingsAsync(ct);
        var raw = settings is null ? new SoapIntegrationSettingsDto { Revision = EmptyRevision } : MapRawDto(settings);
        EnsureExpectedRevision(request.ExpectedRevision, raw.Revision);

        var persisted = FindMethod(raw, request.MethodName);
        if (persisted is null || !SupportedMethods.Contains(request.MethodName))
        {
            return new SoapIntegrationSettingsChangePreviewDto
            {
                MethodName = request.MethodName?.Trim() ?? string.Empty,
                Revision = raw.Revision,
                CurrentConfigurationState = "Unknown",
                RequestedConfigurationState = BuildConfigurationState(request.OperatingMode, request.Enabled),
                CanApply = false,
                BlockingReason = "No fue posible determinar el servicio o su estado vigente."
            };
        }

        var endpoint = ResolveSensitiveValue(request.Endpoint, persisted.Endpoint, SanitizeEndpoint(persisted.Endpoint));
        var soapAction = ResolveSensitiveValue(request.SoapAction, persisted.SoapAction, SanitizeTechnicalUri(persisted.SoapAction));
        var sanitizedEndpoint = SanitizeEndpoint(endpoint);
        var sanitizedSoapAction = SanitizeTechnicalUri(soapAction);
        var requestedState = BuildConfigurationState(request.OperatingMode, request.Enabled);
        var currentState = BuildConfigurationState(persisted.OperatingMode, persisted.Enabled);
        var targetRealSend = IsRealSend(request.OperatingMode, request.Enabled);
        var currentRealSend = IsRealSend(persisted.OperatingMode, persisted.Enabled);
        var destinationKnown = IsSanitizedDestinationKnown(sanitizedEndpoint);
        var actionKnown = !string.IsNullOrWhiteSpace(sanitizedSoapAction)
            && !string.Equals(sanitizedSoapAction, InvalidSanitizedEndpoint, StringComparison.Ordinal);
        var canApply = !targetRealSend || (destinationKnown && actionKnown);

        return new SoapIntegrationSettingsChangePreviewDto
        {
            MethodName = persisted.MethodName,
            Revision = raw.Revision,
            CurrentConfigurationState = currentState,
            RequestedConfigurationState = requestedState,
            SanitizedEndpoint = sanitizedEndpoint,
            SanitizedSoapAction = sanitizedSoapAction,
            WouldEnableRealSend = targetRealSend && !currentRealSend,
            CanApply = canApply,
            BlockingReason = canApply
                ? null
                : !destinationKnown
                    ? "Backend no pudo determinar un destino sanitizado para la comunicación externa."
                    : "Backend no pudo determinar la acción técnica vigente."
        };
    }

    public async Task<SoapIntegrationSettingsDto> SaveAsync(SoapIntegrationSettingsDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = await LoadSingleSettingsAsync(ct);
        var persistedRaw = settings is null
            ? new SoapIntegrationSettingsDto { Revision = EmptyRevision }
            : MapRawDto(settings);

        EnsureExpectedRevision(request.ExpectedRevision, persistedRaw.Revision);

        var normalizedRequest = Normalize(request);
        var merged = PreservePersistedSensitiveValues(normalizedRequest, persistedRaw);
        merged = PreservePersistedTimeouts(merged, persistedRaw);

        if (settings is null)
        {
            settings = new SoapIntegrationSetting();
            _dbContext.Set<SoapIntegrationSetting>().Add(settings);
        }

        settings.WscfaachMappingsJson = SerializePersistedMappings(merged.WscfaachMappings);
        settings.WsAxonRespuestaTransaccionesMappingsJson = SerializePersistedMappings(merged.WsAxonRespuestaTransaccionesMappings);

        await _dbContext.SaveChangesAsync(ct);

        var savedRaw = MapRawDto(settings);
        return await BuildClientResponseAsync(savedRaw, ct);
    }

    private async Task<SoapIntegrationSetting?> LoadSingleSettingsAsync(CancellationToken ct)
    {
        var settingsRows = await _dbContext.Set<SoapIntegrationSetting>()
            .OrderBy(x => x.Id)
            .Take(2)
            .ToListAsync(ct);

        EnsureUniqueSettingsRow(settingsRows);
        return settingsRows.SingleOrDefault();
    }

    private SoapIntegrationSettingsDto MapRawDto(SoapIntegrationSetting settings)
    {
        var wscfaach = Deserialize(settings.WscfaachMappingsJson);
        var wsAxon = Deserialize(settings.WsAxonRespuestaTransaccionesMappingsJson);

        var normalized = Normalize(new SoapIntegrationSettingsDto
        {
            WscfaachMappings = wscfaach,
            WsAxonRespuestaTransaccionesMappings = wsAxon
        });

        return normalized with { Revision = ComputeRevision(settings) };
    }

    private async Task<SoapIntegrationSettingsDto> BuildClientResponseAsync(
        SoapIntegrationSettingsDto raw,
        CancellationToken ct)
    {
        var effective = await WithEffectiveProcTransaccionesSettingsAsync(raw, ct);
        return effective with
        {
            ExpectedRevision = null,
            WscfaachMappings = PrepareClientMappings(effective.WscfaachMappings),
            WsAxonRespuestaTransaccionesMappings = PrepareClientMappings(effective.WsAxonRespuestaTransaccionesMappings),
            ProcTransaccionesEffectiveSettings = effective.ProcTransaccionesEffectiveSettings is null
                ? null
                : effective.ProcTransaccionesEffectiveSettings with
                {
                    Endpoint = SanitizeEndpoint(effective.ProcTransaccionesEffectiveSettings.Endpoint)
                }
        };
    }

    private static List<SoapEndpointMethodMappingDto> PrepareClientMappings(
        IEnumerable<SoapEndpointMethodMappingDto> mappings)
        => mappings.Select(mapping =>
        {
            var sanitizedEndpoint = SanitizeEndpoint(mapping.Endpoint);
            var sanitizedSoapAction = SanitizeTechnicalUri(mapping.SoapAction);
            var knownMethod = SupportedMethods.Contains(mapping.MethodName);
            var stateKnown = knownMethod && !string.IsNullOrWhiteSpace(mapping.MethodName);
            var canRequestRealSend = stateKnown
                && IsSanitizedDestinationKnown(sanitizedEndpoint)
                && !string.IsNullOrWhiteSpace(sanitizedSoapAction)
                && !string.Equals(sanitizedSoapAction, InvalidSanitizedEndpoint, StringComparison.Ordinal);

            return mapping with
            {
                Endpoint = sanitizedEndpoint,
                SoapAction = sanitizedSoapAction,
                SanitizedEndpoint = sanitizedEndpoint,
                SanitizedSoapAction = sanitizedSoapAction,
                ConfigurationState = BuildConfigurationState(mapping.OperatingMode, mapping.Enabled),
                Capabilities = new SoapEndpointMethodCapabilitiesDto
                {
                    ConfigurationStateKnown = stateKnown,
                    CanRequestRealSend = canRequestRealSend,
                    BlockingReason = stateKnown
                        ? canRequestRealSend
                            ? null
                            : "Backend no pudo determinar destino y acción técnica suficientes para confirmar envío real."
                        : "Backend no reconoce el servicio o su estado vigente."
                }
            };
        }).ToList();

    private static string SerializePersistedMappings(IEnumerable<SoapEndpointMethodMappingDto> mappings)
        => JsonSerializer.Serialize(
            mappings.Select(mapping => new
            {
                mapping.MethodName,
                mapping.Endpoint,
                mapping.SoapAction,
                mapping.OperatingMode,
                mapping.TimeoutSeconds,
                mapping.Enabled,
                mapping.InputParameterMappings
            }),
            JsonOptions);

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
        => new()
        {
            WscfaachMappings = NormalizeMappings(request.WscfaachMappings),
            WsAxonRespuestaTransaccionesMappings = NormalizeMappings(request.WsAxonRespuestaTransaccionesMappings)
        };

    private static List<SoapEndpointMethodMappingDto> NormalizeMappings(IEnumerable<SoapEndpointMethodMappingDto> mappings)
        => mappings
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

    private static SoapIntegrationSettingsDto PreservePersistedSensitiveValues(
        SoapIntegrationSettingsDto requested,
        SoapIntegrationSettingsDto persisted)
        => requested with
        {
            WscfaachMappings = PreservePersistedSensitiveValues(
                requested.WscfaachMappings,
                persisted.WscfaachMappings),
            WsAxonRespuestaTransaccionesMappings = PreservePersistedSensitiveValues(
                requested.WsAxonRespuestaTransaccionesMappings,
                persisted.WsAxonRespuestaTransaccionesMappings)
        };

    private static List<SoapEndpointMethodMappingDto> PreservePersistedSensitiveValues(
        IEnumerable<SoapEndpointMethodMappingDto> requested,
        IEnumerable<SoapEndpointMethodMappingDto> persisted)
    {
        var persistedByMethod = persisted
            .Where(x => !string.IsNullOrWhiteSpace(x.MethodName))
            .GroupBy(x => x.MethodName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        return requested.Select(mapping =>
        {
            if (!persistedByMethod.TryGetValue(mapping.MethodName, out var previous))
            {
                return mapping;
            }

            return mapping with
            {
                Endpoint = ResolveSensitiveValue(
                    mapping.Endpoint,
                    previous.Endpoint,
                    SanitizeEndpoint(previous.Endpoint)),
                SoapAction = ResolveSensitiveValue(
                    mapping.SoapAction,
                    previous.SoapAction,
                    SanitizeTechnicalUri(previous.SoapAction))
            };
        }).ToList();
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

    private static SoapEndpointMethodMappingDto? FindMethod(
        SoapIntegrationSettingsDto settings,
        string? methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return null;
        }

        return settings.WscfaachMappings
                   .Concat(settings.WsAxonRespuestaTransaccionesMappings)
                   .FirstOrDefault(x => string.Equals(x.MethodName, methodName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureExpectedRevision(string? expectedRevision, string currentRevision)
    {
        var expected = expectedRevision?.Trim() ?? string.Empty;
        if (expected.Length == 0 || !string.Equals(expected, currentRevision, StringComparison.Ordinal))
        {
            throw new SoapIntegrationSettingsRevisionConflictException(expected, currentRevision);
        }
    }

    private static string ComputeRevision(SoapIntegrationSetting settings)
    {
        var material = $"{settings.WscfaachMappingsJson}\n{settings.WsAxonRespuestaTransaccionesMappingsJson}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static string ResolveSensitiveValue(string? requested, string persisted, string sanitizedPersisted)
    {
        var value = requested?.Trim() ?? string.Empty;
        return string.Equals(value, sanitizedPersisted, StringComparison.Ordinal)
            ? persisted
            : value;
    }

    private static string SanitizeEndpoint(string? endpoint)
        => NachaSoapEndpointSafetyValidator.SanitizeEndpoint(endpoint ?? string.Empty);

    private static string SanitizeTechnicalUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return InvalidSanitizedEndpoint;
        }

        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.GetLeftPart(UriPartial.Path);
    }

    private static bool IsSanitizedDestinationKnown(string value)
        => !string.IsNullOrWhiteSpace(value)
           && !string.Equals(value, InvalidSanitizedEndpoint, StringComparison.Ordinal);

    private static bool IsRealSend(string? operatingMode, bool enabled)
        => enabled && string.Equals(NormalizeOperatingMode(operatingMode), "Live", StringComparison.OrdinalIgnoreCase);

    private static string BuildConfigurationState(string? operatingMode, bool enabled)
    {
        var normalized = NormalizeOperatingMode(operatingMode);
        if (!enabled || string.Equals(normalized, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return "Disabled";
        }

        return string.Equals(normalized, "Live", StringComparison.OrdinalIgnoreCase)
            ? "RealSendEnabled"
            : "Simulation";
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
