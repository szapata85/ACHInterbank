using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ProcTransaccionesResponseParserTests
{
    [Fact]
    public void Parse_ReturnsSuccess_WhenAnsstIs00()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><tem:Proc_TransaccionesResponse xmlns:tem=\"http://tempuri.org/\"><tem:RTAACH>00</tem:RTAACH><tem:RTALOC>OK</tem:RTALOC></tem:Proc_TransaccionesResponse></s:Body></s:Envelope>";

        var result = sut.Parse(xml);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.Equal("00", result.ResponseCode);
    }

    [Fact]
    public void Parse_ReturnsFunctionalRejection_WhenRtaAchIsBusinessError()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><tem:Proc_TransaccionesResponse xmlns:tem=\"http://tempuri.org/\"><tem:RTAACH>105</tem:RTAACH><tem:RTALOC>Saldo insuficiente</tem:RTALOC></tem:Proc_TransaccionesResponse></s:Body></s:Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFunctionalRejection);
        Assert.False(result.IsRetryable);
        Assert.Equal("105", result.ResponseCode);
    }

    [Fact]
    public void Parse_PreservesR96_WithoutAssigningBusinessSuccess()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><tem:Proc_TransaccionesResponse xmlns:tem=\"http://tempuri.org/\"><tem:RTAACH>R96</tem:RTAACH><tem:RTALOC>Aplicado</tem:RTALOC></tem:Proc_TransaccionesResponse></s:Body></s:Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.True(result.IsFunctionalRejection);
        Assert.Equal("R96", result.ResponseCode);
        Assert.Equal("Aplicado", result.ResponseMessage);
    }

    [Fact]
    public void Parse_PreservesUnknownCodes_AsFunctionalRejectionWithoutInventingMeaning()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><tem:Proc_TransaccionesResponse xmlns:tem=\"http://tempuri.org/\"><tem:RTAACH>R17</tem:RTAACH><tem:RTALOC>Codigo legacy observado</tem:RTALOC></tem:Proc_TransaccionesResponse></s:Body></s:Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFunctionalRejection);
        Assert.False(result.IsRetryable);
        Assert.Equal("R17", result.ResponseCode);
        Assert.Equal("Codigo legacy observado", result.ResponseMessage);
    }

    [Fact]
    public void Parse_Quarantines_WhenSoapFault()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><s:Fault><faultstring>Error temporal</faultstring></s:Fault></s:Body></s:Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.Equal("SOAP_FAULT", result.ResponseCode);
    }

    [Theory]
    [InlineData("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>00</RTAACH></Proc_TransaccionesResponse></Body></Envelope>", "INVALID_SOAP_ENVELOPE")]
    [InlineData("<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/' xmlns:t='http://tempuri.org/'><s:Body><t:AnotherOperationResponse><RTAACH>00</RTAACH></t:AnotherOperationResponse></s:Body></s:Envelope>", "WRONG_SOAP_OPERATION")]
    [InlineData("<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/' xmlns:t='http://tempuri.org/'><s:Body><t:Proc_TransaccionesResponse><RTAACH>00</RTAACH><RTAACH>ERROR</RTAACH></t:Proc_TransaccionesResponse></s:Body></s:Envelope>", "MISSING_OR_AMBIGUOUS_RTAACH")]
    [InlineData("<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/' xmlns:t='http://tempuri.org/'><s:Body><t:Proc_TransaccionesResponse /></s:Body></s:Envelope>", "MISSING_OR_AMBIGUOUS_RTAACH")]
    public void Parse_RejectsFalseSuccessFromWrongEnvelopeOrOperation(string xml, string code)
    {
        var result = new ProcTransaccionesResponseParser().Parse(xml);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.Equal(code, result.ResponseCode);
    }

    [Theory]
    [InlineData("TIMEOUT")]
    [InlineData("SOAP_TIMEOUT")]
    [InlineData("500")]
    [InlineData("503")]
    public void Parse_NeverSchedulesReplay_ForAmbiguousCoreCode(string code)
    {
        var xml = $"<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/' xmlns:t='http://tempuri.org/'><s:Body><t:Proc_TransaccionesResponse><t:RTAACH>{code}</t:RTAACH></t:Proc_TransaccionesResponse></s:Body></s:Envelope>";
        var result = new ProcTransaccionesResponseParser().Parse(xml);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
    }
}
