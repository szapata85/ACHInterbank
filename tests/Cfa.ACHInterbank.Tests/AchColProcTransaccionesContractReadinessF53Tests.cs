using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>F5.3: offline, no SOAP client, no database, no Live calls.</summary>
public sealed class AchColProcTransaccionesContractReadinessF53Tests
{
    private const string Action = "http://tempuri.org/";
    private const string Soap = "http://schemas.xmlsoap.org/soap/envelope/";

    [Theory]
    [InlineData("TIPTRAN", "22")]
    [InlineData("TIPTRAN", "27")]
    public void Inspect_ValidDryRun_IsSafeRedactedAndNeverTransmittable(string field, string value)
    {
        var preview = Preview(new Dictionary<string, string>
        {
            [field] = value, ["MONTO"] = "1500", ["NCTARECEP"] = "PRIVATE-ACCOUNT"
        });
        var result = AchColProcTransaccionesDryRunContractCheck.Inspect(preview);
        Assert.True(result.IsSoapPreviewStructurallyValid);
        Assert.False(result.CanTransmit);
        Assert.False(result.HasExternalXsdEvidence);
        Assert.False(result.HasApprovedAchColMapping);
        Assert.Equal("ACHCOL_F53_EXTERNAL_CONTRACT_NOT_HOMOLOGATED", result.BlockingCode);
        Assert.Contains("IDCAMCOMPE", result.MissingRequiredParameterNames);
        Assert.Contains("NCTARECEP", result.ObservedParameterNames);
        Assert.DoesNotContain("PRIVATE-ACCOUNT", string.Join("|", result.ObservedParameterNames));
        Assert.DoesNotContain("1500", string.Join("|", result.ObservedParameterNames));
        Assert.Throws<InvalidOperationException>(() => AchColProcTransaccionesFieldSources.RequireHomologatedMapping(true));
    }

    [Theory]
    [InlineData("IDCAMCOMPE", "8")]
    [InlineData("IDTRAN", "888")]
    [InlineData("IDLOTE", "1")]
    [InlineData("DISCRE", "UNAPPROVED")]
    public void Inspect_RejectsUnhomologatedFieldsEvenIfXmlMatches(string field, string value)
    {
        var values = new Dictionary<string, string> { ["TIPTRAN"] = "22", ["MONTO"] = "1500", [field] = value };
        Assert.Contains("ACHCOL_F53_UNAPPROVED_FIELD", Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesDryRunContractCheck.Inspect(Preview(values))).Message);
    }

    [Fact]
    public void Inspect_RejectsAlteredXmlAndXmlEntityPayloads()
    {
        var preview = Preview(new Dictionary<string, string> { ["TIPTRAN"] = "22", ["MONTO"] = "1" });
        Assert.Contains("ACHCOL_F53_SOAP_PREVIEW_MISMATCH", Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesDryRunContractCheck.Inspect(preview with
            { SoapPreviewXml = preview.SoapPreviewXml.Replace(">22<", ">23<", StringComparison.Ordinal) })).Message);
        Assert.Contains("ACHCOL_F53_SOAP_PREVIEW_INVALID", Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesDryRunContractCheck.Inspect(preview with
            { SoapPreviewXml = "<!DOCTYPE foo [<!ENTITY x SYSTEM 'file:///etc/passwd'>]>" + preview.SoapPreviewXml })).Message);
    }

    [Fact]
    public void Inspect_RejectsUnexpectedSoapOperationOrUnblockedPreview()
    {
        var preview = Preview(new Dictionary<string, string> { ["TIPTRAN"] = "22", ["MONTO"] = "1" });
        Assert.Contains("ACHCOL_F53_SOAP_PREVIEW_INVALID", Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesDryRunContractCheck.Inspect(preview with
            { SoapPreviewXml = preview.SoapPreviewXml.Replace("Proc_Transacciones", "OtraOperacion", StringComparison.Ordinal) })).Message);
        Assert.Contains("ACHCOL_F53_PREVIEW_INVALID", Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesDryRunContractCheck.Inspect(preview with { BlockingCode = "NOT_BLOCKED" })).Message);
    }

    [Fact]
    public void Inspect_RejectsUnexpectedSoapBodyAttributesAndFreeText()
    {
        var preview = Preview(new Dictionary<string, string> { ["TIPTRAN"] = "22", ["MONTO"] = "1" });
        var bodyAttribute = preview.SoapPreviewXml.Replace(
            "<soapenv:Body>", "<soapenv:Body injected=\"unexpected\">", StringComparison.Ordinal);
        Assert.Contains("ACHCOL_F53_SOAP_PREVIEW_INVALID", Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesDryRunContractCheck.Inspect(preview with
            { SoapPreviewXml = bodyAttribute })).Message);

        var strayText = preview.SoapPreviewXml.Replace(
            "</tem:Proc_Transacciones>", "unexpected</tem:Proc_Transacciones>", StringComparison.Ordinal);
        Assert.Contains("ACHCOL_F53_SOAP_PREVIEW_INVALID", Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesDryRunContractCheck.Inspect(preview with
            { SoapPreviewXml = strayText })).Message);
    }

    private static AchColProcTransaccionesDryRunPreview Preview(Dictionary<string, string> values)
    {
        var fields = string.Join("", values.Select(x =>
            $"<tem:{x.Key}>{System.Security.SecurityElement.Escape(x.Value)}</tem:{x.Key}>"));
        var xml = $"<soapenv:Envelope xmlns:soapenv=\"{Soap}\" xmlns:tem=\"{Action}\"><soapenv:Body><tem:Proc_Transacciones>{fields}</tem:Proc_Transacciones></soapenv:Body></soapenv:Envelope>";
        return new AchColProcTransaccionesDryRunPreview(
            Guid.NewGuid(), 101, Guid.NewGuid(), 1, values,
            new[] { "IDCAMCOMPE", "IDTRAN", "IDLOTE" }, new[] { "IDCAMCOMPE" },
            xml, "ACHCOL_SOAP_CONTRACT_NOT_HOMOLOGATED_F52");
    }
}
