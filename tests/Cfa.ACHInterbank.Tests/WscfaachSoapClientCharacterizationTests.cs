using System.Net;
using System.Text;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.External.Connections;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class WscfaachSoapClientCharacterizationTests
{
    [Fact]
    public void AddExternal_WithZeroRetries_CreatesSoapHttpClientWithoutValidationFailure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:Soap:MaxRetryAttempts"] = "0",
                ["appSettings:tokenManager:issuerJwt"] = "synthetic-test-issuer",
                ["appSettings:tokenManager:audienceJwt"] = "synthetic-test-audience",
                ["appSettings:tokenManager:secretKetJwt"] = "synthetic-unit-test-key-32-bytes-minimum"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExternal(configuration);
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(WscfaachSoapClient));

        Assert.NotNull(client);
    }

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

    [Fact]
    public async Task ProcContrapartidasAsync_SendsOnlyExpectedMethodWithoutMetodo()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var settings = Settings(server.Url, "Proc_Contrapartidas");
        var sut = new WscfaachSoapClient(
            Mock.Of<ILoggerManager>(), settings.Object, new StaticHttpClientFactory(), new ConfigurationBuilder().Build());

        await sut.ProcContrapartidasAsync("<Proc_Contrapartidas xmlns=\"http://tempuri.org/\"><OFIDTX>1</OFIDTX></Proc_Contrapartidas>");

        var request = Assert.Single(server.Requests);
        Assert.Contains("Proc_Contrapartidas", request.Body);
        Assert.DoesNotContain("<METODO>", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Proc_Transacciones", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrarRespuestaTransaccion", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLValidarUsuarioBV", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("http://tempuri.org/IWSCFAACH/Proc_Contrapartidas", request.SoapAction);
    }

    [Fact]
    public async Task SoapHttpError_DoesNotWriteResponseBodyToLogOrException()
    {
        const string sensitiveResponse = "SENSITIVE-FINANCIAL-PAYLOAD";
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(sensitiveResponse) });
        var logger = new Mock<ILoggerManager>();
        var sut = new WscfaachSoapClient(
            logger.Object, Settings(server.Url, "Proc_Contrapartidas").Object,
            new StaticHttpClientFactory(), new ConfigurationBuilder().Build());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcContrapartidasAsync("<Proc_Contrapartidas xmlns=\"http://tempuri.org/\"/>"));

        Assert.DoesNotContain(sensitiveResponse, error.Message);
        logger.Verify(x => x.LogError(It.Is<string>(message =>
            message.Contains("Response body redacted", StringComparison.Ordinal)
            && !message.Contains(sensitiveResponse, StringComparison.Ordinal))), Times.Once);
    }

    [Fact]
    public async Task ProcTransaccionesAsync_WhenDatabaseModeIsNotLive_StopsBeforeNetwork()
    {
        var settings = Settings(
            "http://127.0.0.1:1/WSCFAACH.svc",
            "Proc_Transacciones",
            operatingMode: "DryRun");
        var sut = new WscfaachSoapClient(
            Mock.Of<ILoggerManager>(),
            settings.Object,
            new StaticHttpClientFactory(),
            new ConfigurationBuilder().Build());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcTransaccionesAsync("<Proc_Transacciones/>") );

        Assert.Contains("Live", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcTransaccionesAsync_ControlledLocalBridge_PreservesLogicalHostHeader()
    {
        using var server = await LocalSoapServer.StartAsync(
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var logicalEndpoint = new UriBuilder(Uri.UriSchemeHttp, "localhost", server.Port, "WSCFAACH.svc").Uri.AbsoluteUri;
        var settings = Settings(logicalEndpoint, "Proc_Transacciones");
        var logicalUri = new Uri(logicalEndpoint);
        var sut = new WscfaachSoapClient(
            Mock.Of<ILoggerManager>(),
            settings.Object,
            new StaticHttpClientFactory(),
            new ConfigurationBuilder().Build(),
            Options.Create(new ControlledLocalSoapTransportOptions
            {
                TransportHost = "127.0.0.1",
                HostHeader = $"localhost:{logicalUri.Port}"
            }));

        await sut.ProcTransaccionesAsync("<Proc_Transacciones/>");

        var request = Assert.Single(server.Requests);
        Assert.Equal($"localhost:{logicalUri.Port}", request.Host);
        Assert.Equal("/WSCFAACH.svc", request.Path);
    }

    private static WscfaachSoapClient BuildClient(string endpoint)
    {
        var settings = Settings(endpoint, "Proc_Transacciones");

        return new WscfaachSoapClient(
            Mock.Of<ILoggerManager>(),
            settings.Object,
            new StaticHttpClientFactory(),
            new ConfigurationBuilder().Build());
    }

    private static Mock<ISoapIntegrationSettingsService> Settings(
        string endpoint,
        string methodName,
        string operatingMode = "Live")
    {
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto
        {
            WscfaachMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = methodName,
                    Enabled = true,
                    Endpoint = endpoint,
                    SoapAction = $"http://tempuri.org/IWSCFAACH/{methodName}",
                    OperatingMode = operatingMode,
                    TimeoutSeconds = 15
                }
            ]
        });
        return settings;
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

}
