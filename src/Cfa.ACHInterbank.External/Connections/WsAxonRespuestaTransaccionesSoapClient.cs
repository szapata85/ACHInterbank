using System.Net.Http.Headers;
using System.Text;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public class WsAxonRespuestaTransaccionesSoapClient : IWsAxonRespuestaTransaccionesSoapClient
{
    private const string DefaultEndpoint = "http://esparta/WSCFAACH/WSAxonRespuestaTransacciones.svc";
    private const string SoapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion";
    private readonly ILoggerManager _logger;
    private readonly AppSettings _appSettings = AppSettings.Settings;

    public WsAxonRespuestaTransaccionesSoapClient(ILoggerManager logger)
    {
        _logger = logger;
    }

    public Task<string> RegistrarRespuestaTransaccionAsync(string requestXml, CancellationToken ct = default)
        => SendAsync(requestXml, ct);

    public Task<IReadOnlyList<string>> RegistrarRespuestaTransaccionParallelAsync(
        IEnumerable<string> requestXmls,
        int degreeOfParallelism = 4,
        CancellationToken ct = default)
    {
        if (requestXmls is null)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        return Task.Run(
            () => (IReadOnlyList<string>)requestXmls
                .AsParallel()
                .WithDegreeOfParallelism(degreeOfParallelism)
                .WithCancellation(ct)
                .Select(xml => RegistrarRespuestaTransaccionAsync(xml, ct).GetAwaiter().GetResult())
                .ToArray(),
            ct);
    }

    private async Task<string> SendAsync(string requestXml, CancellationToken ct)
    {
        var endpoint = _appSettings.Integrations?.UrlAch ?? DefaultEndpoint;
        var envelope = BuildEnvelope(requestXml);

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        request.Headers.Add("SOAPAction", SoapAction);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

        _logger.LogInfo($"SOAP request RegistrarRespuestaTransaccion -> {endpoint}");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
            .ConfigureAwait(false);

        var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"SOAP error RegistrarRespuestaTransaccion: {response.StatusCode} - {responseContent}");
            throw new InvalidOperationException(
                $"SOAP error RegistrarRespuestaTransaccion: {response.StatusCode}");
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
