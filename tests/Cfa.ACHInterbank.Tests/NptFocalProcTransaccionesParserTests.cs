using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NptFocalProcTransaccionesParserTests
{
    private const string Prefix = "<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/' xmlns:t='http://tempuri.org/'><s:Body><t:Proc_TransaccionesResponse>";
    private const string Suffix = "</t:Proc_TransaccionesResponse></s:Body></s:Envelope>";

    [Theory]
    [InlineData("<container><RTAACH>00</RTAACH></container>", "MISSING_OR_AMBIGUOUS_RTAACH")]
    [InlineData("<t:RTAACH xmlns:t='urn:hostile'>00</t:RTAACH>", "MISSING_OR_AMBIGUOUS_RTAACH")]
    [InlineData("<t:RTAACH>00</t:RTAACH><t:RTALOC>a</t:RTALOC><t:RTALOC>b</t:RTALOC>", "AMBIGUOUS_RTALOC")]
    [InlineData("<RTAACH>00</RTAACH><s:Fault><faultstring>x</faultstring></s:Fault>", "SOAP_FAULT")]
    [InlineData("<RTAACH>00</RTAACH><x><RTAACH>00</RTAACH></x>", "MISSING_OR_AMBIGUOUS_RTAACH")]
    public void RejectsNestedOrSpoofedSuccess(string body, string expectedCode)
    {
        var result = new ProcTransaccionesResponseParser().Parse(Prefix + body + Suffix);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.Equal(expectedCode, result.ResponseCode);
    }

    [Fact]
    public void UnrecognizedDirectResponseChild_NeverAuthorizesBankSuccess()
    {
        var result = new ProcTransaccionesResponseParser().Parse(
            Prefix + "<t:RTAACH>00</t:RTAACH><Unexpected>Override</Unexpected>" + Suffix);
        Assert.False(result.IsSuccess);
        Assert.Equal("UNEXPECTED_SOAP_RESPONSE_FIELD", result.ResponseCode);
    }

    [Fact]
    public void XmlDtdOrExternalEntity_DoesNotBecomeSuccess()
    {
        var xml = "<!DOCTYPE foo [<!ENTITY x SYSTEM 'file:///etc/passwd'>]>"
            + Prefix + "<RTAACH>&x;</RTAACH>" + Suffix;
        Assert.False(new ProcTransaccionesResponseParser().Parse(xml).IsSuccess);
    }
}
