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
    public void AddExternal_WithNoRetrySoapPolicy_CreatesSoapHttpClientWithoutValidationFailure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:ExternalHttp:Soap:MaxRetryAttempts"] = "0",
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
            .CreateClient(ExternalHttpClientNames.WscfaachSoapNoRetry);

        Assert.NotNull(client);
    }

    [Fact]
    public async Task ProcTransaccionesAsync_WithExistingEnvelope_SendsSingleOutboundEnvelopeWithoutOtherSoapMethods()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><tem:Proc_TransaccionesResponse xmlns:tem=\"http://tempuri.org/\"><tem:RTAACH>R96</tem:RTAACH><tem:RTALOC>OK</tem:RTALOC></tem:Proc_TransaccionesResponse></s:Body></s:Envelope>")
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
    public async Task Npt14_ProcTransacciones_WhenSoapActionDiffersFromWsdl_FailsBeforeTransport()
    {
        var configured = new Mock<ISoapIntegrationSettingsService>();
        configured.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto
        {
            WscfaachMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Transacciones",
                    Enabled = true,
                    OperatingMode = "Live",
                    Endpoint = "http://127.0.0.1:1/WSCFAACH.svc",
                    SoapAction = "http://tempuri.org/IWSCFAACH/WRONG_ACTION",
                    TimeoutSeconds = 15
                }
            ]
        });
        var sut = new WscfaachSoapClient(Mock.Of<ILoggerManager>(), configured.Object,
            new StaticHttpClientFactory(), new ConfigurationBuilder().Build());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcTransaccionesAsync("<Proc_Transacciones xmlns='http://tempuri.org/'/>"));
        Assert.Contains("SOAPACTION_XSD_MISMATCH", ex.Message);
    }

    [Fact]
    public async Task Npt06_HttpGatewayTimeout_DoesNotDuplicatePhysicalPost()
    {
        // Deterministic local HTTP server counts physical requests. This is NOT a bank test.
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
            {
                Content = new StringContent("<fault>synthetic timeout</fault>")
            });
        var sut = BuildClient(server.Url);
        const string envelope = "<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'><s:Body><t:Proc_Transacciones xmlns:t='http://tempuri.org/' /></s:Body></s:Envelope>";
        try
        {
            await sut.ProcTransaccionesAsync(envelope);
        }
        catch (InvalidOperationException)
        {
            // HTTP 504 is classified as uncertain; no automatic replay.
        }
        Assert.Single(server.Requests);
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

    [Theory]
    [InlineData("<Proc_Transacciones/>")]
    [InlineData("<Proc_Transacciones xmlns='http://tempuri.org/'><TIPTRAN>not-int</TIPTRAN></Proc_Transacciones>")]
    [InlineData("<Proc_Transacciones xmlns='http://tempuri.org/'><TIPTRAN>22</TIPTRAN><TIPTRAN>22</TIPTRAN></Proc_Transacciones>")]
    [InlineData("<Proc_Transacciones xmlns='http://tempuri.org/'><IDTRAN>bad-long</IDTRAN></Proc_Transacciones>")]
    [InlineData("<Proc_Transacciones xmlns='http://tempuri.org/'><unknown>value</unknown></Proc_Transacciones>")]
    [InlineData("<s:Envelope xmlns:s='http://www.w3.org/2003/05/soap-envelope'><s:Body><Proc_Transacciones xmlns='http://tempuri.org/'/></s:Body></s:Envelope>")]
    [InlineData("<!DOCTYPE doc [<!ENTITY e SYSTEM 'file:///etc/passwd'>]><Proc_Transacciones xmlns='http://tempuri.org/'> &e; </Proc_Transacciones>")]
    public async Task NptH01_Cfa_PublicXmlOverload_RejectsInvalidBodyBeforeHttp(string invalidBody)
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var sut = BuildClient(server.Url);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ProcTransaccionesAsync(invalidBody));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task NptH01_Cfa_ParameterOverload_RejectsInvalidTypeBeforeHttp()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var sut = BuildClient(server.Url);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ProcTransaccionesAsync(
            new Dictionary<string, object?> { ["TIPTRAN"] = "not-int" }));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task ProcContrapartidasAsync_BlocksRawXmlBeforePost()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var sut = new WscfaachSoapClient(
            Mock.Of<ILoggerManager>(), Settings(server.Url, "Proc_Contrapartidas").Object,
            new StaticHttpClientFactory(), new ConfigurationBuilder().Build());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcContrapartidasAsync("<Proc_Contrapartidas xmlns=\"http://tempuri.org/\"><OFIDTX>1</OFIDTX></Proc_Contrapartidas>"));
        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task ProcContrapartidasAsync_RejectsInvalidXmlBeforePost()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var sut = new WscfaachSoapClient(
            Mock.Of<ILoggerManager>(), Settings(server.Url, "Proc_Contrapartidas").Object,
            new StaticHttpClientFactory(), new ConfigurationBuilder().Build());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcContrapartidasAsync("<Proc_Contrapartidas xmlns=\"http://tempuri.org/\"><OFIDARCH>NaN</OFIDARCH></Proc_Contrapartidas>"));
        Assert.Contains("SOAP_XSD", error.Message);
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task ProcContrapartidasBlockedBeforePost_NeverExposesResponseBody()
    {
        const string sensitiveResponse = "SENSITIVE-FINANCIAL-PAYLOAD";
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(sensitiveResponse) });
        var sut = new WscfaachSoapClient(
            Mock.Of<ILoggerManager>(), Settings(server.Url, "Proc_Contrapartidas").Object,
            new StaticHttpClientFactory(), new ConfigurationBuilder().Build());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcContrapartidasAsync("<Proc_Contrapartidas xmlns=\"http://tempuri.org/\"/>"));
        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
        Assert.DoesNotContain(sensitiveResponse, error.Message);
        Assert.Empty(server.Requests);
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

        await sut.ProcTransaccionesAsync("<Proc_Transacciones xmlns=\"http://tempuri.org/\"/>");

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
