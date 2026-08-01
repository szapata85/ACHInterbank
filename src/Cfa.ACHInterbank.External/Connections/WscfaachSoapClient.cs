using System.Net.Http.Headers;
using System.Text;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public class WscfaachSoapClient : IWscfaachSoapClient
{
    private readonly ILoggerManager _logger;
    private readonly ISoapIntegrationSettingsService _soapSettingsService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ControlledLocalSoapTransportOptions _controlledLocalTransport;

    public WscfaachSoapClient(
        ILoggerManager logger,
        ISoapIntegrationSettingsService soapSettingsService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<ControlledLocalSoapTransportOptions>? controlledLocalTransport = null)
    {
        _logger = logger;
        _soapSettingsService = soapSettingsService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _controlledLocalTransport = controlledLocalTransport?.Value ?? new ControlledLocalSoapTransportOptions();
    }

    public Task<string> PLValidarUsuarioBVAsync(string requestXml, CancellationToken ct = default)
        => SendAsync("PLValidarUsuarioBV", requestXml, ct);

    public Task<string> PLValidarUsuarioBVAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default)
        => SendAsync("PLValidarUsuarioBV", BuildBody("PLValidarUsuarioBV", parameters), ct);

    public Task<string> ProcContrapartidasAsync(string requestXml, CancellationToken ct = default)
        => SendAsync("Proc_Contrapartidas", requestXml, ct);

    public Task<string> ProcContrapartidasAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default)
        => SendAsync("Proc_Contrapartidas", BuildBody("Proc_Contrapartidas", parameters), ct);

    public Task<string> ProcTransaccionesAsync(string requestXml, CancellationToken ct = default)
        => SendAsync("Proc_Transacciones", requestXml, ct);

    public Task<string> ProcTransaccionesAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default)
        => SendAsync("Proc_Transacciones", BuildBody("Proc_Transacciones", parameters), ct);

    public Task<IReadOnlyList<string>> ProcTransaccionesParallelAsync(
        IEnumerable<string> requestXmls,
        int degreeOfParallelism = 4,
        CancellationToken ct = default)
    {
        if (requestXmls is null)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        return ProcTransaccionesParallelInternalAsync(requestXmls, degreeOfParallelism, ct);
    }

    public Task<IReadOnlyList<string>> ProcTransaccionesParallelAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> parameterSets,
        int degreeOfParallelism = 4,
        CancellationToken ct = default)
    {
        if (parameterSets is null)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var bodies = parameterSets
            .Select(parameters => BuildBody("Proc_Transacciones", parameters))
            .ToArray();

        return ProcTransaccionesParallelInternalAsync(bodies, degreeOfParallelism, ct);
    }


    private async Task<IReadOnlyList<string>> ProcTransaccionesParallelInternalAsync(
        IEnumerable<string> requestXmls,
        int degreeOfParallelism,
        CancellationToken ct)
    {
        var requests = requestXmls.ToArray();
        if (requests.Length == 0)
        {
            return [];
        }

        if (degreeOfParallelism <= 0)
        {
            degreeOfParallelism = 1;
        }

        var responses = new string[requests.Length];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, requests.Length),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = degreeOfParallelism,
                CancellationToken = ct
            },
            async (index, token) =>
            {
                responses[index] = await ProcTransaccionesAsync(requests[index], token).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return responses;
    }

    private async Task<string> SendAsync(string action, string requestXml, CancellationToken ct)
    {
        var (logicalEndpoint, soapAction, timeout) = await ResolveConfigurationAsync(action, ct)
            .ConfigureAwait(false);
        var transport = ControlledLocalSoapTransportResolver.Resolve(
            logicalEndpoint,
            ResolveControlledLocalTransportOptions(),
            _configuration["WSCFAACH:HostHeader"]);
        var envelope = EnsureEnvelope(requestXml);

        using var client = _httpClientFactory.CreateClient(nameof(WscfaachSoapClient));
        client.Timeout = timeout;
        using var request = new HttpRequestMessage(HttpMethod.Post, transport.TransportEndpoint);
        request.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        request.Headers.Add("SOAPAction", soapAction);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
        if (!string.IsNullOrWhiteSpace(transport.HostHeader))
        {
            request.Headers.Host = transport.HostHeader;
        }

        _logger.LogInfo($"SOAP request {action} -> {logicalEndpoint}; transport={transport.TransportEndpoint}");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
            .ConfigureAwait(false);

        var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"SOAP error {action}: HTTP {(int)response.StatusCode} ({response.StatusCode}). Response body redacted.");
            throw new InvalidOperationException($"SOAP error {action}: {response.StatusCode}");
        }

        return responseContent;
    }

    private async Task<(Uri Endpoint, string SoapAction, TimeSpan Timeout)> ResolveConfigurationAsync(
        string action,
        CancellationToken ct)
    {
        var settings = await _soapSettingsService.GetAsync(ct).ConfigureAwait(false);

        var mapping = settings.WscfaachMappings
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

        if (!string.Equals(mapping.OperatingMode, "Live", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SOAP method '{action}' is not enabled for Live execution by database configuration.");
        }

        if (mapping.TimeoutSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException(
                $"SOAP timeout for '{action}' is not valid in database configuration.");
        }

        if (!Uri.TryCreate(mapping.Endpoint, UriKind.Absolute, out var endpoint)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException(
                $"SOAP endpoint for '{action}' is not a safe absolute URI in database configuration.");
        }

        return (endpoint, mapping.SoapAction, TimeSpan.FromSeconds(mapping.TimeoutSeconds));
    }

    private ControlledLocalSoapTransportOptions ResolveControlledLocalTransportOptions()
        => new()
        {
            TransportHost = string.IsNullOrWhiteSpace(_controlledLocalTransport.TransportHost)
                ? _configuration["WSCFAACH:TransportHost"]
                : _controlledLocalTransport.TransportHost,
            HostHeader = _controlledLocalTransport.HostHeader
        };

    private static string EnsureEnvelope(string body)
        => IsSoapEnvelope(body) ? body : BuildEnvelope(body);

    private static bool IsSoapEnvelope(string body)
        => !string.IsNullOrWhiteSpace(body)
            && (body.Contains("<Envelope", StringComparison.OrdinalIgnoreCase)
                || body.Contains(":Envelope", StringComparison.OrdinalIgnoreCase))
            && (body.Contains("</Envelope>", StringComparison.OrdinalIgnoreCase)
                || body.Contains(":Envelope>", StringComparison.OrdinalIgnoreCase));

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

    private static string BuildBody(string action, IReadOnlyDictionary<string, object?> parameters)
        => SoapParameterMapper.BuildActionBody(action, parameters, "http://tempuri.org/");
}
