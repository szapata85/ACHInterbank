using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ProcTransaccionesResponseParserTests
{
    [Fact]
    public void Parse_ReturnsSuccess_WhenAnsstIs00()
    {
        var sut = new ProcTransaccionesResponseParser();
        var xml = "<Envelope><Body><Proc_TransaccionesResponse><ANSST>00</ANSST><ANSMEN>OK</ANSMEN></Proc_TransaccionesResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.Equal("00", result.ResponseCode);
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
