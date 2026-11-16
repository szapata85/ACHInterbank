using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ProcTransaccionesOriginAccountSafetyTests
{
    private static string Envelope(string content) =>
        $"<soap:Envelope xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'><soap:Body><tem:Proc_Transacciones>{content}</tem:Proc_Transacciones></soap:Body></soap:Envelope>";

    [Fact]
    public void Live_AcceptsPresentOriginAccount_WithoutIndependentProvenanceGate()
    {
        IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountPresent(
            Envelope("<tem:NCTAORIG>0001234500</tem:NCTAORIG>"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("<tem:NCTAORIG> </tem:NCTAORIG>")]
    [InlineData("<tem:NCTAORIG xsi:nil='true'/>")]
    [InlineData("<tem:NCTAORIG xsi:nil='1'/>")]
    [InlineData("<tem:NCTAORIG>123</tem:NCTAORIG><tem:NCTAORIG>123</tem:NCTAORIG>")]
    [InlineData("<tem:NCTARECEP>999</tem:NCTARECEP>")]
    public void Live_RejectsMissingEmptyNilOrDuplicateOriginAccount(string fields)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountPresent(Envelope(fields)));
        Assert.Contains("PROC_TRANSACCIONES_NCTAORIG_SOURCE_INVALID", exception.Message);
    }

    [Fact]
    public void Live_RejectsXmlWithDtd()
    {
        var xml = "<!DOCTYPE soap:Envelope [<!ENTITY x SYSTEM 'file:///etc/passwd'>]>" +
                  Envelope("<tem:NCTAORIG>&x;</tem:NCTAORIG>");
        Assert.Throws<System.Xml.XmlException>(() =>
            IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountPresent(xml));
    }
}
