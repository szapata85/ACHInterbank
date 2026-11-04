using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ProcTransaccionesOriginAccountSafetyTests
{
    [Theory]
    [InlineData("<Envelope><Body><Proc_Transacciones><NCTAORIG>12345</NCTAORIG><NCTARECEP>67890</NCTARECEP></Proc_Transacciones></Body></Envelope>")]
    [InlineData("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\"><soap:Body><tem:Proc_Transacciones><tem:NCTAORIG>12345</tem:NCTAORIG></tem:Proc_Transacciones></soap:Body></soap:Envelope>")]
    public void Live_BlocksUnhomologatedOriginAccount_BeforeTransport(string soap)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountHomologated(soap));
        Assert.Contains("PROC_TRANSACCIONES_NCTAORIG_NOT_HOMOLOGATED", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<Envelope><Body><Proc_Transacciones><NCTARECEP>67890</NCTARECEP></Proc_Transacciones></Body></Envelope>")]
    [InlineData("<Envelope><Body><Proc_Transacciones><NCTAORIG> </NCTAORIG></Proc_Transacciones></Body></Envelope>")]
    public void Live_DoesNotRejectAbsentOriginAccount(string soap)
        => IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountHomologated(soap);
}
