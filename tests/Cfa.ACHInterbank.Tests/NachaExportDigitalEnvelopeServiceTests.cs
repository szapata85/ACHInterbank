using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaExportDigitalEnvelopeServiceTests
{
    [Fact]
    public async Task EncryptAsync_UsesConfiguredControlledRecipientWhenCycleHasNoCertificate()
    {
        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        var envelope = new Mock<IManagedDigitalEnvelopeService>(MockBehavior.Strict);
        var options = Options.Create(new NachaExportDigitalEnvelopeOptions
        {
            Environment = "Test",
            RecipientPurpose = "InboundDecryption",
            RecipientHolderType = "Participant",
            AllowDefaultClearingHouseFallback = true,
            DefaultClearingHouseId = 1
        });
        selection.Setup(x => x.SelectActiveAsync(2, CertificateEnvironment.Test, CertificatePurpose.InboundDecryption, CertificateHolderType.Participant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateVersionDto?)null);
        selection.Setup(x => x.SelectActiveAsync(1, CertificateEnvironment.Test, CertificatePurpose.InboundDecryption, CertificateHolderType.Participant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCertificate(42));
        envelope.Setup(x => x.EncryptAsync(
                It.Is<ManagedDigitalEnvelopeRequest>(request => request.CertificateVersionId == 42 && request.FileName == "file.OUT"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagedDigitalEnvelopeResult([9], "file.OUT.ENV", "application/octet-stream", 42, "THUMB", "PROFILE"));
        var sut = new NachaExportDigitalEnvelopeService(selection.Object, envelope.Object, options);

        var result = await sut.EncryptAsync(2, "file.OUT", [1, 2, 3], "tester");

        Assert.Equal("file.OUT.ENV", result.FileName);
        selection.VerifyAll();
        envelope.VerifyAll();
    }

    [Fact]
    public async Task EncryptAsync_FailsClosedWhenNoConfiguredCertificateExists()
    {
        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        var envelope = new Mock<IManagedDigitalEnvelopeService>(MockBehavior.Strict);
        selection.Setup(x => x.SelectActiveAsync(2, CertificateEnvironment.Test, CertificatePurpose.OutboundEncryption, CertificateHolderType.ClearingHouse, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateVersionDto?)null);
        var sut = new NachaExportDigitalEnvelopeService(
            selection.Object,
            envelope.Object,
            Options.Create(new NachaExportDigitalEnvelopeOptions()));

        var exception = await Assert.ThrowsAsync<ManagedDigitalEnvelopeException>(
            () => sut.EncryptAsync(2, "file.OUT", [1], "tester"));

        Assert.Equal("CERTIFICATE_NOT_FOUND", exception.ErrorCode);
        envelope.VerifyNoOtherCalls();
    }

    private static CertificateVersionDto BuildCertificate(int id)
        => new(
            id,
            "CFA-INBOUND-DECRYPTION",
            "CFA - Descifrado de entrada",
            1,
            CertificateEnvironment.Test,
            CertificatePurpose.InboundDecryption,
            CertificateHolderType.Participant,
            CertificateStatus.Active,
            1,
            "CN=CFA",
            "CN=CFA",
            "01",
            "THUMB",
            "FINGERPRINT",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            true,
            "RSA",
            2048,
            "SHA256RSA",
            "secret-ref",
            DateTime.UtcNow,
            "bootstrap",
            DateTime.UtcNow,
            null,
            "CFA.pfx");
}
