using System.Net;
using System.Text;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.External.Connections;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class WsAxonRespuestaTransaccionesSoapClientCharacterizationTests
{
    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_WithXmlBody_WrapsSoap11Envelope_AndPreservesBody()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });

        var sut = BuildClient(server.Url, out _);
        var body = "<RegistrarRespuestaTransaccion><idTransaccion>TX-1</idTransaccion></RegistrarRespuestaTransaccion>";

        await sut.RegistrarRespuestaTransaccionAsync(body);

        var request = server.Requests.Single();
        Assert.Contains("<soap:Envelope", request.Body);
        Assert.Contains("<soap:Body>", request.Body);
        Assert.Contains(body, request.Body);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_WithParameters_BuildsExpectedActionAndNamespace()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });

        var sut = BuildClient(server.Url, out _);
        var parameters = new Dictionary<string, object?>
        {
            ["idCanal"] = 1,
            ["nombreCanal"] = "ACH",
            ["idTransaccion"] = "TX-001",
            ["idEstado"] = 2,
            ["causal"] = "R01",
            ["idTransaccionAxon"] = 99,
            ["descripcionCausal"] = "Cuenta cerrada"
        };

        await sut.RegistrarRespuestaTransaccionAsync(parameters);

        var body = server.Requests.Single().Body;
        Assert.Contains("RegistrarRespuestaTransaccion", body);
        Assert.Contains("http://tempuri.org/", body);
        Assert.Contains("idCanal", body);
        Assert.Contains("nombreCanal", body);
        Assert.Contains("idTransaccion", body);
        Assert.Contains("idEstado", body);
        Assert.Contains("causal", body);
        Assert.Contains("idTransaccionAxon", body);
        Assert.Contains("descripcionCausal", body);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_SendsConfiguredSoapAction_AndTextXmlContentType()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<resp>ok</resp>") });

        const string soapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion";
        var sut = BuildClient(server.Url, out _, soapAction);

        var response = await sut.RegistrarRespuestaTransaccionAsync("<Body>ok</Body>");

        var request = server.Requests.Single();
        Assert.Equal(soapAction, request.SoapAction);
        Assert.StartsWith("text/xml", request.ContentType, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("<resp>ok</resp>", response);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_WhenHttpIsNotSuccess_ThrowsInvalidOperationException()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("<err/>") });

        var sut = BuildClient(server.Url, out _);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegistrarRespuestaTransaccionAsync("<Body>error</Body>"));
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_WhenMappingMissing_ThrowsControlledError()
    {
        var sut = BuildClientWithoutMapping();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegistrarRespuestaTransaccionAsync("<x/>"));
        Assert.Contains("not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_WhenMappingDisabled_ThrowsControlledError()
    {
        var sut = BuildClientWithCustomMapping(new SoapEndpointMethodMappingDto
        {
            MethodName = "RegistrarRespuestaTransaccion",
            Enabled = false,
            Endpoint = "http://localhost",
            SoapAction = "act"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegistrarRespuestaTransaccionAsync("<x/>"));
        Assert.Contains("disabled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_WhenEndpointMissing_ThrowsControlledError()
    {
        var sut = BuildClientWithCustomMapping(new SoapEndpointMethodMappingDto
        {
            MethodName = "RegistrarRespuestaTransaccion",
            Enabled = true,
            Endpoint = " ",
            SoapAction = "act"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegistrarRespuestaTransaccionAsync("<x/>"));
        Assert.Contains("endpoint", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_WhenSoapActionMissing_ThrowsControlledError()
    {
        var sut = BuildClientWithCustomMapping(new SoapEndpointMethodMappingDto
        {
            MethodName = "RegistrarRespuestaTransaccion",
            Enabled = true,
            Endpoint = "http://localhost",
            SoapAction = "  "
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegistrarRespuestaTransaccionAsync("<x/>"));
        Assert.Contains("action", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://backend1.example.com/WSAxonRespuestaTransacciones.svc")]
    [InlineData("http://localhost:7083/WSCFAACH.svc")]
    [InlineData("ftp://localhost/WSAxonRespuestaTransacciones.svc")]
    public async Task RegistrarRespuestaTransaccionAsync_WhenEndpointIsOutsideControlledAllowlist_BlocksBeforeNetwork(string endpoint)
    {
        var sut = BuildClient(endpoint, out _);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Contains("controlled-local", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static WsAxonRespuestaTransaccionesSoapClient BuildClient(string endpoint, out Mock<ISoapIntegrationSettingsService> settingsMock, string? soapAction = null)
    {
        var mapping = new SoapEndpointMethodMappingDto
        {
            MethodName = "RegistrarRespuestaTransaccion",
            Enabled = true,
            Endpoint = endpoint,
            SoapAction = soapAction ?? "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion"
        };
        return BuildClientWithMockedSettings(mapping, out settingsMock);
    }

    private static WsAxonRespuestaTransaccionesSoapClient BuildClientWithoutMapping()
    {
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto());
        return new WsAxonRespuestaTransaccionesSoapClient(Mock.Of<ILoggerManager>(), settings.Object);
    }

    private static WsAxonRespuestaTransaccionesSoapClient BuildClientWithCustomMapping(SoapEndpointMethodMappingDto mapping)
    {
        return BuildClientWithMockedSettings(mapping, out _);
    }

    private static WsAxonRespuestaTransaccionesSoapClient BuildClientWithMockedSettings(SoapEndpointMethodMappingDto mapping, out Mock<ISoapIntegrationSettingsService> settings)
    {
        settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto
        {
            WsAxonRespuestaTransaccionesMappings = [mapping]
        });
        return new WsAxonRespuestaTransaccionesSoapClient(Mock.Of<ILoggerManager>(), settings.Object);
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
            var url = $"http://127.0.0.1:{port}/WSAxonRespuestaTransacciones.svc/";
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
                foreach (var h in response.Headers)
                    context.Response.Headers[h.Key] = string.Join(",", h.Value);
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
