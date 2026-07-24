using System.Net.Http.Headers;
using System.Text;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public class WsAxonRespuestaTransaccionesSoapClient : IWsAxonRespuestaTransaccionesSoapClient
{
    private static readonly HashSet<string> ControlledLocalHosts =
        new(["localhost", "127.0.0.1", "host.docker.internal"], StringComparer.OrdinalIgnoreCase);

    private readonly ILoggerManager _logger;
    private readonly ISoapIntegrationSettingsService _soapSettingsService;
    private readonly WsAxonEndpointSecurityOptions _endpointSecurity;

    public WsAxonRespuestaTransaccionesSoapClient(
        ILoggerManager logger,
        ISoapIntegrationSettingsService soapSettingsService,
        IOptions<WsAxonEndpointSecurityOptions> endpointSecurity)
    {
        _logger = logger;
        _soapSettingsService = soapSettingsService;
        _endpointSecurity = endpointSecurity.Value;
    }

    public Task<string> RegistrarRespuestaTransaccionAsync(string requestXml, CancellationToken ct = default)
        => SendAsync(requestXml, ct);

    public Task<string> RegistrarRespuestaTransaccionAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default)
        => SendAsync(BuildBody(parameters), ct);

    public async Task<IReadOnlyList<string>> RegistrarRespuestaTransaccionParallelAsync(
    IEnumerable<string> requestXmls,
    int degreeOfParallelism = 4,
    CancellationToken ct = default)
    {
        if (requestXmls is null)
            return [];

        // Materializamos para poder indexar y preservar el orden de entrada.
        var xmlArray = requestXmls as string[] ?? requestXmls.ToArray();

        // El resultado mantiene el mismo orden que xmlArray.
        var results = new string[xmlArray.Length];

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = degreeOfParallelism,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, xmlArray.Length),
            options,
            async (index, token) =>
            {
                var xml = xmlArray[index];

                var respuesta = await RegistrarRespuestaTransaccionAsync(xml, token)
                    .ConfigureAwait(false);

                // Guardamos en la misma posición para mantener el orden.
                results[index] = respuesta;
            });

        return results;
    }

    public async Task<IReadOnlyList<string>> RegistrarRespuestaTransaccionParallelAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> parameterSets,
        int degreeOfParallelism = 4,
        CancellationToken ct = default)
    {
        if (parameterSets is null)
        {
            return [];
        }

        var bodies = parameterSets
            .Select(BuildBody)
            .ToArray();

        return await RegistrarRespuestaTransaccionParallelAsync(bodies, degreeOfParallelism, ct)
            .ConfigureAwait(false);
    }

    private async Task<string> SendAsync(string requestXml, CancellationToken ct)
    {
        var (endpoint, soapAction) = await ResolveConfigurationAsync(ct)
            .ConfigureAwait(false);
        var envelope = BuildEnvelope(requestXml);

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        ApplyControlledLocalHostHeader(request, endpoint, _endpointSecurity);
        request.Headers.Add("SOAPAction", soapAction);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

        _logger.LogInfo($"SOAP request RegistrarRespuestaTransaccion -> {endpoint}");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
            .ConfigureAwait(false);

        var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            // El payload puede contener cuentas, identificadores y datos personales.
            // Conservamos únicamente metadatos operativos seguros.
            _logger.LogError(
                $"SOAP error RegistrarRespuestaTransaccion: HTTP {(int)response.StatusCode}; " +
                $"contentType={response.Content.Headers.ContentType?.MediaType ?? "unknown"}; " +
                $"contentLength={responseContent.Length}");
            throw new InvalidOperationException(
                $"SOAP error RegistrarRespuestaTransaccion: {response.StatusCode}");
        }

        return responseContent;
    }

    private static void ApplyControlledLocalHostHeader(
        HttpRequestMessage request,
        Uri endpoint,
        WsAxonEndpointSecurityOptions policy)
    {
        if (string.IsNullOrWhiteSpace(policy.HostHeader))
        {
            return;
        }

        if (policy.Mode != WsAxonEndpointSecurityMode.ControlledLocal)
        {
            throw new InvalidOperationException(
                "SOAP Host header override is only allowed by the ControlledLocal WSAXON policy.");
        }

        var hostHeader = policy.HostHeader.Trim();
        if (hostHeader.IndexOfAny(['/', '\\', '@', '?', '#', '\r', '\n']) >= 0
            || !Uri.TryCreate($"{endpoint.Scheme}://{hostHeader}", UriKind.Absolute, out var hostUri)
            || !ControlledLocalHosts.Contains(hostUri.IdnHost)
            || hostUri.Port != endpoint.Port)
        {
            throw new InvalidOperationException(
                "SOAP Host header override is outside the ControlledLocal WSAXON policy.");
        }

        request.Headers.Host = hostHeader;
    }

    private async Task<(Uri Endpoint, string SoapAction)> ResolveConfigurationAsync(CancellationToken ct)
    {
        const string action = "RegistrarRespuestaTransaccion";
        var settings = await _soapSettingsService.GetAsync(ct).ConfigureAwait(false);

        var mapping = settings.WsAxonRespuestaTransaccionesMappings
            .FirstOrDefault(x => string.Equals(x.MethodName, action, StringComparison.OrdinalIgnoreCase));

        if (mapping is not null && !mapping.Enabled)
        {
            throw new InvalidOperationException($"SOAP method '{action}' is disabled by configuration.");
        }

        if (mapping is null)
        {
            throw new InvalidOperationException($"SOAP mapping for '{action}' is not configured in database.");
        }

        if (string.IsNullOrWhiteSpace(mapping.Endpoint))
        {
            throw new InvalidOperationException($"SOAP endpoint for '{action}' is not configured in database.");
        }

        if (string.IsNullOrWhiteSpace(mapping.SoapAction))
        {
            throw new InvalidOperationException($"SOAP action for '{action}' is not configured in database.");
        }

        return (ValidateEndpoint(mapping.Endpoint, _endpointSecurity), mapping.SoapAction);
    }

    private static Uri ValidateEndpoint(string endpoint, WsAxonEndpointSecurityOptions policy)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                "SOAP endpoint for 'RegistrarRespuestaTransaccion' must be an absolute URI.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException(
                "SOAP endpoint for 'RegistrarRespuestaTransaccion' cannot contain embedded credentials.");
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                "SOAP endpoint for 'RegistrarRespuestaTransaccion' cannot contain a fragment.");
        }

        return policy.Mode switch
        {
            WsAxonEndpointSecurityMode.ControlledLocal => ValidateControlledLocal(uri, policy),
            WsAxonEndpointSecurityMode.ConfiguredAllowlist => ValidateConfiguredAllowlist(uri, policy),
            _ => throw new InvalidOperationException(
                "SOAP endpoint security policy for 'RegistrarRespuestaTransaccion' is not configured.")
        };
    }

    private static Uri ValidateControlledLocal(Uri uri, WsAxonEndpointSecurityOptions policy)
    {
        var allowedPorts = policy.AllowedPorts.Count == 0 ? [7083] : ValidatePorts(policy.AllowedPorts);
        var allowedHost = ControlledLocalHosts.Contains(uri.Host);
        var expectedPath = PathsEqual(uri.AbsolutePath, "/WSAxonRespuestaTransacciones.svc");

        if (policy.RequireHttps
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || !allowedHost
            || !allowedPorts.Contains(uri.Port)
            || !expectedPath)
        {
            throw new InvalidOperationException(
                "SOAP endpoint for 'RegistrarRespuestaTransaccion' is outside the ControlledLocal WSAXON policy.");
        }

        return uri;
    }

    private static Uri ValidateConfiguredAllowlist(Uri uri, WsAxonEndpointSecurityOptions policy)
    {
        var schemes = NormalizeSchemes(policy.AllowedSchemes);
        var hosts = NormalizeHosts(policy.AllowedHosts);
        var paths = NormalizePaths(policy.AllowedPaths);
        var ports = ValidatePorts(policy.AllowedPorts);

        if (schemes.Count == 0 || hosts.Count == 0 || paths.Count == 0)
        {
            throw new InvalidOperationException(
                "ConfiguredAllowlist for 'RegistrarRespuestaTransaccion' requires explicit schemes, hosts and paths.");
        }

        if (policy.RequireHttps && !schemes.Contains(Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "ConfiguredAllowlist for 'RegistrarRespuestaTransaccion' is inconsistent with RequireHttps.");
        }

        var allowed = (!policy.RequireHttps || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && schemes.Contains(uri.Scheme)
            && hosts.Contains(uri.IdnHost)
            && (ports.Count == 0 || ports.Contains(uri.Port))
            && paths.Any(path => PathsEqual(uri.AbsolutePath, path));

        if (!allowed)
        {
            throw new InvalidOperationException(
                "SOAP endpoint for 'RegistrarRespuestaTransaccion' is outside the ConfiguredAllowlist policy.");
        }

        return uri;
    }

    private static HashSet<string> NormalizeSchemes(IEnumerable<string> configured)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in configured)
        {
            var scheme = value?.Trim() ?? string.Empty;
            if (ContainsWildcard(scheme)
                || (!string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "ConfiguredAllowlist contains an invalid or unsafe SOAP scheme.");
            }

            result.Add(scheme);
        }

        return result;
    }

    private static HashSet<string> NormalizeHosts(IEnumerable<string> configured)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in configured)
        {
            var host = value?.Trim() ?? string.Empty;
            if (ContainsWildcard(host)
                || Uri.CheckHostName(host) == UriHostNameType.Unknown
                || host.Contains("://", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ConfiguredAllowlist contains an invalid or unsafe SOAP host.");
            }

            result.Add(new UriBuilder(Uri.UriSchemeHttps, host).Uri.IdnHost);
        }

        return result;
    }

    private static List<string> NormalizePaths(IEnumerable<string> configured)
    {
        var result = new List<string>();
        foreach (var value in configured)
        {
            var path = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path)
                || !path.StartsWith('/')
                || ContainsWildcard(path)
                || path.Contains('#')
                || path.Contains('?'))
            {
                throw new InvalidOperationException(
                    "ConfiguredAllowlist contains an invalid or unsafe SOAP path.");
            }

            result.Add(path);
        }

        return result;
    }

    private static HashSet<int> ValidatePorts(IEnumerable<int> configured)
    {
        var result = new HashSet<int>();
        foreach (var port in configured)
        {
            if (port is < 1 or > 65535)
            {
                throw new InvalidOperationException(
                    "SOAP endpoint security policy contains an invalid port.");
            }

            result.Add(port);
        }

        return result;
    }

    private static bool PathsEqual(string left, string right)
        => string.Equals(
            left.TrimEnd('/'),
            right.TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);

    private static bool ContainsWildcard(string value)
        => value.Contains('*') || value.Contains('?');

    private static string BuildEnvelope(string body)
    {
        return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                  <soap:Body>
                    {body}
                  </soap:Body>
                </soap:Envelope>
                """;
    }

    private static string BuildBody(IReadOnlyDictionary<string, object?> parameters)
        => SoapParameterMapper.BuildActionBody("RegistrarRespuestaTransaccion", parameters, "http://tempuri.org/");
}
