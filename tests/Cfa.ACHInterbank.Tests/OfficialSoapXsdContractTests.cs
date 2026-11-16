using System.Xml.Linq;
using Cfa.ACHInterbank.Application.Integrations.Contracts;
using Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>Offline tests using the five actual bundled bank-provided XSDs. No SOAP transport.</summary>
public sealed class OfficialSoapXsdContractTests
{
    private static readonly XNamespace Ns = OfficialSoapContracts.TargetNamespace;
    private static readonly XNamespace AxonNs = OfficialSoapContracts.AxonDataNamespace;
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";

    [Fact]
    public void WsdlAndFiveLocalSchemas_CompileOffline_AndActionsMatch()
    {
        OfficialSoapContracts.VerifyBundledWsdlContracts();
        Assert.Equal(27, OfficialSoapContracts.ProcTransaccionesFieldOrder.Count);
        Assert.Equal("NCTAORIG", OfficialSoapContracts.ProcTransaccionesFieldOrder[5]);
        Assert.Equal("http://tempuri.org/IWSCFAACH/Proc_Transacciones", OfficialSoapContracts.CfaSoapAction);
        Assert.Equal("http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion", OfficialSoapContracts.AxonSoapAction);
    }

    [Fact]
    public void ProcContrapartidasRequest_ValidatesContractTypesAndOptionality()
    {
        var operation = new XElement(Ns + "Proc_Contrapartidas",
            new XElement(Ns + "OFNIT", "900000001"),
            new XElement(Ns + "OFEMP", "EMPRESA-ACREDITADA"),
            new XElement(Ns + "OFCTA", "123456789"),
            new XElement(Ns + "OFDD", "D"),
            new XElement(Ns + "OFMONDEB", "123.45"),
            new XElement(Ns + "OFMONCRE", "0"),
            new XElement(Ns + "OFIDARCH", "17"),
            new XElement(Ns + "OFIDLOT", "5"));
        OfficialSoapContracts.ValidateTransportRequest(
            SoapWrap(operation), OfficialSoapContracts.Operation.ProcContrapartidasRequest);
        // Optional XSD elements are not made mandatory by the schema validator.
        OfficialSoapContracts.ValidateElement(
            new XElement(Ns + "Proc_Contrapartidas"), OfficialSoapContracts.Operation.ProcContrapartidasRequest);
    }

    [Theory]
    [InlineData("<OFIDARCH xmlns='http://tempuri.org/'>ABC</OFIDARCH>")]
    [InlineData("<OFMONDEB xmlns='http://tempuri.org/'>not-decimal</OFMONDEB>")]
    [InlineData("<OFIDLOT xmlns='http://tempuri.org/'>1.5</OFIDLOT>")]
    public void ProcContrapartidasRequest_RejectsInvalidNumericContract(string child)
    {
        var operation = new XElement(Ns + "Proc_Contrapartidas", XElement.Parse(child));
        Assert.Throws<InvalidOperationException>(() => OfficialSoapContracts.ValidateTransportRequest(
            SoapWrap(operation), OfficialSoapContracts.Operation.ProcContrapartidasRequest));
    }

    [Fact]
    public void ProcTransaccionesRequest_ValidatesOptionalElementsInExactXsdOrderAndTypes()
    {
        var operation = new XElement(Ns + "Proc_Transacciones",
            new XElement(Ns + "TIPTRAN", "22"),
            new XElement(Ns + "NCTAORIG", "ORIGIN-REAL"),
            new XElement(Ns + "FECEFEC", "20261116"),
            new XElement(Ns + "NCTARECEP", "RECIPIENT-REAL"),
            new XElement(Ns + "MONTO", "1500.25"),
            new XElement(Ns + "IDTRAN", "123456789"));
        OfficialSoapContracts.ValidateElement(operation, OfficialSoapContracts.Operation.ProcTransaccionesRequest);
        Assert.False(new ProcTransaccionesResponseParser().Parse(SoapWrap(operation)).IsSuccess);
        // The schema accepts this request; mapping provenance is checked by the mapper,
        // and the final transport retains its independent endpoint and security guards.
        IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountPresent(SoapWrap(operation));
    }

    [Theory]
    [InlineData("<TIPTRAN xmlns='http://tempuri.org/'>not-int</TIPTRAN>")]
    [InlineData("<MONTO xmlns='http://tempuri.org/'>not-double</MONTO>")]
    [InlineData("<FECEFEC xmlns='http://tempuri.org/'>999999999999</FECEFEC>")]
    [InlineData("<IDTRAN xmlns='http://tempuri.org/'>not-long</IDTRAN>")]
    [InlineData("<TIPTRAN xmlns=''>22</TIPTRAN>")]
    [InlineData("<TIPTRAN xmlns='http://tempuri.org/' xsi:nil='true' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'/>")]
    public void ProcTransaccionesRequest_RejectsInvalidTypeNamespaceOrNil(string child)
    {
        var operation = XElement.Parse($"<Proc_Transacciones xmlns='{Ns}'>{child}</Proc_Transacciones>");
        Assert.Throws<InvalidOperationException>(() =>
            OfficialSoapContracts.ValidateElement(operation, OfficialSoapContracts.Operation.ProcTransaccionesRequest));
    }

    [Fact]
    public void ProcTransaccionesRequest_RejectsReorderDuplicateAndUnknown()
    {
        foreach (var fields in new[]
        {
            new XElement[] { new(Ns + "MONTO", "10"), new(Ns + "TIPTRAN", "22") },
            new XElement[] { new(Ns + "TIPTRAN", "22"), new(Ns + "TIPTRAN", "22") },
            new XElement[] { new(Ns + "SomeOtherField", "A") }
        })
        {
            Assert.Throws<InvalidOperationException>(() =>
                OfficialSoapContracts.ValidateElement(new XElement(Ns + "Proc_Transacciones", fields),
                    OfficialSoapContracts.Operation.ProcTransaccionesRequest));
        }
    }

    [Fact]
    public void ProcResponse_SchemaValidationPlusExistingBankResultPolicy()
    {
        var parser = new ProcTransaccionesResponseParser();
        var acceptedShape = new XElement(Ns + "Proc_TransaccionesResponse",
            new XElement(Ns + "RTAACH", "00"), new XElement(Ns + "RTALOC", "OK"));
        OfficialSoapContracts.ValidateElement(acceptedShape, OfficialSoapContracts.Operation.ProcTransaccionesResponse);
        Assert.True(parser.Parse(SoapWrap(acceptedShape)).IsSuccess); // Existing code policy, NOT an XSD rule
        var noResult = new XElement(Ns + "Proc_TransaccionesResponse");
        OfficialSoapContracts.ValidateElement(noResult, OfficialSoapContracts.Operation.ProcTransaccionesResponse);
        Assert.False(parser.Parse(SoapWrap(noResult)).IsSuccess); // XSD minOccurs=0, security requires RTAACH
    }

    [Fact]
    public void ProcResponse_RejectsWrongNamespaceOrderNilStatusDuplicatesAndFault()
    {
        var parser = new ProcTransaccionesResponseParser();
        var cases = new[]
        {
            new XElement(Ns + "Proc_TransaccionesResponse", new XElement("RTAACH", "00")),
            new XElement(Ns + "Proc_TransaccionesResponse", new XElement(Ns + "RTALOC", "OK"), new XElement(Ns + "RTAACH", "00")),
            new XElement(Ns + "Proc_TransaccionesResponse", new XElement(Ns + "RTAACH", new XAttribute(Xsi + "nil", "true"))),
            new XElement(Ns + "Proc_TransaccionesResponse", new XElement(Ns + "RTAACH", "00"), new XElement(Ns + "RTAACH", "00")),
            new XElement(Ns + "Proc_TransaccionesResponse", new XElement(Ns + "RTAACH", "00"), new XElement(Ns + "SomethingUnexpected", "X"))
        };
        Assert.All(cases, body => Assert.False(parser.Parse(SoapWrap(body)).IsSuccess));
        Assert.Equal("SOAP_FAULT", parser.Parse(SoapWrap(new XElement(Soap + "Fault", "fault"))).ResponseCode);
        Assert.False(parser.Parse("<!DOCTYPE foo [<!ENTITY x SYSTEM 'file:///etc/passwd'>]>" + SoapWrap(cases[0])).IsSuccess);
    }

    [Fact]
    public void AxonRequest_XsdAcceptsSevenCorrectlyOrderedFields_RejectsInvalidInt()
    {
        var request = new XElement(Ns + "RegistrarRespuestaTransaccion",
            new XElement(Ns + "idCanal", "1"), new XElement(Ns + "nombreCanal", "ACH"),
            new XElement(Ns + "idTransaccion", "REF"), new XElement(Ns + "idEstado", "2"),
            new XElement(Ns + "causal", "R01"), new XElement(Ns + "idTransaccionAxon", "42"),
            new XElement(Ns + "descripcionCausal", "Causal"));
        OfficialSoapContracts.ValidateElement(request, OfficialSoapContracts.Operation.RegistrarRespuestaRequest);
        request.Element(Ns + "idCanal")!.Value = "bad-int";
        Assert.Throws<InvalidOperationException>(() =>
            OfficialSoapContracts.ValidateElement(request, OfficialSoapContracts.Operation.RegistrarRespuestaRequest));
    }

    [Fact]
    public void AxonResponse_ValidDataContract_ParsedWithXmlSchemaBoolean()
    {
        var parser = new RegistrarRespuestaAchSoapResponseParser();
        foreach (var lexical in new[] { "false", "0" })
        {
            var outcome = parser.Parse(SoapWrap(AxonResult(lexical)));
            Assert.False(outcome.ExisteError);
        }
        Assert.True(parser.Parse(SoapWrap(AxonResult("true"))).ExisteError);
    }

    [Fact]
    public void AxonResponse_RejectsDuplicateWrongNamespaceNilSoapFaultAndDtd()
    {
        var parser = new RegistrarRespuestaAchSoapResponseParser();
        var correct = AxonResult("false");
        var bad = new List<XElement>
        {
            new(correct),
            new(Ns + "RegistrarRespuestaTransaccionResponse", new XElement(Ns + "RegistrarRespuestaTransaccionResult",
                new XElement(Ns + "existeError", "false"))),
            new(Ns + "RegistrarRespuestaTransaccionResponse", new XElement(Ns + "RegistrarRespuestaTransaccionResult",
                new XElement(AxonNs + "existeError", "false"), new XElement(AxonNs + "existeError", "false"))),
            new(Ns + "RegistrarRespuestaTransaccionResponse", new XElement(Ns + "RegistrarRespuestaTransaccionResult",
                new XAttribute(Xsi + "nil", "true"))),
            new(Ns + "RegistrarRespuestaTransaccionResponse", new XElement(Ns + "RegistrarRespuestaTransaccionResult",
                new XElement(AxonNs + "existeError", "not-bool")))
        };
        bad[0].Element(Ns + "RegistrarRespuestaTransaccionResult")!.Element(AxonNs + "existeError")!.Remove();
        Assert.All(bad, response => Assert.Throws<InvalidOperationException>(() => parser.Parse(SoapWrap(response))));
        Assert.Throws<InvalidOperationException>(() => parser.Parse(SoapWrap(new XElement(Soap + "Fault", "fault"))));
        Assert.Throws<InvalidOperationException>(() => parser.Parse("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///etc/passwd'>]>" + SoapWrap(correct)));
    }

    private static XElement AxonResult(string flag) =>
        new(Ns + "RegistrarRespuestaTransaccionResponse",
            new XElement(Ns + "RegistrarRespuestaTransaccionResult",
                new XElement(AxonNs + "codigoError", ""),
                new XElement(AxonNs + "descripcionError", ""),
                new XElement(AxonNs + "existeError", flag)));

    private static string SoapWrap(XElement operation) =>
        new XDocument(new XElement(Soap + "Envelope", new XElement(Soap + "Body", operation)))
            .ToString(SaveOptions.DisableFormatting);
}
