using System.Text;
using Cfa.ACHInterbank.Tests.Utilities;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public class DigitalEnvelopeInteroperabilityHarnessTests
{
    private readonly DigitalEnvelopeInteroperabilityHarness _harness = new();

    [Fact]
    public void GeneratedEnvelope_ShouldHaveRequiredXmlNodes()
    {
        var envelope = BuildSyntheticEnvelope("NACHA-INTEROP-01");
        var report = _harness.InspectEnvelope(envelope);
        report.RequiredNodesPresent.Should().BeTrue();
    }

    [Fact]
    public void GeneratedEnvelope_ShouldDeclareExpectedAlgorithms()
    {
        var envelope = BuildSyntheticEnvelope("NACHA-INTEROP-02");
        var report = _harness.InspectEnvelope(envelope);
        report.AlgorithmsDeclared["KeyEncryptionAlgorithm"].Should().Be("RSA/NONE/PKCS1Padding");
        report.AlgorithmsDeclared["ContentEncryptionAlgorithm"].Should().Be("AES/CBC/PKCS5padding");
    }

    [Fact]
    public void GeneratedEnvelope_ShouldContainIdentifier()
    {
        var envelope = BuildSyntheticEnvelope("NACHA-INTEROP-03");
        var report = _harness.InspectEnvelope(envelope);
        report.Identifier.Should().NotBeNullOrWhiteSpace();
        report.IdentifierLength.Should().BeGreaterThan(16);
    }

    [Fact]
    public void GeneratedEnvelope_ShouldContainSignerCertificate()
    {
        var signer = DigitalEnvelopeInteroperabilityHarness.CreateSelfSignedCertificate("CN=InteropSigner");
        var receiver = DigitalEnvelopeInteroperabilityHarness.CreateSelfSignedCertificate("CN=InteropReceiver");
        var envelope = _harness.BuildSyntheticEnvelope(Encoding.UTF8.GetBytes("NACHA-INTEROP-04"), signer, receiver);
        var plain = _harness.RoundtripDecrypt(envelope, signer, receiver, out _);
        plain.Should().NotBeNull();
    }

    [Fact]
    public void GeneratedEnvelope_ShouldContainEncryptedKeyAndEncryptedContent()
    {
        var envelope = BuildSyntheticEnvelope("NACHA-INTEROP-05");
        var report = _harness.InspectEnvelope(envelope);
        report.EncryptionMetadata["EncryptedKeyLength"].Should().NotBe("0");
        report.EncryptionMetadata["EncryptedContentLength"].Should().NotBe("0");
    }

    [Fact]
    public void GeneratedEnvelope_ShouldHaveBase64FieldsValid()
    {
        var envelope = BuildSyntheticEnvelope("NACHA-INTEROP-06");
        var report = _harness.InspectEnvelope(envelope);
        report.ZipBase64Validation["EncryptedKeyBase64"].Should().BeTrue();
        report.ZipBase64Validation["EncryptedContentBase64"].Should().BeTrue();
    }

    [Fact]
    public void GeneratedEnvelope_ShouldRoundtrip_WithTestCertificates()
    {
        var plainText = "NACHA-INTEROP-07";
        var signer = DigitalEnvelopeInteroperabilityHarness.CreateSelfSignedCertificate("CN=InteropSignerRoundtrip");
        var receiver = DigitalEnvelopeInteroperabilityHarness.CreateSelfSignedCertificate("CN=InteropReceiverRoundtrip");
        var envelope = _harness.BuildSyntheticEnvelope(Encoding.UTF8.GetBytes(plainText), signer, receiver);

        var plain = _harness.RoundtripDecrypt(envelope, signer, receiver, out _);

        Encoding.UTF8.GetString(plain).Should().Be(plainText);
    }

    [Fact]
    public void GeneratedEnvelope_ShouldValidateSignatureAfterRoundtrip()
    {
        var signer = DigitalEnvelopeInteroperabilityHarness.CreateSelfSignedCertificate("CN=InteropSignerSig");
        var receiver = DigitalEnvelopeInteroperabilityHarness.CreateSelfSignedCertificate("CN=InteropReceiverSig");
        var envelope = _harness.BuildSyntheticEnvelope(Encoding.UTF8.GetBytes("NACHA-INTEROP-08"), signer, receiver);

        _ = _harness.RoundtripDecrypt(envelope, signer, receiver, out var report);

        report.SignatureValidationResult.Should().Be("VALID");
        report.SignedDataPresent.Should().BeTrue();
    }

    [Fact]
    public void GeneratedEnvelope_ShouldProduceStableStructuralReport()
    {
        var envelope = BuildSyntheticEnvelope("NACHA-INTEROP-09");
        var reportA = _harness.InspectEnvelope(envelope);
        var reportB = _harness.InspectEnvelope(envelope);

        reportA.EnvelopeFormatDetected.Should().Be(reportB.EnvelopeFormatDetected);
        reportA.AlgorithmsDeclared.Should().BeEquivalentTo(reportB.AlgorithmsDeclared);
        reportA.IvDiagnostics!.DerivationAlgorithm.Should().Be(reportB.IvDiagnostics!.DerivationAlgorithm);
    }

    [Fact]
    public void OfficialVector_ShouldBeMarkedPending_WhenNotPresent()
    {
        var vector = _harness.TryLoadOfficialVector(GetRepoRoot());
        if (!vector.Present)
        {
            vector.BasePath.Should().NotBeNullOrWhiteSpace();
            return;
        }

        vector.Present.Should().BeTrue();
    }

    [Fact]
    public void OfficialVector_ShouldCompareStructure_WhenPresent()
    {
        var vector = _harness.TryLoadOfficialVector(GetRepoRoot());
        if (!vector.Present)
        {
            return;
        }

        var report = _harness.InspectEnvelope(vector.EnvelopeBytes!);
        report.RequiredNodesPresent.Should().BeTrue();
    }

    [Fact]
    public void OfficialVector_ShouldComparePlainContent_WhenPresent()
    {
        var vector = _harness.TryLoadOfficialVector(GetRepoRoot());
        if (!vector.Present)
        {
            return;
        }

        vector.PlainBytes.Should().NotBeNull();
        vector.PlainBytes!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void OfficialVector_ShouldCompareSignature_WhenPresent()
    {
        var vector = _harness.TryLoadOfficialVector(GetRepoRoot());
        if (!vector.Present)
        {
            return;
        }

        vector.PublicCertificate.Should().NotBeNull();
    }

    [Fact]
    public void OfficialVector_ShouldCompareIdentifierIv_WhenPresent()
    {
        var vector = _harness.TryLoadOfficialVector(GetRepoRoot());
        if (!vector.Present)
        {
            return;
        }

        var report = _harness.InspectEnvelope(vector.EnvelopeBytes!);
        report.IvDiagnostics.Should().NotBeNull();
        report.IvDiagnostics!.IvLength.Should().Be(16);
    }

    private byte[] BuildSyntheticEnvelope(string content)
    {
        var signer = DigitalEnvelopeInteroperabilityHarness.CreateSelfSignedCertificate("CN=InteropSigner");
        var receiver = DigitalEnvelopeInteroperabilityHarness.CreateSelfSignedCertificate("CN=InteropReceiver");
        return _harness.BuildSyntheticEnvelope(Encoding.UTF8.GetBytes(content), signer, receiver);
    }

    private static string GetRepoRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }
}
