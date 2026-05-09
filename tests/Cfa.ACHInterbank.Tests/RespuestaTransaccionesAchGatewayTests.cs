using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class RespuestaTransaccionesAchGatewayTests
{
    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldMapCommandToPhysicalRequest()
    {
        var soapClient = new Mock<IWsAxonRespuestaTransaccionesSoapClient>();
        IReadOnlyDictionary<string, object?>? captured = null;

        soapClient
            .Setup(x => x.RegistrarRespuestaTransaccionAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyDictionary<string, object?>, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(SuccessResponseXml());

        var sut = new RespuestaTransaccionesAchGateway(soapClient.Object, new RegistrarRespuestaAchSoapRequestMapper(), new RegistrarRespuestaAchSoapResponseParser());

        await sut.RegistrarRespuestaAsync(BuildCommand());

        captured.Should().NotBeNull();
        captured!["idCanal"].Should().Be(10);
        captured["nombreCanal"].Should().Be("ACH");
        captured["idTransaccion"].Should().Be("TX-123");
        captured["idEstado"].Should().Be(2);
        captured["causal"].Should().Be("R01");
        captured["idTransaccionAxon"].Should().Be(555);
        captured["descripcionCausal"].Should().Be("Cuenta cerrada");
    }

    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldReturnSuccess_WhenSoapResponseExisteErrorFalse()
    {
        var sut = BuildGatewayReturning(SuccessResponseXml());

        var result = await sut.RegistrarRespuestaAsync(BuildCommand());

        result.ExisteError.Should().BeFalse();
        result.Exitoso.Should().BeTrue();
    }

    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldReturnFunctionalError_WhenSoapResponseExisteErrorTrue()
    {
        var sut = BuildGatewayReturning("""
            <s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">
              <s:Body>
                <RegistrarRespuestaTransaccionResponse xmlns=\"http://tempuri.org/\">
                  <RegistrarRespuestaTransaccionResult xmlns:a=\"http://schemas.datacontract.org/2004/07/WSCFAACH\">
                    <a:codigoError>E01</a:codigoError>
                    <a:descripcionError>Error funcional</a:descripcionError>
                    <a:existeError>true</a:existeError>
                  </RegistrarRespuestaTransaccionResult>
                </RegistrarRespuestaTransaccionResponse>
              </s:Body>
            </s:Envelope>
            """);

        var result = await sut.RegistrarRespuestaAsync(BuildCommand());

        result.ExisteError.Should().BeTrue();
        result.CodigoError.Should().Be("E01");
        result.DescripcionError.Should().Be("Error funcional");
    }

    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldThrowTechnicalException_WhenSoapResponseMalformed()
    {
        var sut = BuildGatewayReturning("<not-valid-xml");

        var action = () => sut.RegistrarRespuestaAsync(BuildCommand());

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldThrowTechnicalException_WhenSoapResponseHasNoResult()
    {
        var sut = BuildGatewayReturning("<Envelope><Body><NoResult/></Body></Envelope>");

        var action = () => sut.RegistrarRespuestaAsync(BuildCommand());

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void RegistrarRespuestaAchSoapResponseParser_ShouldParseNamespacedResponse()
    {
        var parser = new RegistrarRespuestaAchSoapResponseParser();
        var xml = """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/" xmlns:t="http://tempuri.org/" xmlns:a="http://schemas.datacontract.org/2004/07/WSCFAACH">
              <s:Body>
                <t:RegistrarRespuestaTransaccionResponse>
                  <t:RegistrarRespuestaTransaccionResult>
                    <a:codigoError></a:codigoError>
                    <a:descripcionError></a:descripcionError>
                    <a:existeError>false</a:existeError>
                  </t:RegistrarRespuestaTransaccionResult>
                </t:RegistrarRespuestaTransaccionResponse>
              </s:Body>
            </s:Envelope>
            """;

        var result = parser.Parse(xml);

        result.ExisteError.Should().BeFalse();
        result.Exitoso.Should().BeTrue();
    }


    [Fact]
    public void ExternalGatewayPhysicalComponents_ShouldRemainInternal()
    {
        typeof(RegistrarRespuestaAchSoapRequestMapper).IsPublic.Should().BeFalse();
        typeof(RegistrarRespuestaAchSoapResponseParser).IsPublic.Should().BeFalse();
    }

    [Fact]
    public void ExternalPhysicalMapper_ShouldContainPhysicalSoapFieldName_IdTransaccionAxon()
    {
        var mapper = new RegistrarRespuestaAchSoapRequestMapper();
        var map = mapper.Map(BuildCommand());

        map.ContainsKey("idTransaccionAxon").Should().BeTrue();
        map["idTransaccionAxon"].Should().Be(555);
    }

    [Fact]
    public void RespuestaTransaccionesAchGateway_ShouldNotExposePhysicalNamesInApplicationTypes()
    {
        typeof(RegistrarRespuestaAchCommand).GetProperties().Select(x => x.Name)
            .Should().NotContain(x => x.Contains("Axon", StringComparison.OrdinalIgnoreCase));

        typeof(ResultadoRegistroRespuestaAch).GetProperties().Select(x => x.Name)
            .Should().NotContain(x => x.Contains("RegistrarRespuestaTransaccion", StringComparison.OrdinalIgnoreCase));
    }

    private static RespuestaTransaccionesAchGateway BuildGatewayReturning(string xml)
    {
        var soapClient = new Mock<IWsAxonRespuestaTransaccionesSoapClient>();
        soapClient.Setup(x => x.RegistrarRespuestaTransaccionAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);

        return new RespuestaTransaccionesAchGateway(soapClient.Object, new RegistrarRespuestaAchSoapRequestMapper(), new RegistrarRespuestaAchSoapResponseParser());
    }

    private static RegistrarRespuestaAchCommand BuildCommand() => new(
        TipoRespuestaAch.Transaccion,
        "TX-123",
        10,
        "ACH",
        2,
        "R01",
        555,
        "Cuenta cerrada",
        "01",
        "1001",
        "2002",
        "corr-1");

    private static string SuccessResponseXml() => """
        <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
          <s:Body>
            <RegistrarRespuestaTransaccionResponse xmlns="http://tempuri.org/">
              <RegistrarRespuestaTransaccionResult xmlns:a="http://schemas.datacontract.org/2004/07/WSCFAACH">
                <a:codigoError></a:codigoError>
                <a:descripcionError></a:descripcionError>
                <a:existeError>false</a:existeError>
              </RegistrarRespuestaTransaccionResult>
            </RegistrarRespuestaTransaccionResponse>
          </s:Body>
        </s:Envelope>
        """;
}
