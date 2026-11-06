using System.Net;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External.Connections;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class SoapEnvelopeJournalTests
{
    private const string RequestXml = """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Header><Authorization token="SECRET-TOKEN">PASSWORD-VALUE</Authorization></soap:Header>
          <soap:Body>
            <Proc_Transacciones><Cuenta>123456789</Cuenta><Valor>900000</Valor>
              <ANSST>OK</ANSST><x:Extra xmlns:x="urn:example" key="SECRET-ATTRIBUTE">ABC</x:Extra>
            </Proc_Transacciones>
          </soap:Body>
        </soap:Envelope>
        """;

    [Fact]
    public void Journal_Request_PreservesExactEnvelopeIncludingAttributesValuesAndFormatting()
    {
        var logger = new CollectingLogger();
        var journal = BuildJournal(logger, true);
        var exactXml = RequestXml.Replace("\n", "\r\n", StringComparison.Ordinal)
            + "\r\n<!-- End of original envelope -->";

        journal.Request("Proc_Transacciones", "test-raw-request", exactXml);

        Assert.Single(logger.Messages);
        Assert.Contains("direction=REQUEST", logger.Messages[0]);
        Assert.Equal(exactXml, ExtractEnvelope(logger.Messages[0]));
        Assert.Contains("SECRET-TOKEN", logger.Messages[0]);
        Assert.Contains("SECRET-ATTRIBUTE", logger.Messages[0]);
        Assert.Contains("PASSWORD-VALUE", logger.Messages[0]);
        Assert.Contains("123456789", logger.Messages[0]);
        Assert.Contains("900000", logger.Messages[0]);
    }

    [Fact]
    public void Journal_Response_PreservesExactSoapFaultAndCorrelationId()
    {
        const string fault = """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body><s:Fault><faultcode>s:Server</faultcode>
              <faultstring>account 1234 rejected - SECRET</faultstring></s:Fault></s:Body>
            </s:Envelope>
            """;
        var logger = new CollectingLogger();
        var journal = BuildJournal(logger, true);
        const string id = "test-correlation-1";

        journal.Request("Proc_Transacciones", id, RequestXml);
        journal.Response("Proc_Transacciones", id, 500, 42, fault);

        Assert.Equal(2, logger.Messages.Count);
        Assert.Contains("direction=REQUEST", logger.Messages[0]);
        Assert.Contains("direction=RESPONSE", logger.Messages[1]);
        Assert.All(logger.Messages, m => Assert.Contains(id, m));
        Assert.Contains("soapFault=True", logger.Messages[1]);
        Assert.Equal(RequestXml, ExtractEnvelope(logger.Messages[0]));
        Assert.Equal(fault, ExtractEnvelope(logger.Messages[1]));
    }

    [Theory]
    [InlineData("<!DOCTYPE a [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><a>&x;</a>")]
    [InlineData("<not-closed")]
    public void Journal_MalformedOrDtdXml_IsStillLoggedVerbatim(string xml)
    {
        var logger = new CollectingLogger();
        var journal = BuildJournal(logger, true);

        journal.Response("Proc_Contrapartidas", "malformed", 500, 7, xml);

        Assert.Single(logger.Messages);
        Assert.Equal(xml, ExtractEnvelope(logger.Messages[0]));
        Assert.Contains("soapFault=False", logger.Messages[0]);
    }

    [Fact]
    public void Journal_LargeEnvelope_IsNotTruncatedOrSuppressed()
    {
        var logger = new CollectingLogger();
        var journal = BuildJournal(logger, true);
        var xml = "<Envelope>" + new string('S', 200000) + "</Envelope>";

        journal.Request("Proc_Transacciones", "large-message", xml);

        Assert.Single(logger.Messages);
        Assert.Equal(xml, ExtractEnvelope(logger.Messages[0]));
        Assert.Contains("payloadChars=" + xml.Length, logger.Messages[0]);
    }

    [Fact]
    public void Journal_Disabled_DoesNotEmitAnything()
    {
        var logger = new CollectingLogger();
        var journal = BuildJournal(logger, false);

        journal.Request("Proc_Contrapartidas", "abc", RequestXml);
        journal.Response("Proc_Contrapartidas", "abc", 200, 11, RequestXml);
        journal.Failure("Proc_Contrapartidas", "abc", "TIMEOUT", 11);

        Assert.Empty(logger.Messages);
    }

    [Fact]
    public void Journal_TransportFailure_DoesNotFabricateResponseEnvelope()
    {
        var logger = new CollectingLogger();
        var journal = BuildJournal(logger, true);
        journal.Failure("RegistrarRespuestaTransaccion", "unique-id", "TIMEOUT", 5000);
        Assert.Single(logger.Messages);
        Assert.Contains("direction=NO_RESPONSE", logger.Messages[0]);
        Assert.Contains("reason=TIMEOUT", logger.Messages[0]);
    }

    [Fact]
    public async Task ProcContrapartidas_Http500_LogsRawRequestAndFaultResponse()
    {
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("""
                    <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                      <s:Body><s:Fault><faultstring>SECRET-RESPONSE</faultstring></s:Fault></s:Body>
                    </s:Envelope>
                    """)
            });
        var logger = new CollectingLogger();
        var journal = BuildJournal(logger, true);
        var endpoint = new UriBuilder(Uri.UriSchemeHttp, "127.0.0.1", server.Port, "WSCFAACH.svc").Uri.AbsoluteUri;
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto
        {
            WscfaachMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Contrapartidas", Enabled = true,
                    Endpoint = endpoint, SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
                    OperatingMode = "Live", TimeoutSeconds = 15
                }
            ]
        });
        var sut = new WscfaachSoapClient(Mock.Of<ILoggerManager>(), settings.Object,
            new StaticHttpClientFactory(), new ConfigurationBuilder().Build(), envelopeJournal: journal);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcContrapartidasAsync("<Proc_Contrapartidas><OFCTA>SECRET-ACCOUNT</OFCTA></Proc_Contrapartidas>"));

        Assert.Equal(2, logger.Messages.Count);
        Assert.Contains("direction=REQUEST", logger.Messages[0]);
        Assert.Contains("direction=RESPONSE", logger.Messages[1]);
        Assert.Contains("httpStatus=500", logger.Messages[1]);
        Assert.Contains("soapFault=True", logger.Messages[1]);
        Assert.Contains("<OFCTA>SECRET-ACCOUNT</OFCTA>", ExtractEnvelope(logger.Messages[0]));
        Assert.Contains("<faultstring>SECRET-RESPONSE</faultstring>", ExtractEnvelope(logger.Messages[1]));
        var ids = logger.Messages.Select(m => Regex.Match(m, "correlationId=([a-f0-9]{32})").Groups[1].Value).ToArray();
        Assert.Equal(ids[0], ids[1]);
        Assert.NotEmpty(ids[0]);
    }

    [Fact]
    public async Task ProcTransacciones_Success_LogsExactResponseAndRequestValues()
    {
        const string responseXml = """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body><Proc_TransaccionesResponse note="ATTR-ORIGINAL">
              <Resultado>VALOR-EXACTO-123</Resultado></Proc_TransaccionesResponse></s:Body>
            </s:Envelope>
            """;
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseXml) });
        var endpoint = new UriBuilder(Uri.UriSchemeHttp, "127.0.0.1", server.Port, "WSCFAACH.svc").Uri.AbsoluteUri;
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto
        {
            WscfaachMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Transacciones", Enabled = true,
                    Endpoint = endpoint, SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
                    OperatingMode = "Live", TimeoutSeconds = 15
                }
            ]
        });
        var logger = new CollectingLogger();
        var sut = new WscfaachSoapClient(Mock.Of<ILoggerManager>(), settings.Object,
            new StaticHttpClientFactory(), new ConfigurationBuilder().Build(),
            envelopeJournal: BuildJournal(logger, true));

        var response = await sut.ProcTransaccionesAsync(
            "<Proc_Transacciones><Cuenta>123456789</Cuenta></Proc_Transacciones>");

        Assert.Equal(responseXml, response);
        Assert.Equal(2, logger.Messages.Count);
        Assert.Contains("<Cuenta>123456789</Cuenta>", ExtractEnvelope(logger.Messages[0]));
        Assert.Equal(responseXml, ExtractEnvelope(logger.Messages[1]));
        Assert.Contains("httpStatus=200", logger.Messages[1]);
    }

    [Fact]
    public async Task RespuestasAxon_Success_LogsRequestAndResponseWithoutChangingReturnedValue()
    {
        const string responseXml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                <soap:Body><RegistrarRespuestaTransaccionResponse><Resultado>SECRET-VALUE</Resultado></RegistrarRespuestaTransaccionResponse></soap:Body>
            </soap:Envelope>
            """;
        using var server = await LocalSoapServer.StartAsync((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseXml) });
        var endpoint = new UriBuilder(Uri.UriSchemeHttp, "127.0.0.1", server.Port,
            "WSAxonRespuestaTransacciones.svc").Uri.AbsoluteUri;
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SoapIntegrationSettingsDto
        {
            WsAxonRespuestaTransaccionesMappings =
            [
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "RegistrarRespuestaTransaccion", Enabled = true,
                    Endpoint = endpoint,
                    SoapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
                    OperatingMode = "Live", TimeoutSeconds = 15
                }
            ]
        });
        var logger = new CollectingLogger();
        var journal = BuildJournal(logger, true);
        var sut = new WsAxonRespuestaTransaccionesSoapClient(
            Mock.Of<ILoggerManager>(), settings.Object,
            Options.Create(new WsAxonEndpointSecurityOptions
            {
                Mode = WsAxonEndpointSecurityMode.ControlledLocal,
                AllowedPorts = [server.Port]
            }), new StaticHttpClientFactory(), envelopeJournal: journal);

        var response = await sut.RegistrarRespuestaTransaccionAsync(
            "<RegistrarRespuestaTransaccion><Cuenta>123456789</Cuenta></RegistrarRespuestaTransaccion>");

        Assert.Equal(responseXml, response);
        Assert.Equal(2, logger.Messages.Count);
        Assert.Contains("direction=REQUEST", logger.Messages[0]);
        Assert.Contains("direction=RESPONSE", logger.Messages[1]);
        Assert.Contains("httpStatus=200", logger.Messages[1]);
        Assert.Contains("<Cuenta>123456789</Cuenta>", ExtractEnvelope(logger.Messages[0]));
        Assert.Equal(responseXml, ExtractEnvelope(logger.Messages[1]));
    }

    private static string ExtractEnvelope(string message)
    {
        const string marker = " envelope=";
        var markerIndex = message.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "SOAP journal entry does not contain the envelope marker.");
        return message[(markerIndex + marker.Length)..];
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private static SoapEnvelopeJournal BuildJournal(CollectingLogger logger, bool enabled)
    {
        var monitor = new Mock<IOptionsMonitor<SoapEnvelopeLoggingOptions>>();
        monitor.SetupGet(x => x.CurrentValue).Returns(new SoapEnvelopeLoggingOptions
        {
            Enabled = enabled
        });
        return new SoapEnvelopeJournal(logger, monitor.Object);
    }

    private sealed class CollectingLogger : ILogger<SoapEnvelopeJournal>
    {
        public List<string> Messages { get; } = [];
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }
}
