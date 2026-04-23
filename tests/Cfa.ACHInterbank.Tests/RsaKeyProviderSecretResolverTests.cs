using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Implementations;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class RsaKeyProviderSecretResolverTests
{
    [Fact]
    public async Task RsaKeyProvider_ShouldUseCertificateManagementPrivateKey_WhenSecretRefResolvable()
    {
        var cert = CreateSelfSignedCertificate();
        var resolver = new Mock<IDigitalEnvelopeCertificateResolver>(MockBehavior.Strict);
        resolver.Setup(x => x.ResolveAsync("CertSign", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificateResolutionResult(
                true,
                cert,
                5001,
                DigitalEnvelopeCertificateSource.CertificateManagement,
                CertificatePurpose.OutboundSigning,
                cert.Thumbprint,
                cert.SerialNumber,
                cert.Subject,
                null,
                null,
                Array.Empty<string>()));

        var provider = new RsaKeyProvider(resolver.Object);
        var resolved = provider.ObtenerCertificate("CertSign");

        resolved.Should().NotBeNull();
        resolved.HasPrivateKey.Should().BeTrue();
        await Task.CompletedTask;
    }

    [Fact]
    public void RsaKeyProvider_HistoricalDecrypt_ShouldUseHistoricalCertificate_WhenResolvable()
    {
        var cert = CreateSelfSignedCertificate();
        var resolver = new Mock<IDigitalEnvelopeCertificateResolver>(MockBehavior.Strict);
        resolver.Setup(x => x.ResolveHistoricalDecryptAsync(It.IsAny<HistoricalDecryptCertificateCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificateResolutionResult(
                true,
                cert,
                7001,
                DigitalEnvelopeCertificateSource.CertificateManagement,
                CertificatePurpose.InboundDecryption,
                cert.Thumbprint,
                cert.SerialNumber,
                cert.Subject,
                null,
                null,
                Array.Empty<string>()));

        var provider = new RsaKeyProvider(resolver.Object);
        var resolved = provider.ObtenerCertificateForDecrypt("CN=hist", "SER-1");

        resolved.Should().NotBeNull();
        resolved.Thumbprint.Should().Be(cert.Thumbprint);
    }

    [Fact]
    public void RsaKeyProvider_HistoricalDecrypt_ShouldFailClose_WhenCriteriaPresentAndNotResolvable()
    {
        var resolver = new Mock<IDigitalEnvelopeCertificateResolver>(MockBehavior.Strict);
        resolver.Setup(x => x.ResolveHistoricalDecryptAsync(It.IsAny<HistoricalDecryptCertificateCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificateResolutionResult(
                false,
                null,
                null,
                DigitalEnvelopeCertificateSource.CertificateManagement,
                CertificatePurpose.InboundDecryption,
                null,
                null,
                null,
                "HISTORICAL_CERT_NOT_FOUND",
                "missing",
                Array.Empty<string>()));

        var provider = new RsaKeyProvider(resolver.Object);
        var act = () => provider.ObtenerCertificateForDecrypt("CN=hist", "SER-1");
        act.Should().Throw<InvalidOperationException>();
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=RsaProviderTest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }
}
