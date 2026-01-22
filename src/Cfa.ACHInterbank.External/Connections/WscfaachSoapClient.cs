using System.Net.Http.Headers;
using System.Text;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public class WscfaachSoapClient : IWscfaachSoapClient
{
    private const string DefaultEndpoint = "http://esparta/WSCFAACH/WSCFAACH.svc";
    private const string SoapActionPrefix = "http://tempuri.org/IWSCFAACH/";
    private readonly ILoggerManager _logger;
    private readonly AppSettings _appSettings = AppSettings.Settings;

    public WscfaachSoapClient(ILoggerManager logger)
    {
        _logger = logger;
    }

    public Task<string> PLValidarUsuarioBVAsync(string requestXml, CancellationToken ct = default)
        => SendAsync("PLValidarUsuarioBV", requestXml, ct);

    public Task<string> ProcContrapartidasAsync(string requestXml, CancellationToken ct = default)
        => SendAsync("Proc_Contrapartidas", requestXml, ct);

    public Task<string> ProcTransaccionesAsync(string requestXml, CancellationToken ct = default)
        => SendAsync("Proc_Transacciones", requestXml, ct);

    public Task<IReadOnlyList<string>> ProcTransaccionesParallelAsync(
        IEnumerable<string> requestXmls,
        int degreeOfParallelism = 4,
        CancellationToken ct = default)
    {
        if (requestXmls is null)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        return Task.Run<IReadOnlyList<string>>(
            () => requestXmls
                .AsParallel()
                .WithDegreeOfParallelism(degreeOfParallelism)
                .WithCancellation(ct)
                .Select(xml => ProcTransaccionesAsync(xml, ct).GetAwaiter().GetResult())
                .ToArray(),
            ct);
    }

    private async Task<string> SendAsync(string action, string requestXml, CancellationToken ct)
    {
        var endpoint = _appSettings.Integrations?.UrlAch ?? DefaultEndpoint;
        var soapAction = $"{SoapActionPrefix}{action}";
        var envelope = BuildEnvelope(requestXml);

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        request.Headers.Add("SOAPAction", soapAction);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

        _logger.LogInfo($"SOAP request {action} -> {endpoint}");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
            .ConfigureAwait(false);

        var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"SOAP error {action}: {response.StatusCode} - {responseContent}");
            throw new InvalidOperationException($"SOAP error {action}: {response.StatusCode}");
        }

        return responseContent;
    }

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
}
