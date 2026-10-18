using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Integrations.Services;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.External.Connections;
using Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractDrivenSoapBodyWriterTests
{
    private const string TempUri = "http://tempuri.org/";
    private static readonly XNamespace Soap11 = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private readonly ContractDrivenSoapBodyWriter _sut = new();

    [Fact]
    public void Write_ShouldBuildSoap11EnvelopeFromContractInDeterministicInputOrderAndEscapeXml()
    {
        var second = Binding(2, "SECOND", 20, required: true);
        var first = Binding(1, "FIRST", 10, required: true);
        var output = Binding(3, "OUTPUT", 5, required: false, direction: IntegrationParameterDirectionEnum.Output);
        var contract = Contract("WSCFAACH.Proc_Transacciones", "Proc_Transacciones", [second, output, first]);
        var request = Request(contract,
        [
            Resolved(second, "B"),
            Resolved(first, "A & <B>")
        ]);

        var xml = _sut.Write(request);
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var envelope = Assert.IsType<XElement>(document.Root);
        var body = Assert.Single(envelope.Elements());
        var operation = Assert.Single(body.Elements());

        Assert.Equal(Soap11 + "Envelope", envelope.Name);
        Assert.Equal(Soap11 + "Body", body.Name);
        Assert.Equal(XNamespace.Get(TempUri) + "Proc_Transacciones", operation.Name);
        Assert.Equal(["FIRST", "SECOND"], operation.Elements().Select(element => element.Name.LocalName).ToArray());
        Assert.DoesNotContain(operation.Elements(), element => element.Name.LocalName == "OUTPUT");
        Assert.Equal("A & <B>", operation.Element(XNamespace.Get(TempUri) + "FIRST")!.Value);
        Assert.Contains("A &amp; &lt;B&gt;", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ShouldRespectOmittedExplicitNullNillableAndExplicitEmptyStates()
    {
        var omitted = Binding(1, "OMITTED", 1, required: false);
        var emptyFromNull = Binding(2, "EMPTY_FROM_NULL", 2, required: false);
        var nil = Binding(3, "NIL", 3, required: false, nillable: true);
        var empty = Binding(4, "EMPTY", 4, required: false);
        var contract = Contract("WSAXON.RegistrarRespuestaTransaccion", "RegistrarRespuestaTransaccion", [omitted, emptyFromNull, nil, empty]);
        var request = Request(contract,
        [
            Resolved(omitted, null, isPresent: false),
            Resolved(emptyFromNull, null),
            Resolved(nil, null),
            Resolved(empty, string.Empty)
        ]);

        var operation = Operation(_sut.Write(request));

        Assert.Null(operation.Element(XNamespace.Get(TempUri) + "OMITTED"));
        Assert.Equal(string.Empty, operation.Element(XNamespace.Get(TempUri) + "EMPTY_FROM_NULL")!.Value);
        Assert.Equal("true", operation.Element(XNamespace.Get(TempUri) + "NIL")!.Attribute(Xsi + "nil")!.Value);
        Assert.Equal(string.Empty, operation.Element(XNamespace.Get(TempUri) + "EMPTY")!.Value);
    }

    [Fact]
    public void Write_ShouldNormalizeTypedScalarValuesAndPreservePreformattedStringLexicalValues()
    {
        var decimalBinding = Binding(1, "AMOUNT", 1, true, dataType: "decimal");
        var boolBinding = Binding(2, "FLAG", 2, true, dataType: "boolean");
        var dateBinding = Binding(3, "WHEN", 3, true, dataType: "dateTime");
        var paddedBinding = Binding(4, "PADDED", 4, true, dataType: "long");
        var contract = Contract("WSCFAACH.Proc_Transacciones", "Proc_Transacciones", [decimalBinding, boolBinding, dateBinding, paddedBinding]);
        var request = Request(contract,
        [
            Resolved(decimalBinding, 12.34m),
            Resolved(boolBinding, false),
            Resolved(dateBinding, new DateTime(2026, 10, 13, 0, 30, 0, DateTimeKind.Utc)),
            Resolved(paddedBinding, "0000001")
        ]);

        var operation = Operation(_sut.Write(request));
        var ns = XNamespace.Get(TempUri);

        Assert.Equal("12.34", operation.Element(ns + "AMOUNT")!.Value);
        Assert.Equal("false", operation.Element(ns + "FLAG")!.Value);
        Assert.Equal("2026-10-13T00:30:00Z", operation.Element(ns + "WHEN")!.Value);
        Assert.Equal("0000001", operation.Element(ns + "PADDED")!.Value);
    }

    [Theory]
    [InlineData("WSCFAACH.Proc_Transacciones", "Proc_Transacciones")]
    [InlineData("WSCFAACH.Proc_Contrapartidas", "Proc_Contrapartidas")]
    [InlineData("WSAXON.RegistrarRespuestaTransaccion", "RegistrarRespuestaTransaccion")]
    public void Write_ShouldFailWhenResolvedRequestContainsParameterOutsideContract(string methodCode, string operationName)
    {
        var allowed = Binding(1, "ALLOWED", 1, true);
        var contract = Contract(methodCode, operationName, [allowed]);
        var foreign = new ResolvedIntegrationParameter(
            999,
            "FOREIGN",
            "FOREIGN",
            "string",
            "string",
            false,
            0,
            1,
            false,
            999,
            "NO");
        var request = Request(contract, [Resolved(allowed, "YES"), foreign]);

        var error = Assert.Throws<InvalidOperationException>(() => _sut.Write(request));

        Assert.Contains("RESOLVED_REQUEST_PARAMETER_NOT_IN_CONTRACT", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SAFE></SAFE><Injected>")]
    [InlineData("SAFE xmlns:evil='urn:evil'")]
    [InlineData("SAFE attr='x'")]
    [InlineData("1INVALID") ]
    public void Write_ShouldRejectMaliciousResolvedElementNameBeforeXmlCreation(string maliciousName)
    {
        var binding = Binding(1, "SAFE", 1, true);
        var contract = Contract("WSCFAACH.Proc_Transacciones", "Proc_Transacciones", [binding]);
        var malicious = new ResolvedIntegrationParameter(
            binding.ParameterId,
            binding.ParameterPath,
            maliciousName,
            binding.DataType,
            binding.XsdType,
            binding.Nillable,
            binding.MinOccurs,
            binding.MaxOccurs,
            binding.Required,
            binding.SortOrder,
            "VALUE");

        var error = Assert.Throws<InvalidOperationException>(() => _sut.Write(Request(contract, [malicious])));

        Assert.Contains("RESOLVED_REQUEST_CONTRACT_MISMATCH", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ShouldFailWhenRequiredInputIsMissing()
    {
        var required = Binding(1, "REQUIRED", 1, true);
        var contract = Contract("WSCFAACH.Proc_Transacciones", "Proc_Transacciones", [required]);

        var error = Assert.Throws<InvalidOperationException>(() => _sut.Write(Request(contract, [])));

        Assert.Contains("RESOLVED_REQUEST_PARAMETER_MISSING", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ShouldRejectOutputParameterWhenInjectedIntoResolvedRequest()
    {
        var output = Binding(1, "RESPONSE_CODE", 1, false, direction: IntegrationParameterDirectionEnum.Output);
        var contract = Contract("WSAXON.RegistrarRespuestaTransaccion", "RegistrarRespuestaTransaccion", [output]);

        var error = Assert.Throws<InvalidOperationException>(() => _sut.Write(Request(contract, [Resolved(output, "R00")])));

        Assert.Contains("RESOLVED_REQUEST_OUTPUT_PARAMETER", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("WSCFAACH.Proc_Transacciones", "Proc_Transacciones")]
    [InlineData("WSCFAACH.Proc_Contrapartidas", "Proc_Contrapartidas")]
    [InlineData("WSAXON.RegistrarRespuestaTransaccion", "RegistrarRespuestaTransaccion")]
    public void Write_ShouldRespectContractFieldOrderForAllContractDrivenServices(string methodCode, string operationName)
    {
        var third = Binding(3, "THIRD", 30, true);
        var first = Binding(1, "FIRST", 10, true);
        var second = Binding(2, "SECOND", 20, true);
        var contract = Contract(methodCode, operationName, [third, first, second]);

        var operation = Operation(_sut.Write(Request(contract,
        [
            Resolved(third, "3"),
            Resolved(second, "2"),
            Resolved(first, "1")
        ])));

        Assert.Equal(["FIRST", "SECOND", "THIRD"], operation.Elements().Select(element => element.Name.LocalName).ToArray());
    }

    [Fact]
    public void Write_ShouldFailWhenResolvedMetadataDoesNotMatchContract()
    {
        var binding = Binding(1, "EXPECTED", 1, true);
        var contract = Contract("WSCFAACH.Proc_Transacciones", "Proc_Transacciones", [binding]);
        var inconsistent = new ResolvedIntegrationParameter(
            binding.ParameterId,
            binding.ParameterPath,
            "OTHER_ELEMENT",
            binding.DataType,
            binding.XsdType,
            binding.Nillable,
            binding.MinOccurs,
            binding.MaxOccurs,
            binding.Required,
            binding.SortOrder,
            "VALUE");

        var error = Assert.Throws<InvalidOperationException>(() => _sut.Write(Request(contract, [inconsistent])));

        Assert.Contains("RESOLVED_REQUEST_CONTRACT_MISMATCH", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcTransacciones_ShouldMatchLegacyGoldenEnvelopeForEquivalentResolvedInput()
    {
        await using var context = BuildContext();
        var legacy = new ProcTransaccionesRequestMapper(context);
        var legacyRequest = new ProcTransaccionesRequestContract(new Dictionary<string, string>
        {
            ["TREG"] = "6",
            ["TIPTRAN"] = "22",
            ["DESCRIPCION"] = "SYNTH & <VALUE>",
            ["DISCRE"] = string.Empty,
            ["IDTRAN"] = "0000001"
        });
        var treg = Binding(1, "TREG", 1, true);
        var tiptran = Binding(2, "TIPTRAN", 2, true, dataType: "int");
        var descripcion = Binding(3, "DESCRIPCION", 3, true);
        var discre = Binding(4, "DISCRE", 4, false);
        var idtran = Binding(5, "IDTRAN", 5, true, dataType: "long");
        var contract = Contract("WSCFAACH.Proc_Transacciones", "Proc_Transacciones", [treg, tiptran, descripcion, discre, idtran]);
        var genericRequest = Request(contract,
        [
            Resolved(treg, "6"),
            Resolved(tiptran, "22"),
            Resolved(descripcion, "SYNTH & <VALUE>"),
            Resolved(discre, string.Empty, isPresent: false),
            Resolved(idtran, "0000001")
        ]);

        var legacyOperation = Operation(legacy.BuildSoapBody(legacyRequest));
        var genericOperation = Operation(_sut.Write(genericRequest));

        AssertOperationEquivalent(legacyOperation, genericOperation);
    }

    [Fact]
    public async Task ProcTransaccionesParityPolicy_ShouldFreezeP0OmissionAndEscapingSemantics()
    {
        await using var context = BuildContext();
        var legacy = new ProcTransaccionesRequestMapper(context);
        var legacyRequest = new ProcTransaccionesRequestContract(new Dictionary<string, string>
        {
            ["TREG"] = "6",
            ["TIPTRAN"] = "22",
            ["DESCRIPCION"] = "SYNTH & <VALUE>",
            ["EMPTY"] = string.Empty,
            ["WHITESPACE"] = "   ",
            ["NULLVALUE"] = null!,
            ["ILR"] = "A",
            ["METODO"] = "LEGACY-SYNTH",
            ["IDTRAN"] = "0000001"
        });
        var treg = Binding(1, "TREG", 1, true);
        var tiptran = Binding(2, "TIPTRAN", 2, true, dataType: "int");
        var descripcion = Binding(3, "DESCRIPCION", 3, true);
        var empty = Binding(4, "EMPTY", 4, false);
        var whitespace = Binding(5, "WHITESPACE", 5, false);
        var nullValue = Binding(6, "NULLVALUE", 6, false);
        var ilr = Binding(7, "ILR", 7, false);
        var metodo = Binding(8, "METODO", 8, false);
        var idtran = Binding(9, "IDTRAN", 9, true, dataType: "long");
        var contract = Contract(
            "WSCFAACH.Proc_Transacciones",
            "Proc_Transacciones",
            [treg, tiptran, descripcion, empty, whitespace, nullValue, ilr, metodo, idtran]);
        var genericRequest = Request(contract,
        [
            Resolved(treg, "6"),
            Resolved(tiptran, "22"),
            Resolved(descripcion, "SYNTH & <VALUE>"),
            Resolved(empty, string.Empty),
            Resolved(whitespace, "   "),
            Resolved(nullValue, null),
            Resolved(ilr, "A"),
            Resolved(metodo, "LEGACY-SYNTH"),
            Resolved(idtran, "0000001")
        ]);

        var parity = new ProcTransaccionesRequestParityPolicy().Apply(genericRequest);
        var legacyOperation = Operation(legacy.BuildSoapBody(legacyRequest));
        var genericOperation = Operation(_sut.Write(parity.ContractDrivenRequest));

        AssertOperationEquivalent(legacyOperation, genericOperation);
        Assert.Equal(["TREG", "TIPTRAN", "DESCRIPCION", "IDTRAN"], genericOperation.Elements().Select(x => x.Name.LocalName).ToArray());
    }

    [Fact]
    public void ProcContrapartidas_ShouldMatchLegacyGoldenOperationForEquivalentResolvedInput()
    {
        var legacy = new ProcContrapartidasRequestMapper(Mock.Of<IProcContrapartidasFunctionalMappingResolver>());
        var legacyRequest = new ProcContrapartidasRequestContract
        {
            OFNIT = "SYNTH-NIT",
            OFEMP = "SYNTH-EMP",
            OFCTA = "SYNTH-ACCOUNT",
            OFDD = "D",
            OFFECHEFEC = "20991231",
            OFMONDEB = 12.34m,
            OFMONCRE = 56.78m,
            OFIDARCH = 101,
            OFIDLOT = 202,
            OFST = "SYNTH-ST",
            OFIDTX = "SYNTH-TX",
            OFIDREVER = 0,
            OFIDEBAPLI = 303,
            OFIDCAMCOMPE = 404,
            OFDIRECCIONIP = string.Empty,
            OFLIBRE = "SYNTH & <FREE>",
            OFLIBRE1 = 0,
            ANSIDLOTE = 0,
            ANSST = string.Empty,
            ANCLC = string.Empty,
            ANSIDTX = string.Empty,
            ANSIDREVER = 0
        };
        var values = new (string Name, string Type, bool Required, object? Value)[]
        {
            ("OFNIT", "string", true, legacyRequest.OFNIT),
            ("OFEMP", "string", true, legacyRequest.OFEMP),
            ("OFCTA", "string", true, legacyRequest.OFCTA),
            ("OFDD", "string", true, legacyRequest.OFDD),
            ("OFFECHEFEC", "string", true, legacyRequest.OFFECHEFEC),
            ("OFMONDEB", "decimal", true, legacyRequest.OFMONDEB),
            ("OFMONCRE", "decimal", true, legacyRequest.OFMONCRE),
            ("OFIDARCH", "int", true, legacyRequest.OFIDARCH),
            ("OFIDLOT", "int", true, legacyRequest.OFIDLOT),
            ("OFST", "string", true, legacyRequest.OFST),
            ("OFIDTX", "string", true, legacyRequest.OFIDTX),
            ("OFIDREVER", "int", true, legacyRequest.OFIDREVER),
            ("OFIDEBAPLI", "int", true, legacyRequest.OFIDEBAPLI),
            ("OFIDCAMCOMPE", "int", true, legacyRequest.OFIDCAMCOMPE),
            ("OFDIRECCIONIP", "string", true, legacyRequest.OFDIRECCIONIP),
            ("OFLIBRE", "string", true, legacyRequest.OFLIBRE),
            ("OFLIBRE1", "int", true, legacyRequest.OFLIBRE1),
            ("ANSIDLOTE", "int", false, legacyRequest.ANSIDLOTE),
            ("ANSST", "string", false, legacyRequest.ANSST),
            ("ANCLC", "string", false, legacyRequest.ANCLC),
            ("ANSIDTX", "string", false, legacyRequest.ANSIDTX),
            ("ANSIDREVER", "int", false, legacyRequest.ANSIDREVER)
        };
        var bindings = values.Select((value, index) => Binding(index + 1, value.Name, index + 1, value.Required, dataType: value.Type)).ToArray();
        var contract = Contract("WSCFAACH.Proc_Contrapartidas", "Proc_Contrapartidas", bindings);
        var genericRequest = Request(
            contract,
            bindings.Zip(values, (binding, value) => Resolved(binding, value.Value)).ToArray());

        var legacyOperation = XElement.Parse(legacy.BuildSoapBody(legacyRequest), LoadOptions.PreserveWhitespace);
        var genericOperation = Operation(_sut.Write(genericRequest));

        AssertOperationEquivalent(legacyOperation, genericOperation);
    }

    [Fact]
    public void RegistrarRespuestaTransaccion_ShouldMatchLegacyGoldenSevenParameterOperationIncludingNullAsEmpty()
    {
        var mapper = new RegistrarRespuestaAchSoapRequestMapper();
        var command = new RegistrarRespuestaAchCommand(
            TipoRespuestaAch.Transaccion,
            "SYNTH-TX-001",
            10,
            "SYNTH & <CHANNEL>",
            20,
            null,
            30,
            "SYNTH & <DESCRIPTION>",
            "SYNTH-CH",
            "SYNTH-ORIGIN",
            "SYNTH-DEST",
            "SYNTH-CORR");
        var legacyParameters = mapper.Map(command);
        var legacyOperation = XElement.Parse(
            SoapParameterMapper.BuildActionBody("RegistrarRespuestaTransaccion", legacyParameters, TempUri),
            LoadOptions.PreserveWhitespace);
        var specs = new (string Name, string Type, bool Required)[]
        {
            ("idCanal", "int", true),
            ("nombreCanal", "string", true),
            ("idTransaccion", "string", true),
            ("idEstado", "int", true),
            ("causal", "string", false),
            ("idTransaccionAxon", "int", true),
            ("descripcionCausal", "string", false)
        };
        var bindings = specs.Select((spec, index) => Binding(index + 1, spec.Name, index + 1, spec.Required, dataType: spec.Type)).ToArray();
        var contract = Contract("WSAXON.RegistrarRespuestaTransaccion", "RegistrarRespuestaTransaccion", bindings);
        var genericRequest = Request(
            contract,
            bindings.Select(binding => Resolved(binding, legacyParameters[binding.ParameterPath])).ToArray());

        var genericOperation = Operation(_sut.Write(genericRequest));

        Assert.Equal(7, genericOperation.Elements().Count());
        AssertOperationEquivalent(legacyOperation, genericOperation);
    }

    private static IntegrationExecutionContract Contract(
        string methodCode,
        string operationName,
        IEnumerable<IntegrationExecutionParameterBinding> bindings)
        => new(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            1,
            1,
            methodCode,
            operationName,
            TempUri,
            $"{TempUri}IContract/{operationName}",
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            1,
            1,
            bindings);

    private static IntegrationExecutionParameterBinding Binding(
        long id,
        string name,
        int sortOrder,
        bool required,
        bool nillable = false,
        string dataType = "string",
        IntegrationParameterDirectionEnum direction = IntegrationParameterDirectionEnum.Input)
        => new(
            id,
            name,
            name,
            dataType,
            dataType,
            nillable,
            required ? 1 : 0,
            1,
            required,
            sortOrder,
            new IntegrationSourceReference(IntegrationSourceKindEnum.Constant, $"constant.{name}"),
            direction: direction);

    private static ResolvedIntegrationParameter Resolved(
        IntegrationExecutionParameterBinding binding,
        object? value,
        bool isPresent = true)
        => new(
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
            value,
            binding.Direction,
            isPresent);

    private static ResolvedIntegrationRequest Request(
        IntegrationExecutionContract contract,
        IEnumerable<ResolvedIntegrationParameter> parameters)
        => new(contract, "corr-p2.3", parameters);

    private static void AssertOperationEquivalent(XElement expected, XElement actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        var expectedElements = expected.Elements().ToArray();
        var actualElements = actual.Elements().ToArray();
        Assert.Equal(expectedElements.Select(element => element.Name).ToArray(), actualElements.Select(element => element.Name).ToArray());
        Assert.Equal(expectedElements.Select(element => element.Value).ToArray(), actualElements.Select(element => element.Value).ToArray());
        Assert.Equal(
            expectedElements.Select(element => element.Attribute(Xsi + "nil")?.Value).ToArray(),
            actualElements.Select(element => element.Attribute(Xsi + "nil")?.Value).ToArray());
    }

    private static XElement Operation(string envelopeXml)
    {
        var document = XDocument.Parse(envelopeXml, LoadOptions.PreserveWhitespace);
        var body = Assert.Single(Assert.IsType<XElement>(document.Root).Elements());
        Assert.Equal(Soap11 + "Body", body.Name);
        return Assert.Single(body.Elements());
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }
}
