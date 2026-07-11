using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ProcTransaccionesResponseParserTests
{
    [Fact]
    public void Parse_ReturnsSuccess_WhenAnsstIs00()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<Envelope><Body><Proc_TransaccionesResponse><RTAACH>00</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.Equal("00", result.ResponseCode);
    }

    [Fact]
    public void Parse_ReturnsFunctionalRejection_WhenRtaAchIsBusinessError()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<Envelope><Body><Proc_TransaccionesResponse><RTAACH>105</RTAACH><RTALOC>Saldo insuficiente</RTALOC></Proc_TransaccionesResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFunctionalRejection);
        Assert.False(result.IsRetryable);
        Assert.Equal("105", result.ResponseCode);
    }

    [Fact]
    public void Parse_ReturnsSuccess_WhenRtaAchIsObservedR96()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<Envelope><Body><Proc_TransaccionesResponse><RTAACH>R96</RTAACH><RTALOC>Aplicado</RTALOC></Proc_TransaccionesResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.False(result.IsFunctionalRejection);
        Assert.Equal("R96", result.ResponseCode);
        Assert.Equal("Aplicado", result.ResponseMessage);
    }

    [Fact]
    public void Parse_PreservesUnknownCodes_AsFunctionalRejectionWithoutInventingMeaning()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<Envelope><Body><Proc_TransaccionesResponse><RTAACH>R17</RTAACH><RTALOC>Codigo legacy observado</RTALOC></Proc_TransaccionesResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFunctionalRejection);
        Assert.False(result.IsRetryable);
        Assert.Equal("R17", result.ResponseCode);
        Assert.Equal("Codigo legacy observado", result.ResponseMessage);
    }

    [Fact]
    public void Parse_ReturnsRetryable_WhenSoapFault()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<Envelope><Body><Fault><faultstring>Error temporal</faultstring></Fault></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsRetryable);
    }
}
