using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaSoapOperationParityGuardTests
{
    private const string Action = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas";
    private const string Legacy = "<Proc_Contrapartidas xmlns=\"http://tempuri.org/\"><OFIDTX>SYNTH-001</OFIDTX><OFMONDEB>12.34</OFMONDEB><ANSST></ANSST></Proc_Contrapartidas>";
    private const string Equivalent = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"><soapenv:Body><tem:Proc_Contrapartidas xmlns:tem=\"http://tempuri.org/\"><tem:OFIDTX>SYNTH-001</tem:OFIDTX><tem:OFMONDEB>12.34</tem:OFMONDEB><tem:ANSST /></tem:Proc_Contrapartidas></soapenv:Body></soapenv:Envelope>";

    [Fact]
    public void EnsureEquivalent_ShouldAcceptSameSoapFieldsWithDifferentPrefixAndEmptySyntax()
        => Ensure(Equivalent);

    [Theory]
    [InlineData("SYNTH-002", "SYNTH-001")]
    [InlineData("OFIDTX_ALT", "OFIDTX")]
    public void EnsureEquivalent_ShouldRejectValueAndElementDrift(string replacement, string original)
    {
        var xml = Equivalent.Replace(original, replacement, StringComparison.Ordinal);
        var error = Assert.Throws<InvalidOperationException>(() => Ensure(xml));
        Assert.Contains("PROC_CONTRAPARTIDAS_WIRE_PARITY_MISMATCH", error.Message);
    }

    [Fact]
    public void EnsureEquivalent_ShouldRejectChangedInputOrder()
    {
        var xml = Equivalent.Replace(
            "<tem:OFIDTX>SYNTH-001</tem:OFIDTX><tem:OFMONDEB>12.34</tem:OFMONDEB>",
            "<tem:OFMONDEB>12.34</tem:OFMONDEB><tem:OFIDTX>SYNTH-001</tem:OFIDTX>",
            StringComparison.Ordinal);
        var error = Assert.Throws<InvalidOperationException>(() => Ensure(xml));
        Assert.Contains("WIRE_PARITY_MISMATCH", error.Message);
    }

    [Fact]
    public void EnsureEquivalent_ShouldRejectDifferentSoapActionBeforeComparingFields()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            CfaSoapOperationParityGuard.EnsureEquivalent(
                "PROC_CONTRAPARTIDAS", "Proc_Contrapartidas", Action,
                "Proc_Contrapartidas", "http://tempuri.org/", "urn:incorrect-action", Legacy, Equivalent));
        Assert.Contains("WIRE_PARITY_MISMATCH", error.Message);
    }

    [Fact]
    public void EnsureEquivalent_ShouldRejectMissingSoapEnvelopeFromContractDrivenWriter()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Ensure(Legacy));
        Assert.Contains("SOAP_WIRE_PARITY_ENVELOPE_MISSING", error.Message);
    }

    [Fact]
    public void EnsureEquivalent_ShouldRejectNilVersusEmptyElement()
    {
        var xml = Equivalent.Replace(
            "<tem:ANSST />",
            "<tem:ANSST xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\" />",
            StringComparison.Ordinal);
        var error = Assert.Throws<InvalidOperationException>(() => Ensure(xml));
        Assert.Contains("WIRE_PARITY_MISMATCH", error.Message);
    }

    [Fact]
    public void EnsureEquivalent_ShouldCoverProcTransaccionesEnvelopeWithExactFieldNames()
    {
        const string action = "http://tempuri.org/IWSCFAACH/Proc_Transacciones";
        const string xml = "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><Proc_Transacciones xmlns=\"http://tempuri.org/\"><TIPTRAN>22</TIPTRAN><MONTO>12.34</MONTO></Proc_Transacciones></soap:Body></soap:Envelope>";
        CfaSoapOperationParityGuard.EnsureEquivalent(
            "PROC_TRANSACCIONES", "Proc_Transacciones", action,
            "Proc_Transacciones", "http://tempuri.org/", action, xml, xml);
        var error = Assert.Throws<InvalidOperationException>(() =>
            CfaSoapOperationParityGuard.EnsureEquivalent(
                "PROC_TRANSACCIONES", "Proc_Transacciones", action,
                "Proc_Transacciones", "http://tempuri.org/", "urn:changed", xml, xml));
        Assert.Contains("WIRE_PARITY_MISMATCH", error.Message);
    }

    private static void Ensure(string actual) => CfaSoapOperationParityGuard.EnsureEquivalent(
        "PROC_CONTRAPARTIDAS", "Proc_Contrapartidas", Action,
        "Proc_Contrapartidas", "http://tempuri.org/", Action, Legacy, actual);
}
