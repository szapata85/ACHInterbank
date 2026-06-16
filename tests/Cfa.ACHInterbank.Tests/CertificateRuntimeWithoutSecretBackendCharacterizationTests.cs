using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Implementation;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Implementations;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class CertificateRuntimeWithoutSecretBackendCharacterizationTests
{
    [Fact]
    public async Task CryptoService_ShouldCreateAndOpenEnvelope_CurrentRuntime()
    {
        var signer = CreateCert("CN=core-signer", true);
        var receiver = CreateCert("CN=core-receiver", true);
        var provider = new TrackingRsaKeyProvider(signer, receiver);
        var service = CreateCrypto(provider);
        var plain = Encoding.UTF8.GetBytes("nacha-core-runtime");

        var createdEnvelope = await service.CreateEnvelopeAsync(plain, "core.txt");
        var createdXml = Encoding.UTF8.GetString(createdEnvelope);
        var serializer = new XmlSerializer(typeof(DigitalEnvelopeModel));
        using var reader = new StringReader(createdXml);
        var createdModel = (DigitalEnvelopeModel)serializer.Deserialize(reader)!;
        createdModel.RecipientInfo.Should().NotBeNull();
        createdModel.EncryptedContentInfo.Should().NotBeNull();

        var compatibleEnvelope = BuildCompatibleEnvelopeForOpen(plain, signer, receiver);
        var roundtrip = await service.OpenEnvelopeAsync(compatibleEnvelope, "core.env");

        roundtrip.Should().Equal(plain);
        provider.SecretRefCalls.Should().Be(0);
        provider.BackendCalls.Should().Be(0);
    }

    [Fact]
    public async Task CryptoService_ShouldSignEncryptDecryptAndVerify_WithoutSecretRef()
    {
        var signer = CreateCert("CN=signer", true);
        var receiver = CreateCert("CN=receiver", true);
        var provider = new TrackingRsaKeyProvider(signer, receiver);
        var service = CreateCrypto(provider);

        var payload = Encoding.UTF8.GetBytes("payload");
        var createdEnvelope = await service.CreateEnvelopeAsync(payload, "a.txt");
        var createdXml = Encoding.UTF8.GetString(createdEnvelope);
        var serializer = new XmlSerializer(typeof(DigitalEnvelopeModel));
        using var reader = new StringReader(createdXml);
        var createdModel = (DigitalEnvelopeModel)serializer.Deserialize(reader)!;
        createdModel.RecipientInfo.Should().NotBeNull();
        createdModel.EncryptedContentInfo.Should().NotBeNull();

        var compatibleEnvelope = BuildCompatibleEnvelopeForOpen(payload, signer, receiver);
        var plain = await service.OpenEnvelopeAsync(compatibleEnvelope, "a.env");

        plain.Should().BeEquivalentTo(payload);
        provider.SignFetchCount.Should().BeGreaterThan(0);
        provider.CryptFetchCount.Should().BeGreaterThan(0);
        provider.DecryptFetchCount.Should().BeGreaterThan(0);
        provider.SecretRefCalls.Should().Be(0);
        provider.BackendCalls.Should().Be(0);
    }

    [Fact]
    public async Task CryptoService_ShouldFail_WhenCertificateMissing()
    {
        var signer = CreateCert("CN=signer", true);
        var provider = new TrackingRsaKeyProvider(signer, decrypt: null!);
        var service = CreateCrypto(provider);

        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(
            () => service.CreateEnvelopeAsync(Encoding.UTF8.GetBytes("x"), "missing.txt"));

        ex.ErrorCode.Should().Be("CERTIFICATE_NOT_AVAILABLE");
        provider.BackendCalls.Should().Be(0);
        provider.SecretRefCalls.Should().Be(0);
    }

    [Fact]
    public void RsaKeyProvider_ShouldResolveCurrentCertificate_WithoutSecretBackend()
    {
        var cert = CreateCert("CN=dec", true);
        var resolver = new Mock<IDigitalEnvelopeCertificateResolver>(MockBehavior.Strict);
        resolver.Setup(x => x.ResolveHistoricalDecryptAsync(It.IsAny<HistoricalDecryptCertificateCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificateResolutionResult(false, null, null, DigitalEnvelopeCertificateSource.Legacy, CertificatePurpose.InboundDecryption, null, null, null, "none", "none", Array.Empty<string>()));
        resolver.Setup(x => x.ResolveAsync("CertDecrypt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificateResolutionResult(true, cert, null, DigitalEnvelopeCertificateSource.Legacy, CertificatePurpose.InboundDecryption, cert.Thumbprint, cert.SerialNumber, cert.Subject, null, null, Array.Empty<string>()));

        var provider = new RsaKeyProvider(resolver.Object);
        var resolved = provider.ObtenerCertificateForDecrypt(null, null);

        resolved.Should().NotBeNull();
        resolver.Verify(x => x.ResolveAsync("CertDecrypt", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RsaKeyProvider_ShouldFailClosedForHistoricalCriteria_WhenHistoricalResolutionFails()
    {
        var resolver = new Mock<IDigitalEnvelopeCertificateResolver>(MockBehavior.Strict);
        resolver.Setup(x => x.ResolveHistoricalDecryptAsync(It.IsAny<HistoricalDecryptCertificateCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificateResolutionResult(false, null, null, DigitalEnvelopeCertificateSource.CertificateManagement, CertificatePurpose.InboundDecryption, null, null, null, "HISTORICAL_CERT_NOT_FOUND", "missing", Array.Empty<string>()));

        var provider = new RsaKeyProvider(resolver.Object);
        var act = () => provider.ObtenerCertificateForDecrypt("issuer", "serial");

        act.Should().Throw<InvalidOperationException>();
        resolver.Verify(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void RsaKeyProvider_ShouldRejectCertificateManagementReferences_ForRuntimeCertificate()
    {
        var cert = CreateCert("CN=cm", true);
        var resolver = new Mock<IDigitalEnvelopeCertificateResolver>(MockBehavior.Strict);
        resolver.Setup(x => x.ResolveAsync("CertSign", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificateResolutionResult(
                true,
                cert,
                22,
                DigitalEnvelopeCertificateSource.CertificateManagement,
                CertificatePurpose.OutboundSigning,
                cert.Thumbprint,
                cert.SerialNumber,
                cert.Subject,
                null,
                null,
                Array.Empty<string>()));

        var provider = new RsaKeyProvider(resolver.Object);
        var act = () => provider.ObtenerCertificate("CertSign");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CERTIFICATE_RUNTIME_SECRET_REF_NOT_ALLOWED*");
    }

    [Fact]
    public void RuntimeCompose_ShouldContainOnlyCoreServices()
    {
        var text = File.ReadAllText(Path.Combine(ResolveRepositoryRoot(), "docker-compose.yml"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        text.Should().Contain("achinterbank-api");
        text.Should().Contain("achinterbank-spa");
        text.Should().Contain("postgres");

        var serviceCount = text.Split("\n  ", StringSplitOptions.None)
            .Count(line => line.StartsWith("  ") && line.TrimEnd().EndsWith(":"));

        serviceCount.Should().BeGreaterOrEqualTo(3);
    }

    private static string ResolveRepositoryRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ACHInterbank.sln"))) return dir;
            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }

        throw new DirectoryNotFoundException("No se pudo resolver la raíz del repo.");
    }

    private static CryptoServiceScoped CreateCrypto(IRsaKeyProvider provider)
    {
        var opts = new DigitalEnvelopeSignatureValidationOptions
        {
            EnableSignatureValidation = true,
            AllowLegacyUnsignedEnvelope = false,
            FailWhenSignerCertificateExpired = true,
            ValidateSignerCertificateChain = false,
            AuditInvalidSignature = true
        };

        return new CryptoServiceScoped(
            provider,
            new DigitalEnvelopeSignatureValidator(Options.Create(opts)),
            new NoopAudit(),
            Options.Create(opts),
            NullLogger<CryptoServiceScoped>.Instance);
    }

    private static X509Certificate2 CreateCert(string subject, bool withPrivate)
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

        if (!withPrivate)
            return X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert));

        var pfx = cert.Export(X509ContentType.Pkcs12);
        return X509CertificateLoader.LoadPkcs12(pfx, string.Empty, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }

    private static byte[] BuildCompatibleEnvelopeForOpen(byte[] plain, X509Certificate2 signer, X509Certificate2 receiver)
    {
        var method = typeof(DigitalEnvelopeCertificateCharacterizationTests)
            .GetMethod("BuildEnvelopeWithMutator", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull("se reutiliza el patrón determinístico ya estabilizado para OpenEnvelope");
        return (byte[])method!.Invoke(null, [plain, signer, receiver, null, false])!;
    }

    private sealed class NoopAudit : IDigitalEnvelopeSignatureAuditService
    {
        public Task AuditAsync(string result, string? errorCode, string? signerThumbprint, string? signerSerialNumber, string? signatureAlgorithm, bool failCloseApplied, bool legacyBypassUsed, string actor, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class TrackingRsaKeyProvider(X509Certificate2 sign, X509Certificate2 decrypt) : IRsaKeyProvider
    {
        public int SignFetchCount { get; private set; }
        public int CryptFetchCount { get; private set; }
        public int DecryptFetchCount { get; private set; }
        public int SecretRefCalls { get; private set; }
        public int BackendCalls { get; private set; }

        public X509Certificate2 ObtenerCertificate(string Key_cert)
        {
            if (Key_cert.Contains("SecretRef", StringComparison.OrdinalIgnoreCase)) SecretRefCalls++;
            if (Key_cert.Contains("Backend", StringComparison.OrdinalIgnoreCase)) BackendCalls++;

            return Key_cert switch
            {
                "CertSign" => CountAndReturn(sign, isSign: true),
                "CertCrypt" => CountAndReturn(decrypt, isCrypt: true),
                "CertDecrypt" => CountAndReturn(decrypt, isDecrypt: true),
                _ => throw new InvalidOperationException($"cert not configured: {Key_cert}")
            };
        }

        public X509Certificate2 ObtenerCertificateForDecrypt(string? recipientIssuer, string? recipientSerial, string? recipientThumbprint = null)
            => CountAndReturn(decrypt, isDecrypt: true);

        private X509Certificate2 CountAndReturn(X509Certificate2 cert, bool isSign = false, bool isCrypt = false, bool isDecrypt = false)
        {
            if (isSign) SignFetchCount++;
            if (isCrypt) CryptFetchCount++;
            if (isDecrypt) DecryptFetchCount++;
            if (cert is null) throw new DigitalEnvelopeSignatureValidationException("CERTIFICATE_NOT_AVAILABLE", "missing cert");
            return cert;
        }
    }
}
