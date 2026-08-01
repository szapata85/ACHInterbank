using System.Net;
using System.Text;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.External.Connections;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.Extensions.Options;
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

        var sut = BuildClient(Endpoint(server), out _);
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

        var sut = BuildClient(Endpoint(server), out _);
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
        var sut = BuildClient(Endpoint(server), out _, soapAction);

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

        var sut = BuildClient(Endpoint(server), out _);

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

    [Fact]
    public async Task RegistrarRespuestaTransaccionAsync_WhenDatabaseModeIsNotLive_StopsBeforeNetwork()
    {
        var sut = BuildClientWithCustomMapping(new SoapEndpointMethodMappingDto
        {
            MethodName = "RegistrarRespuestaTransaccion",
            Enabled = true,
            OperatingMode = "DryRun",
            TimeoutSeconds = 15,
            Endpoint = "http://127.0.0.1:1/WSAxonRespuestaTransacciones.svc",
            SoapAction = "act"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Contains("Live", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ControlledLocal_AllowsExpectedLocalService()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var sut = BuildClient(Endpoint(server), out _);

        await sut.RegistrarRespuestaTransaccionAsync("<x/>");

        Assert.Single(server.Requests);
    }

    [Fact]
    public async Task ControlledLocal_RejectsExternalHostBeforeNetwork()
    {
        var endpoint = "http://external.example.test:7083/WSAxonRespuestaTransacciones.svc";
        var sut = BuildClient(endpoint, out _);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Contains("ControlledLocal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ControlledLocal_AppliesConfiguredLocalHostHeader()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var endpoint = new Uri(Endpoint(server));
        var policy = ControlledLocalPolicy(Endpoint(server));
        policy.HostHeader = $"127.0.0.1:{endpoint.Port}";
        var sut = BuildClient(Endpoint(server), out _, policy: policy);

        await sut.RegistrarRespuestaTransaccionAsync("<x/>");

        Assert.Equal(policy.HostHeader, server.Requests.Single().Host);
    }

    [Fact]
    public async Task ControlledLocal_BridgePreservesPersistedLogicalHostHeaderAndPath()
    {
        using var server = await LocalSoapServer.StartAsync(
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var logicalEndpoint = new UriBuilder(
            Uri.UriSchemeHttp,
            "localhost",
            server.Port,
            "WSAxonRespuestaTransacciones.svc").Uri;
        var transport = new ControlledLocalSoapTransportOptions
        {
            TransportHost = "127.0.0.1",
            HostHeader = $"localhost:{logicalEndpoint.Port}"
        };
        var sut = BuildClient(
            logicalEndpoint.AbsoluteUri,
            out _,
            policy: ControlledLocalPolicy(logicalEndpoint.AbsoluteUri),
            transport: transport);

        await sut.RegistrarRespuestaTransaccionAsync("<x/>");

        var request = Assert.Single(server.Requests);
        Assert.Equal(transport.HostHeader, request.Host);
        Assert.Equal("/WSAxonRespuestaTransacciones.svc", request.Path);
    }

    [Fact]
    public async Task ConfiguredAllowlist_RejectsHostHeaderOverrideBeforeNetwork()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<unexpected/>") });
        var endpoint = new Uri(Endpoint(server));
        var policy = ConfiguredPolicy(
            schemes: ["http"],
            hosts: ["127.0.0.1"],
            ports: [endpoint.Port],
            paths: ["/WSAxonRespuestaTransacciones.svc"]);
        policy.HostHeader = $"localhost:{endpoint.Port}";
        var sut = BuildClient(Endpoint(server), out _, policy: policy);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Contains("ControlledLocal", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task ConfiguredAllowlist_AllowsExplicitHost()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ok/>") });
        var sut = BuildClient(Endpoint(server), out _, policy: ConfiguredPolicy(
            schemes: ["http"],
            hosts: ["127.0.0.1"],
            ports: [server.Port],
            paths: ["/WSAxonRespuestaTransacciones.svc"]));

        await sut.RegistrarRespuestaTransaccionAsync("<x/>");

        Assert.Single(server.Requests);
    }

    [Fact]
    public async Task ConfiguredAllowlist_RejectsUnlistedHostWithoutNetworkAttempt()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<unexpected/>") });
        var sut = BuildClient(Endpoint(server), out _, policy: ConfiguredPolicy(
            schemes: ["http"],
            hosts: ["allowed.example.test"],
            ports: [server.Port],
            paths: ["/WSAxonRespuestaTransacciones.svc"]));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task ConfiguredAllowlist_RejectsSchemeNotAllowed()
    {
        var endpoint = "https://service.example.test/WSAxonRespuestaTransacciones.svc";
        var sut = BuildClient(endpoint, out _, policy: ConfiguredPolicy(
            schemes: ["http"],
            hosts: ["service.example.test"],
            paths: ["/WSAxonRespuestaTransacciones.svc"]));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));
    }

    [Fact]
    public async Task ConfiguredAllowlist_RejectsUnexpectedPath()
    {
        var endpoint = "https://service.example.test/WSCFAACH.svc";
        var sut = BuildClient(endpoint, out _, policy: ConfiguredPolicy(
            schemes: ["https"],
            hosts: ["service.example.test"],
            paths: ["/WSAxonRespuestaTransacciones.svc"],
            requireHttps: true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));
    }

    [Fact]
    public async Task ConfiguredAllowlist_RejectsEmptyAllowlist()
    {
        var sut = BuildClient(
            "https://service.example.test/WSAxonRespuestaTransacciones.svc",
            out _,
            policy: new WsAxonEndpointSecurityOptions
            {
                Mode = WsAxonEndpointSecurityMode.ConfiguredAllowlist
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Contains("requires explicit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://user:password@service.example.test/WSAxonRespuestaTransacciones.svc", "credentials")]
    [InlineData("http://service.example.test/WSAxonRespuestaTransacciones.svc#fragment", "fragment")]
    public async Task ConfiguredAllowlist_RejectsUnsafeUriComponents(string endpoint, string expectedDiagnostic)
    {
        var sut = BuildClient(endpoint, out _, policy: ConfiguredPolicy(
            schemes: ["http"],
            hosts: ["service.example.test"],
            paths: ["/WSAxonRespuestaTransacciones.svc"]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Contains(expectedDiagnostic, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfiguredAllowlist_RejectsUnsafeWildcard()
    {
        var sut = BuildClient(
            "https://service.example.test/WSAxonRespuestaTransacciones.svc",
            out _,
            policy: ConfiguredPolicy(
                schemes: ["https"],
                hosts: ["*.example.test"],
                paths: ["/WSAxonRespuestaTransacciones.svc"]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Contains("unsafe SOAP host", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnconfiguredPolicy_RejectsByDefault()
    {
        var sut = BuildClient(
            "http://127.0.0.1:7083/WSAxonRespuestaTransacciones.svc",
            out _,
            policy: new WsAxonEndpointSecurityOptions());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RegistrarRespuestaTransaccionAsync("<x/>"));

        Assert.Contains("not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string Endpoint(LocalSoapServer server)
        => new UriBuilder(
            Uri.UriSchemeHttp,
            IPAddress.Loopback.ToString(),
            server.Port,
            "WSAxonRespuestaTransacciones.svc").Uri.AbsoluteUri;

    private static WsAxonRespuestaTransaccionesSoapClient BuildClient(
        string endpoint,
        out Mock<ISoapIntegrationSettingsService> settingsMock,
        string? soapAction = null,
        WsAxonEndpointSecurityOptions? policy = null,
        ControlledLocalSoapTransportOptions? transport = null)
    {
        var mapping = new SoapEndpointMethodMappingDto
        {
            MethodName = "RegistrarRespuestaTransaccion",
            Enabled = true,
            Endpoint = endpoint,
            SoapAction = soapAction ?? "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
            OperatingMode = "Live",
            TimeoutSeconds = 15
        };
        policy ??= ControlledLocalPolicy(endpoint);
        return BuildClientWithMockedSettings(mapping, policy, out settingsMock, transport);
    }

    private static WsAxonRespuestaTransaccionesSoapClient BuildClientWithoutMapping()
    {
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto());
        return new WsAxonRespuestaTransaccionesSoapClient(
            Mock.Of<ILoggerManager>(),
            settings.Object,
            Options.Create(new WsAxonEndpointSecurityOptions()));
    }

    private static WsAxonRespuestaTransaccionesSoapClient BuildClientWithCustomMapping(SoapEndpointMethodMappingDto mapping)
    {
        mapping = mapping with
        {
            OperatingMode = string.IsNullOrWhiteSpace(mapping.OperatingMode) ? "Live" : mapping.OperatingMode,
            TimeoutSeconds = mapping.TimeoutSeconds <= 0 ? 15 : mapping.TimeoutSeconds
        };
        return BuildClientWithMockedSettings(
            mapping,
            new WsAxonEndpointSecurityOptions(),
            out _);
    }

    private static WsAxonRespuestaTransaccionesSoapClient BuildClientWithMockedSettings(
        SoapEndpointMethodMappingDto mapping,
        WsAxonEndpointSecurityOptions policy,
        out Mock<ISoapIntegrationSettingsService> settings,
        ControlledLocalSoapTransportOptions? transport = null)
    {
        settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto
        {
            WsAxonRespuestaTransaccionesMappings = [mapping]
        });
        return new WsAxonRespuestaTransaccionesSoapClient(
            Mock.Of<ILoggerManager>(),
            settings.Object,
            Options.Create(policy),
            Options.Create(transport ?? new ControlledLocalSoapTransportOptions()));
    }

    private static WsAxonEndpointSecurityOptions ControlledLocalPolicy(string endpoint)
        => new()
        {
            Mode = WsAxonEndpointSecurityMode.ControlledLocal,
            AllowedPorts = [new Uri(endpoint).Port]
        };

    private static WsAxonEndpointSecurityOptions ConfiguredPolicy(
        IEnumerable<string> schemes,
        IEnumerable<string> hosts,
        IEnumerable<string> paths,
        IEnumerable<int>? ports = null,
        bool requireHttps = false)
        => new()
        {
            Mode = WsAxonEndpointSecurityMode.ConfiguredAllowlist,
            AllowedSchemes = [.. schemes],
            AllowedHosts = [.. hosts],
            AllowedPorts = ports is null ? [] : [.. ports],
            AllowedPaths = [.. paths],
            RequireHttps = requireHttps
        };

}
