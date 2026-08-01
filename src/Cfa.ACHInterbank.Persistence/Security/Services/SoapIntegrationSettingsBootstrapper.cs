using System.Text.Json;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

public sealed class SoapIntegrationSettingsBootstrapper
{
    public const string SectionName = "SoapIntegrationBootstrap";

    private const string ProcTransaccionesMethodCode = "WSCFAACH.Proc_Transacciones";
    private const string ProcTransaccionesMethodName = "Proc_Transacciones";
    private const string RegistrarRespuestaMethodCode = "WSAXON.RegistrarRespuestaTransaccion";
    private const string RegistrarRespuestaMethodName = "RegistrarRespuestaTransaccion";

    private static readonly string[] RegistrarRespuestaWsdlParameters =
    [
        "idCanal",
        "nombreCanal",
        "idTransaccion",
        "idEstado",
        "causal",
        "idTransaccionAxon",
        "descripcionCausal"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AchDbContext _context;
    private readonly IConfiguration _configuration;

    public SoapIntegrationSettingsBootstrapper(AchDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        var section = _configuration.GetSection(SectionName);
        if (!section.GetValue<bool>("Enabled"))
        {
            return;
        }

        var defaultTimeoutSeconds = ReadTimeout(section, "DefaultTimeoutSeconds");
        var procSpec = ReadOperation(
            section.GetSection("ProcTransacciones"),
            ProcTransaccionesMethodName,
            defaultTimeoutSeconds);
        var registrarSpec = ReadOperation(
            section.GetSection("RegistrarRespuestaTransaccion"),
            RegistrarRespuestaMethodName,
            defaultTimeoutSeconds);

        var procParameters = await ReadInputParametersAsync(ProcTransaccionesMethodCode, ct);
        var registrarParameters = await ReadInputParametersAsync(RegistrarRespuestaMethodCode, ct);
        ValidateRegistrarContract(registrarParameters);

        var settingsRows = await _context.SoapIntegrationSettings
            .OrderBy(x => x.Id)
            .Take(2)
            .ToListAsync(ct);
        if (settingsRows.Count > 1)
        {
            throw new InvalidOperationException(
                "SOAP_SETTINGS_NOT_UNIQUE: more than one SoapIntegrationSettings row exists.");
        }

        var settings = settingsRows.SingleOrDefault();
        var wscfaach = DeserializeMappings(settings?.WscfaachMappingsJson, "WSCFAACH");
        var wsAxon = DeserializeMappings(settings?.WsAxonRespuestaTransaccionesMappingsJson, "WSAXON");

        EnsureExistingTimeouts(wscfaach, defaultTimeoutSeconds);
        Upsert(
            wscfaach,
            BuildMapping(procSpec, procParameters));
        Upsert(
            wsAxon,
            BuildMapping(registrarSpec, registrarParameters));

        var wscfaachJson = JsonSerializer.Serialize(wscfaach, JsonOptions);
        var wsAxonJson = JsonSerializer.Serialize(wsAxon, JsonOptions);

        if (settings is null)
        {
            settings = new SoapIntegrationSetting
            {
                WscfaachMappingsJson = wscfaachJson,
                WsAxonRespuestaTransaccionesMappingsJson = wsAxonJson
            };
            _context.SoapIntegrationSettings.Add(settings);
        }
        else
        {
            if (string.Equals(settings.WscfaachMappingsJson, wscfaachJson, StringComparison.Ordinal)
                && string.Equals(settings.WsAxonRespuestaTransaccionesMappingsJson, wsAxonJson, StringComparison.Ordinal))
            {
                return;
            }

            settings.WscfaachMappingsJson = wscfaachJson;
            settings.WsAxonRespuestaTransaccionesMappingsJson = wsAxonJson;
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<IntegrationMethodParameter>> ReadInputParametersAsync(
        string methodCode,
        CancellationToken ct)
    {
        var method = await _context.IntegrationMethods
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == methodCode && x.IsActive, ct)
            ?? throw new InvalidOperationException(
                $"SOAP_BOOTSTRAP_METHOD_MISSING: active catalog method '{methodCode}' was not found.");

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.IsActive
                && x.Direction == IntegrationParameterDirectionEnum.Input)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ParameterPath)
            .ToListAsync(ct);

        if (parameters.Count == 0)
        {
            throw new InvalidOperationException(
                $"SOAP_BOOTSTRAP_PARAMETERS_MISSING: active input parameters for '{methodCode}' were not found.");
        }

        return parameters;
    }

    private static SoapEndpointMethodMappingDto BuildMapping(
        SoapOperationBootstrapSpec spec,
        IReadOnlyList<IntegrationMethodParameter> parameters)
        => new()
        {
            MethodName = spec.MethodName,
            Endpoint = spec.Endpoint,
            SoapAction = spec.SoapAction,
            OperatingMode = spec.OperatingMode,
            TimeoutSeconds = spec.TimeoutSeconds,
            Enabled = spec.Enabled,
            InputParameterMappings = parameters
                .Select(x => new SoapInputParameterMappingDto
                {
                    InputName = x.ParameterPath,
                    SoapParameterName = x.ParameterPath,
                    Required = x.Required
                })
                .ToList()
        };

    private static void ValidateRegistrarContract(IReadOnlyList<IntegrationMethodParameter> parameters)
    {
        var actual = parameters
            .Select(x => x.ParameterPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (actual.Count != RegistrarRespuestaWsdlParameters.Length
            || !actual.SetEquals(RegistrarRespuestaWsdlParameters))
        {
            throw new InvalidOperationException(
                "SOAP_BOOTSTRAP_REGISTRAR_CONTRACT_INVALID: catalog must expose exactly the seven WSDL parameters.");
        }
    }

    private static List<SoapEndpointMethodMappingDto> DeserializeMappings(string? json, string catalog)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<SoapEndpointMethodMappingDto>>(json, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"SOAP_BOOTSTRAP_JSON_INVALID: persisted {catalog} settings are not valid JSON.",
                ex);
        }
    }

    private static void EnsureExistingTimeouts(
        IList<SoapEndpointMethodMappingDto> mappings,
        int defaultTimeoutSeconds)
    {
        for (var index = 0; index < mappings.Count; index++)
        {
            if (mappings[index].TimeoutSeconds <= 0)
            {
                mappings[index] = mappings[index] with { TimeoutSeconds = defaultTimeoutSeconds };
            }
        }
    }

    private static void Upsert(
        IList<SoapEndpointMethodMappingDto> mappings,
        SoapEndpointMethodMappingDto desired)
    {
        var matchingIndexes = mappings
            .Select((mapping, index) => (mapping, index))
            .Where(x => string.Equals(
                x.mapping.MethodName,
                desired.MethodName,
                StringComparison.OrdinalIgnoreCase))
            .Select(x => x.index)
            .ToList();

        if (matchingIndexes.Count == 0)
        {
            mappings.Add(desired);
            return;
        }

        mappings[matchingIndexes[0]] = desired;
        for (var index = matchingIndexes.Count - 1; index > 0; index--)
        {
            mappings.RemoveAt(matchingIndexes[index]);
        }
    }

    private static SoapOperationBootstrapSpec ReadOperation(
        IConfigurationSection section,
        string methodName,
        int defaultTimeoutSeconds)
    {
        var endpoint = ReadRequired(section, "Endpoint", methodName);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
            || !string.IsNullOrEmpty(endpointUri.UserInfo)
            || !string.IsNullOrEmpty(endpointUri.Fragment))
        {
            throw new InvalidOperationException(
                $"SOAP_BOOTSTRAP_ENDPOINT_INVALID: '{methodName}' requires a safe absolute endpoint URI.");
        }

        var soapAction = ReadRequired(section, "SoapAction", methodName);
        var operatingMode = ReadRequired(section, "OperatingMode", methodName);
        if (!string.Equals(operatingMode, "Live", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(operatingMode, "DryRun", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(operatingMode, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SOAP_BOOTSTRAP_MODE_INVALID: '{methodName}' mode must be Live, DryRun or Disabled.");
        }

        var enabledValue = section["Enabled"];
        if (!bool.TryParse(enabledValue, out var enabled))
        {
            throw new InvalidOperationException(
                $"SOAP_BOOTSTRAP_ENABLED_INVALID: '{methodName}' requires an explicit boolean Enabled value.");
        }

        var timeoutSeconds = string.IsNullOrWhiteSpace(section["TimeoutSeconds"])
            ? defaultTimeoutSeconds
            : ReadTimeout(section, "TimeoutSeconds");

        return new SoapOperationBootstrapSpec(
            methodName,
            endpointUri.AbsoluteUri,
            soapAction,
            NormalizeMode(operatingMode),
            timeoutSeconds,
            enabled);
    }

    private static int ReadTimeout(IConfiguration section, string key)
    {
        var raw = section[key];
        if (!int.TryParse(raw, out var timeoutSeconds) || timeoutSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException(
                $"SOAP_BOOTSTRAP_TIMEOUT_INVALID: '{key}' must be between 1 and 300 seconds.");
        }

        return timeoutSeconds;
    }

    private static string ReadRequired(IConfiguration section, string key, string methodName)
    {
        var value = section[key]?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"SOAP_BOOTSTRAP_VALUE_MISSING: '{methodName}.{key}' is required.");
        }

        return value;
    }

    private static string NormalizeMode(string mode)
        => string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase)
            ? "Live"
            : string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase)
                ? "Disabled"
                : "DryRun";

    private sealed record SoapOperationBootstrapSpec(
        string MethodName,
        string Endpoint,
        string SoapAction,
        string OperatingMode,
        int TimeoutSeconds,
        bool Enabled);
}
