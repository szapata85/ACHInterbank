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
    public void Parse_ReturnsRetryable_WhenSoapFault()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<Envelope><Body><Fault><faultstring>Error temporal</faultstring></Fault></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsRetryable);
    }
}
