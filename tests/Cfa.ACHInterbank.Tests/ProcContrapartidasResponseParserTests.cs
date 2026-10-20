using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ProcContrapartidasResponseParserTests
{
    [Fact]
    public void Parse_DebeRetornarRetryable_CuandoRespuestaVacia()
    {
        var sut = new ProcContrapartidasResponseParser();

        var result = sut.Parse(string.Empty);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsSoapFault);
        Assert.True(result.IsRetryable);
        Assert.Equal("EMPTY_RESPONSE", result.ResponseCode);
    }

    [Fact]
    public void Parse_DebeRetornarExito_CuandoAnsstEs00()
    {
        var sut = new ProcContrapartidasResponseParser();
        var xml = "<Envelope><Body><Proc_ContrapartidasResponse><ANSST>00</ANSST><ANCLC>00</ANCLC><ANSIDTX>123</ANSIDTX></Proc_ContrapartidasResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFunctionalRejection);
        Assert.False(result.IsRetryable);
        Assert.Equal("00", result.ResponseCode);
        Assert.True(result.ItemResults.ContainsKey(1));
        Assert.True(result.ItemResults[1].IsSuccess);
    }

    [Fact]
    public void Parse_DebeRetornarExito_CuandoAnsstEsR96()
    {
        var sut = new ProcContrapartidasResponseParser();
        var xml = "<Envelope><Body><Proc_ContrapartidasResponse><ANSST>R96</ANSST><ANCLC>00</ANCLC></Proc_ContrapartidasResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFunctionalRejection);
        Assert.Equal("R96", result.ResponseCode);
    }

    [Fact]
    public void Parse_DebeRetornarRechazoFuncional_CuandoAnsstEsR01()
    {
        var sut = new ProcContrapartidasResponseParser();
        var xml = "<Envelope><Body><Proc_ContrapartidasResponse><ANSST>R01</ANSST><ANCLC>R01</ANCLC></Proc_ContrapartidasResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFunctionalRejection);
        Assert.False(result.IsRetryable);
        Assert.Equal("R01", result.ResponseCode);
    }

    [Fact]
    public void Parse_DebeRetornarRechazoFuncional_CuandoAnsstNoExitoso()
    {
        var sut = new ProcContrapartidasResponseParser();
        var xml = "<Envelope><Body><Proc_ContrapartidasResponse><ANSST>R10</ANSST><ANCLC>R10</ANCLC></Proc_ContrapartidasResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFunctionalRejection);
        Assert.False(result.IsRetryable);
        Assert.Equal("R10", result.ResponseCode);
        Assert.Equal("R10", result.ErrorCode);
    }

    [Theory]
    [InlineData("RE")]
    [InlineData("0")]
    public void Parse_DebeTratarCodigosTecnicosAnomalosComoNoFuncionales(string code)
    {
        var sut = new ProcContrapartidasResponseParser();
        var xml = $"<Envelope><Body><Proc_ContrapartidasResponse><ANSST>{code}</ANSST><ANCLC>{code}</ANCLC></Proc_ContrapartidasResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsFunctionalRejection);
        Assert.False(result.IsSoapFault);
        Assert.Equal(code, result.ResponseCode);
    }

    [Fact]
    public void Parse_DebeRetornarSoapFaultRetryable_CuandoFaultCodeEsServer()
    {
        var sut = new ProcContrapartidasResponseParser();
        var xml = "<Envelope><Body><Fault><faultcode>soap:Server</faultcode><faultstring>Error interno</faultstring><detail>detalle</detail></Fault></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsSoapFault);
        Assert.True(result.IsRetryable);
        Assert.Equal("soap:Server", result.FaultCode);
        Assert.Equal("soap:Server", result.ErrorCode);
    }

    [Fact]
    public void Parse_DebeRetornarSoapFaultNoRetryable_CuandoFaultCodeEsClient()
    {
        var sut = new ProcContrapartidasResponseParser();
        var xml = "<Envelope><Body><Fault><faultcode>soap:Client</faultcode><faultstring>Solicitud invalida</faultstring></Fault></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsSoapFault);
        Assert.False(result.IsRetryable);
        Assert.Equal("soap:Client", result.ResponseCode);
    }

    [Fact]
    public void Parse_DebeInterpretarResultadosPorItem_CuandoRespuestaIncluyeTransactionResult()
    {
        var sut = new ProcContrapartidasResponseParser();
        var xml = "<Envelope><Body><Proc_ContrapartidasResponse><Codigo>R96</Codigo><TransactionResult><TransactionId>101</TransactionId><Code>R96</Code><Message>Aplicado</Message></TransactionResult><TransactionResult><TransactionId>202</TransactionId><Code>R98</Code><Message>Temporal</Message></TransactionResult></Proc_ContrapartidasResponse></Body></Envelope>";

        var result = sut.Parse(xml);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsSoapFault);
        Assert.True(result.IsRetryable);
        Assert.Equal(2, result.ItemResults.Count);
        Assert.False(result.ItemResults[101].IsSuccess);
        Assert.True(result.ItemResults[202].IsRetryable);
    }
}
