using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Integrations.Services;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;
using Microsoft.Extensions.Options;
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

        var sut = new RespuestaTransaccionesAchGateway(soapClient.Object);

        await sut.RegistrarRespuestaAsync(BuildCommand());

        captured.Should().NotBeNull();
        captured!["idCanal"].Should().Be(10);
        captured["nombreCanal"].Should().Be("ACH");
        captured["idTransaccion"].Should().Be("TX-123");
        captured["idEstado"].Should().Be(2);
        captured["causal"].Should().Be("R01");
        captured["idTransaccionAxon"].Should().Be(555);
        captured["descripcionCausal"].Should().Be("Cuenta cerrada");
        soapClient.Verify(
            x => x.RegistrarRespuestaTransaccionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldUseContractDrivenBaselineV1_WhenFeatureFlagEnabled()
    {
        var responseId = Guid.NewGuid();
        var command = BuildCommand() with { DifferentialResponseId = responseId };
        var resolvedRequest = BuildBaselineResolvedRequest(command);
        IntegrationExecutionContext? capturedContext = null;
        string? capturedOperation = null;

        var resolver = new Mock<IIntegrationRequestResolver>();
        resolver
            .Setup(x => x.ResolveAsync(It.IsAny<IntegrationExecutionContext>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationExecutionContext, CancellationToken>((context, _) => capturedContext = context)
            .ReturnsAsync(resolvedRequest);

        var soapClient = new Mock<IWsAxonRespuestaTransaccionesSoapClient>();
        soapClient
            .Setup(x => x.RegistrarRespuestaTransaccionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((xml, _) => capturedOperation = xml)
            .ReturnsAsync(SuccessResponseXml());

        var sut = new RespuestaTransaccionesAchGateway(
            soapClient.Object,
            Options.Create(new RegistrarRespuestaDispatchOptions { UseContractDrivenRequest = true }),
            resolver.Object,
            new ContractDrivenSoapBodyWriter());

        var result = await sut.RegistrarRespuestaAsync(command);

        result.Exitoso.Should().BeTrue();
        capturedContext.Should().NotBeNull();
        capturedContext!.MethodCode.Should().Be("WSAXON.RegistrarRespuestaTransaccion");
        capturedContext.DifferentialResponseId.Should().Be(responseId);

        capturedOperation.Should().NotBeNullOrWhiteSpace();
        var operation = XDocument.Parse(capturedOperation!).Root!;
        operation.Name.LocalName.Should().Be("RegistrarRespuestaTransaccion");
        operation.Name.NamespaceName.Should().Be("http://tempuri.org/");
        operation.Elements().Select(element => element.Name.LocalName).Should().Equal(
            "idCanal",
            "nombreCanal",
            "idTransaccion",
            "idEstado",
            "causal",
            "idTransaccionAxon",
            "descripcionCausal");
        operation.Elements().Select(element => element.Value).Should().Equal(
            "10",
            "ACH",
            "TX-123",
            "2",
            "R01",
            "555",
            "Cuenta cerrada");

        soapClient.Verify(
            x => x.RegistrarRespuestaTransaccionAsync(
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldKeepSevenEmptyOptionalElements_WhenContractDrivenValuesAreNull()
    {
        var command = BuildCommand(causal: null, descripcionCausal: null) with
        {
            DifferentialResponseId = Guid.NewGuid()
        };
        var resolver = new Mock<IIntegrationRequestResolver>();
        resolver
            .Setup(x => x.ResolveAsync(It.IsAny<IntegrationExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBaselineResolvedRequest(command));

        string? capturedOperation = null;
        var soapClient = new Mock<IWsAxonRespuestaTransaccionesSoapClient>();
        soapClient
            .Setup(x => x.RegistrarRespuestaTransaccionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((xml, _) => capturedOperation = xml)
            .ReturnsAsync(SuccessResponseXml());

        var sut = new RespuestaTransaccionesAchGateway(
            soapClient.Object,
            Options.Create(new RegistrarRespuestaDispatchOptions { UseContractDrivenRequest = true }),
            resolver.Object,
            new ContractDrivenSoapBodyWriter());

        await sut.RegistrarRespuestaAsync(command);

        var elements = XDocument.Parse(capturedOperation!).Root!.Elements().ToArray();
        elements.Should().HaveCount(7);
        elements[4].Name.LocalName.Should().Be("causal");
        elements[4].Value.Should().BeEmpty();
        elements[6].Name.LocalName.Should().Be("descripcionCausal");
        elements[6].Value.Should().BeEmpty();
    }

    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldRejectContractDrivenV1_WhenContractDoesNotContainExactlySevenFields()
    {
        var command = BuildCommand() with { DifferentialResponseId = Guid.NewGuid() };
        var resolver = new Mock<IIntegrationRequestResolver>();
        resolver
            .Setup(x => x.ResolveAsync(It.IsAny<IntegrationExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBaselineResolvedRequest(command, includeExtraField: true));
        var soapClient = new Mock<IWsAxonRespuestaTransaccionesSoapClient>();

        var sut = new RespuestaTransaccionesAchGateway(
            soapClient.Object,
            Options.Create(new RegistrarRespuestaDispatchOptions { UseContractDrivenRequest = true }),
            resolver.Object,
            new ContractDrivenSoapBodyWriter());

        var action = () => sut.RegistrarRespuestaAsync(command);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exactamente siete campos*");
        soapClient.Verify(
            x => x.RegistrarRespuestaTransaccionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrarRespuestaAsync_ShouldRequireDifferentialResponseIdentity_WhenContractDrivenFlagEnabled()
    {
        var resolver = new Mock<IIntegrationRequestResolver>();
        var soapClient = new Mock<IWsAxonRespuestaTransaccionesSoapClient>();
        var sut = new RespuestaTransaccionesAchGateway(
            soapClient.Object,
            Options.Create(new RegistrarRespuestaDispatchOptions { UseContractDrivenRequest = true }),
            resolver.Object,
            new ContractDrivenSoapBodyWriter());

        var action = () => sut.RegistrarRespuestaAsync(BuildCommand());

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*respuesta diferencial*");
        resolver.Verify(
            x => x.ResolveAsync(It.IsAny<IntegrationExecutionContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <RegistrarRespuestaTransaccionResponse xmlns="http://tempuri.org/">
                  <RegistrarRespuestaTransaccionResult xmlns:a="http://schemas.datacontract.org/2004/07/WSCFAACH">
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
    public void RegistrarRespuestaAchSoapResponseParser_ShouldParseFunctionalErrorResponse()
    {
        var parser = new RegistrarRespuestaAchSoapResponseParser();
        var xml = """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <RegistrarRespuestaTransaccionResponse xmlns="http://tempuri.org/">
                  <RegistrarRespuestaTransaccionResult xmlns:a="http://schemas.datacontract.org/2004/07/WSCFAACH">
                    <a:codigoError>E01</a:codigoError>
                    <a:descripcionError>Error funcional</a:descripcionError>
                    <a:existeError>true</a:existeError>
                  </RegistrarRespuestaTransaccionResult>
                </RegistrarRespuestaTransaccionResponse>
              </s:Body>
            </s:Envelope>
            """;

        var result = parser.Parse(xml);

        result.ExisteError.Should().BeTrue();
        result.CodigoError.Should().Be("E01");
        result.DescripcionError.Should().Be("Error funcional");
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

    [Fact]
    public void RespuestaTransaccionesAchGateway_ShouldNotDependOnMonetarySoapClient()
    {
        var constructorParameters = typeof(RespuestaTransaccionesAchGateway)
            .GetConstructors()
            .SelectMany(x => x.GetParameters())
            .Select(x => x.ParameterType)
            .ToArray();

        constructorParameters.Should().Contain(typeof(IWsAxonRespuestaTransaccionesSoapClient));
        constructorParameters.Should().NotContain(typeof(IWscfaachSoapClient));
    }

    private static RespuestaTransaccionesAchGateway BuildGatewayReturning(string xml)
    {
        var soapClient = new Mock<IWsAxonRespuestaTransaccionesSoapClient>();
        soapClient.Setup(x => x.RegistrarRespuestaTransaccionAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);

        return new RespuestaTransaccionesAchGateway(soapClient.Object);
    }

    private static RegistrarRespuestaAchCommand BuildCommand(
        string? causal = "R01",
        string? descripcionCausal = "Cuenta cerrada") => new(
        TipoRespuestaAch.Transaccion,
        "TX-123",
        10,
        "ACH",
        2,
        causal,
        555,
        descripcionCausal,
        "01",
        "1001",
        "2002",
        "corr-1");

    private static ResolvedIntegrationRequest BuildBaselineResolvedRequest(
        RegistrarRespuestaAchCommand command,
        bool includeExtraField = false)
    {
        var specs = new List<(string Name, string Type, bool Required, object? Value, string Source)>
        {
            ("idCanal", "int", true, command.IdCanal, "differentialResponse.idCanal"),
            ("nombreCanal", "string", true, command.NombreCanal, "differentialResponse.nombreCanal"),
            ("idTransaccion", "string", true, command.IdTransaccion, "differentialResponse.idTransaccion"),
            ("idEstado", "int", true, command.IdEstado, "differentialResponse.idEstado"),
            ("causal", "string", false, command.Causal, "differentialResponse.codigoCausalExterna"),
            ("idTransaccionAxon", "int", true, command.IdTransaccionServicioExterno, "differentialResponse.idTransaccionServicioExterno"),
            ("descripcionCausal", "string", false, command.DescripcionCausal, "differentialResponse.descripcionCausalExterna")
        };

        if (includeExtraField)
        {
            specs.Add(("futureField", "string", false, "future", "differentialResponse.descripcionCausalExterna"));
        }

        var bindings = specs
            .Select((spec, index) => new IntegrationExecutionParameterBinding(
                parameterId: index + 1,
                parameterPath: spec.Name,
                xmlElementName: spec.Name,
                dataType: spec.Type,
                xsdType: spec.Type,
                nillable: false,
                minOccurs: spec.Required ? 1 : 0,
                maxOccurs: 1,
                required: spec.Required,
                sortOrder: index + 1,
                source: new IntegrationSourceReference(
                    IntegrationSourceKindEnum.DifferentialResponse,
                    spec.Source)))
            .ToArray();

        var contract = new IntegrationExecutionContract(
            new Guid("4a093eb5-beed-5b15-bc7c-5768c0e0547e"),
            contractRevision: 1,
            methodId: 3,
            methodCode: "WSAXON.RegistrarRespuestaTransaccion",
            operationName: "RegistrarRespuestaTransaccion",
            targetNamespace: "http://tempuri.org/",
            soapAction: "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
            mappingSetId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            mappingVersion: 1,
            mappingRevision: 1,
            parameters: bindings);

        var resolved = bindings
            .Select((binding, index) => new ResolvedIntegrationParameter(
                binding.ParameterId,
                binding.ParameterPath,
                binding.XmlElementName,
                binding.DataType,
                binding.XsdType,
                binding.Nillable,
                binding.MinOccurs,
                binding.MaxOccurs,
                binding.Required,
                binding.SortOrder,
                specs[index].Value,
                binding.Direction,
                isPresent: true))
            .ToArray();

        return new ResolvedIntegrationRequest(contract, command.CorrelationId ?? "corr-test", resolved);
    }

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
