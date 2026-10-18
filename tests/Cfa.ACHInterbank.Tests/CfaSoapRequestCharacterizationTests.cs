using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.External.Connections;
using Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaSoapRequestCharacterizationTests
{
    private static readonly XNamespace Soap11 = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace TempUri = "http://tempuri.org/";

    [Fact]
    public async Task ProcTransacciones_BuildSoapBody_FreezesDynamicDictionaryOrderOmissionsNamespacesAndEscaping()
    {
        await using var context = BuildContext();
        var sut = new ProcTransaccionesRequestMapper(context);
        var request = new ProcTransaccionesRequestContract(new Dictionary<string, string>
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

        var xml = sut.BuildSoapBody(request);
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var envelope = Assert.IsType<XElement>(document.Root);
        Assert.Equal(Soap11 + "Envelope", envelope.Name);

        var body = Assert.Single(envelope.Elements());
        Assert.Equal(Soap11 + "Body", body.Name);
        var operation = Assert.Single(body.Elements());
        Assert.Equal(TempUri + "Proc_Transacciones", operation.Name);

        Assert.Equal(
            ["TREG", "TIPTRAN", "DESCRIPCION", "IDTRAN"],
            operation.Elements().Select(x => x.Name.LocalName).ToArray());
        Assert.All(operation.Elements(), x => Assert.Equal(TempUri.NamespaceName, x.Name.NamespaceName));
        Assert.Equal("6", operation.Element(TempUri + "TREG")!.Value);
        Assert.Equal("22", operation.Element(TempUri + "TIPTRAN")!.Value);
        Assert.Equal("SYNTH & <VALUE>", operation.Element(TempUri + "DESCRIPCION")!.Value);
        Assert.Equal("0000001", operation.Element(TempUri + "IDTRAN")!.Value);
        Assert.DoesNotContain(operation.Elements(), x => x.Name.LocalName is "EMPTY" or "WHITESPACE" or "NULLVALUE" or "ILR" or "METODO");
        Assert.Contains("SYNTH &amp; &lt;VALUE&gt;", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcContrapartidas_BuildSoapBody_FreezesTypedContractElementOrderPresenceAndEscaping()
    {
        var sut = new ProcContrapartidasRequestMapper(Mock.Of<IProcContrapartidasFunctionalMappingResolver>());
        var request = new ProcContrapartidasRequestContract
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

        var xml = sut.BuildSoapBody(request);
        var operation = XElement.Parse(xml, LoadOptions.PreserveWhitespace);

        Assert.Equal(TempUri + "Proc_Contrapartidas", operation.Name);
        Assert.Equal(
            [
                "OFNIT", "OFEMP", "OFCTA", "OFDD", "OFFECHEFEC", "OFMONDEB", "OFMONCRE", "OFIDARCH",
                "OFIDLOT", "OFST", "OFIDTX", "OFIDREVER", "OFIDEBAPLI", "OFIDCAMCOMPE", "OFDIRECCIONIP",
                "OFLIBRE", "OFLIBRE1", "ANSIDLOTE", "ANSST", "ANCLC", "ANSIDTX", "ANSIDREVER"
            ],
            operation.Elements().Select(x => x.Name.LocalName).ToArray());
        Assert.All(operation.Elements(), x => Assert.Equal(TempUri.NamespaceName, x.Name.NamespaceName));
        Assert.Equal("12.34", operation.Element(TempUri + "OFMONDEB")!.Value);
        Assert.Equal("56.78", operation.Element(TempUri + "OFMONCRE")!.Value);
        Assert.Equal(string.Empty, operation.Element(TempUri + "OFDIRECCIONIP")!.Value);
        Assert.Equal(string.Empty, operation.Element(TempUri + "ANSST")!.Value);
        Assert.Equal(string.Empty, operation.Element(TempUri + "ANCLC")!.Value);
        Assert.Equal(string.Empty, operation.Element(TempUri + "ANSIDTX")!.Value);
        Assert.Equal("SYNTH & <FREE>", operation.Element(TempUri + "OFLIBRE")!.Value);
        Assert.Contains("SYNTH &amp; &lt;FREE&gt;", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistrarRespuestaTransaccion_PhysicalMapperAndBody_FreezesSevenParametersOrderNullsAndEscaping()
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

        var parameters = mapper.Map(command);

        Assert.Equal(7, parameters.Count);
        Assert.Equal(
            ["idCanal", "nombreCanal", "idTransaccion", "idEstado", "causal", "idTransaccionAxon", "descripcionCausal"],
            parameters.Keys.ToArray());
        Assert.Equal(10, parameters["idCanal"]);
        Assert.Equal("SYNTH & <CHANNEL>", parameters["nombreCanal"]);
        Assert.Equal("SYNTH-TX-001", parameters["idTransaccion"]);
        Assert.Equal(20, parameters["idEstado"]);
        Assert.Null(parameters["causal"]);
        Assert.Equal(30, parameters["idTransaccionAxon"]);
        Assert.Equal("SYNTH & <DESCRIPTION>", parameters["descripcionCausal"]);

        var xml = SoapParameterMapper.BuildActionBody(
            "RegistrarRespuestaTransaccion",
            parameters,
            TempUri.NamespaceName);
        var operation = XElement.Parse(xml, LoadOptions.PreserveWhitespace);

        Assert.Equal(TempUri + "RegistrarRespuestaTransaccion", operation.Name);
        Assert.Equal(
            ["idCanal", "nombreCanal", "idTransaccion", "idEstado", "causal", "idTransaccionAxon", "descripcionCausal"],
            operation.Elements().Select(x => x.Name.LocalName).ToArray());
        Assert.All(operation.Elements(), x => Assert.Equal(TempUri.NamespaceName, x.Name.NamespaceName));
        Assert.Equal(string.Empty, operation.Element(TempUri + "causal")!.Value);
        Assert.Equal("SYNTH & <DESCRIPTION>", operation.Element(TempUri + "descripcionCausal")!.Value);
        Assert.Contains("SYNTH &amp; &lt;CHANNEL&gt;", xml, StringComparison.Ordinal);
        Assert.Contains("SYNTH &amp; &lt;DESCRIPTION&gt;", xml, StringComparison.Ordinal);
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }
}
