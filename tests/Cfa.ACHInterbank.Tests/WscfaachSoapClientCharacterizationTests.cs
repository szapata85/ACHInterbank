using System.Net;
using System.Text;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.External.Connections;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class WscfaachSoapClientCharacterizationTests
{
    [Fact]
    public async Task ProcTransaccionesAsync_WithExistingEnvelope_SendsSingleOutboundEnvelopeWithoutOtherSoapMethods()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>R96</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>")
            });

        var sut = BuildClient(server.Url);
        const string envelope = """
            <Envelope xmlns="http://schemas.xmlsoap.org/soap/envelope/">
              <Body>
                <Proc_Transacciones xmlns="http://tempuri.org/">
                  <TREG>6</TREG>
                  <TIPTRAN>22</TIPTRAN>
                  <IDTRAN>123</IDTRAN>
                </Proc_Transacciones>
              </Body>
            </Envelope>
            """;

        await sut.ProcTransaccionesAsync(envelope);

        var request = server.Requests.Single();
        Assert.Equal(1, CountOccurrences(request.Body, "<Envelope", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Proc_Transacciones", request.Body);
        Assert.DoesNotContain("<METODO>", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Proc_Contrapartidas", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrarRespuestaTransaccion", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLValidarUsuarioBV", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("http://tempuri.org/IWSCFAACH/Proc_Transacciones", request.SoapAction);
        Assert.StartsWith("text/xml", request.ContentType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcTransaccionesAsync_WithActionBody_WrapsOnceAsSoap11Envelope()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });

        var sut = BuildClient(server.Url);
        const string body = "<Proc_Transacciones xmlns=\"http://tempuri.org/\"><IDTRAN>123</IDTRAN></Proc_Transacciones>";

        await sut.ProcTransaccionesAsync(body);

        var request = server.Requests.Single();
        Assert.Equal(1, CountOccurrences(request.Body, "<soap:Envelope", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("<soap:Body>", request.Body);
        Assert.Contains(body, request.Body);
    }

    private static WscfaachSoapClient BuildClient(string endpoint)
    {
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto
        {
            WscfaachMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Transacciones",
                    Enabled = true,
                    Endpoint = endpoint,
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones"
                }
            ]
        });

        return new WscfaachSoapClient(
            Mock.Of<ILoggerManager>(),
            settings.Object,
            new StaticHttpClientFactory(),
            new ConfigurationBuilder().Build());
    }

    private static int CountOccurrences(string value, string pattern, StringComparison comparison)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(pattern, index, comparison)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class LocalSoapServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Func<HttpListenerRequest, string, HttpResponseMessage> _handler;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loopTask;

        public List<CapturedRequest> Requests { get; } = [];
        public string Url { get; }

        private LocalSoapServer(string url, Func<HttpListenerRequest, string, HttpResponseMessage> handler)
        {
            Url = url;
            _handler = handler;
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();
            _loopTask = Task.Run(ListenLoopAsync);
        }

        public static async Task<LocalSoapServer> StartAsync(Func<HttpListenerRequest, string, HttpResponseMessage> handler)
        {
            var port = GetFreePort();
            var url = $"http://127.0.0.1:{port}/";
            var server = new LocalSoapServer(url, handler);
            await Task.Delay(20);
            return server;
        }

        private async Task ListenLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext context;
                try { context = await _listener.GetContextAsync(); }
                catch { break; }

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                Requests.Add(new CapturedRequest(
                    body,
                    context.Request.Headers["SOAPAction"] ?? string.Empty,
                    context.Request.ContentType ?? string.Empty));

                var response = _handler(context.Request, body);
                context.Response.StatusCode = (int)response.StatusCode;
                if (response.Content is not null)
                {
                    var payload = await response.Content.ReadAsStringAsync();
                    var bytes = Encoding.UTF8.GetBytes(payload);
                    context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/xml";
                    await context.Response.OutputStream.WriteAsync(bytes);
                }

                context.Response.Close();
            }
        }

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
            try { _loopTask.Wait(500); } catch { }
            _cts.Dispose();
        }
    }

    private sealed record CapturedRequest(string Body, string SoapAction, string ContentType);
}
