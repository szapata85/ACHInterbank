using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Implementation;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class ManagedDigitalEnvelopeServiceTests
{
    [Theory]
    [InlineData("archivo.OUT", "archivo.OUT.ENV")]
    [InlineData("0001283.001.20250331.1.OUT", "0001283.001.20250331.1.OUT.ENV")]
    public void BuildEncryptedFileName_AppendsEnvWithoutReplacingExistingExtension(string input, string expected)
        => ManagedDigitalEnvelopeService.BuildEncryptedFileName(input).Should().Be(expected);

    [Theory]
    [InlineData("archivo.OUT.ENV", "archivo.OUT")]
    [InlineData("0001283.001.20250331.1.OUT.env", "0001283.001.20250331.1.OUT")]
    public void BuildDecryptedFileName_RemovesOnlyFinalEnv(string input, string expected)
        => ManagedDigitalEnvelopeService.BuildDecryptedFileName(input).Should().Be(expected);

    [Fact]
    public async Task EncryptDecrypt_WithRealRsaCertificates_RoundtripsByteForByte()
    {
        using var certificate = CreateCertificate("CN=Managed Envelope Roundtrip");
        await using var context = CreateContext();
        AddCertificateVersions(context, certificate, CertificateStatus.Active);
        await context.SaveChangesAsync();
        var service = CreateService(context, certificate);
        var original = "NACHA-M\r\n106-BYTE-RECORD"u8.ToArray();

        var encrypted = await service.EncryptAsync(new ManagedDigitalEnvelopeRequest(2, "archivo.OUT", original, "test"));
        var decrypted = await service.DecryptAsync(new ManagedDigitalEnvelopeRequest(2, encrypted.FileName, encrypted.Content, "test"));

        encrypted.FileName.Should().Be("archivo.OUT.ENV");
        encrypted.Content.Should().NotEqual(original);
        decrypted.FileName.Should().Be("archivo.OUT");
        decrypted.Content.Should().Equal(original);
        SHA256.HashData(decrypted.Content).Should().Equal(SHA256.HashData(original));
    }

    [Theory]
    [InlineData(CertificateStatus.Draft, "CERTIFICATE_INACTIVE")]
    [InlineData(CertificateStatus.Inactive, "CERTIFICATE_INACTIVE")]
    [InlineData(CertificateStatus.Revoked, "CERTIFICATE_INACTIVE")]
    public async Task Encrypt_RejectsNonActiveCertificate(CertificateStatus status, string expectedCode)
    {
        using var certificate = CreateCertificate("CN=Inactive Envelope");
        await using var context = CreateContext();
        AddCertificateVersions(context, certificate, status);
        await context.SaveChangesAsync();
        var service = CreateService(context, certificate);

        var action = () => service.EncryptAsync(new ManagedDigitalEnvelopeRequest(2, "archivo.OUT", [1], "test"));

        var exception = await action.Should().ThrowAsync<ManagedDigitalEnvelopeException>();
        exception.Which.ErrorCode.Should().Be(expectedCode);
    }

    [Fact]
    public async Task Encrypt_RejectsEmptyFile()
    {
        using var certificate = CreateCertificate("CN=Empty Envelope");
        await using var context = CreateContext();
        AddCertificateVersions(context, certificate, CertificateStatus.Active);
        await context.SaveChangesAsync();
        var service = CreateService(context, certificate);

        var action = () => service.EncryptAsync(new ManagedDigitalEnvelopeRequest(2, "archivo.OUT", [], "test"));

        var exception = await action.Should().ThrowAsync<ManagedDigitalEnvelopeException>();
        exception.Which.ErrorCode.Should().Be("FILE_EMPTY");
    }

    [Fact]
    public async Task Decrypt_RejectsCertificateWithoutPrivateKey()
    {
        using var certificate = CreateCertificate("CN=No Private Envelope");
        await using var context = CreateContext();
        AddCertificateVersions(context, certificate, CertificateStatus.Active);
        context.DigitalCertificateVersions.Local.Single(x => x.Id == 2).HasPrivateKey = false;
        await context.SaveChangesAsync();
        var service = CreateService(context, certificate);

        var action = () => service.DecryptAsync(new ManagedDigitalEnvelopeRequest(2, "archivo.OUT.ENV", [1], "test"));

        var exception = await action.Should().ThrowAsync<ManagedDigitalEnvelopeException>();
        exception.Which.ErrorCode.Should().Be("CERTIFICATE_PRIVATE_KEY_REQUIRED");
    }

    [Fact]
    public async Task Decrypt_RejectsWrongCertificateAndTamperedEnvelope()
    {
        using var certificate = CreateCertificate("CN=Envelope Recipient One");
        using var wrongCertificate = CreateCertificate("CN=Envelope Recipient Two");
        await using var context = CreateContext();
        AddCertificateVersions(context, certificate, CertificateStatus.Active);
        AddDecryptVersion(context, wrongCertificate, id: 3, aggregateId: 3);
        await context.SaveChangesAsync();
        var service = CreateService(context, certificate, new Dictionary<int, X509Certificate2> { [3] = wrongCertificate });
        var encrypted = await service.EncryptAsync(new ManagedDigitalEnvelopeRequest(2, "archivo.OUT", [1, 2, 3, 4], "test"));

        var wrongAction = () => service.DecryptAsync(new ManagedDigitalEnvelopeRequest(3, encrypted.FileName, encrypted.Content, "test"));
        var wrongException = await wrongAction.Should().ThrowAsync<ManagedDigitalEnvelopeException>();
        wrongException.Which.ErrorCode.Should().Be("CERTIFICATE_MISMATCH");

        var tampered = encrypted.Content.ToArray();
        tampered[^20] ^= 0x01;
        var tamperedAction = () => service.DecryptAsync(new ManagedDigitalEnvelopeRequest(2, encrypted.FileName, tampered, "test"));
        await tamperedAction.Should().ThrowAsync<ManagedDigitalEnvelopeException>();
    }

    [Fact]
    public async Task ListUsableCertificates_ExcludesExpiredAndInactiveVersions()
    {
        using var certificate = CreateCertificate("CN=List Envelope");
        await using var context = CreateContext();
        AddCertificateVersions(context, certificate, CertificateStatus.Active);
        AddDecryptVersion(context, certificate, id: 3, aggregateId: 3, status: CertificateStatus.Inactive);
        await context.SaveChangesAsync();
        var service = CreateService(context, certificate);

        var result = await service.ListUsableCertificatesAsync();

        result.Should().ContainSingle(x => x.Id == 2 && x.CanEncrypt && x.CanDecrypt);
        result.Should().NotContain(x => x.Id == 3);
    }

    private static ManagedDigitalEnvelopeService CreateService(
        AchDbContext context,
        X509Certificate2 defaultCertificate,
        IReadOnlyDictionary<int, X509Certificate2>? overrides = null)
    {
        var resolver = new Mock<ICertificateSecretResolver>();
        resolver
            .Setup(x => x.ResolveAsync(It.IsAny<CertificateSecretResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateSecretResolutionRequest request, CancellationToken _) =>
            {
                var source = overrides is not null && overrides.TryGetValue(request.CertificateVersionId, out var selected)
                    ? selected
                    : defaultCertificate;
                var copy = ClonePrivateCertificate(source);
                return new CertificateSecretResolutionResult(
                    true,
                    CertificateSecretProviderType.DatabaseEncrypted,
                    new CertificateSecretMaterial(copy, copy.Thumbprint!, copy.SerialNumber, copy.Subject, true),
                    null,
                    null,
                    "****");
            });
        var validator = new DigitalEnvelopeSignatureValidator(Options.Create(new DigitalEnvelopeSignatureValidationOptions()));
        return new ManagedDigitalEnvelopeService(context, resolver.Object, validator, NullLogger<ManagedDigitalEnvelopeService>.Instance);
    }

    private static AchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"managed-envelope-{Guid.NewGuid():N}")
            .Options;
        var context = new AchDbContext(options) { AuditEnabled = false };
        return context;
    }

    private static void AddCertificateVersions(AchDbContext context, X509Certificate2 certificate, CertificateStatus status)
    {
        context.DigitalCertificates.AddRange(
            new DigitalCertificate { Id = 1, Code = "SIGN", DisplayName = "Signing", CreatedBy = "test" },
            new DigitalCertificate { Id = 2, Code = "DECRYPT", DisplayName = "Decrypt", CreatedBy = "test" });
        context.DigitalCertificateVersions.AddRange(
            BuildVersion(certificate, 1, 1, CertificatePurpose.OutboundSigning, status, true),
            BuildVersion(certificate, 2, 2, CertificatePurpose.InboundDecryption, status, true));
    }

    private static void AddDecryptVersion(
        AchDbContext context,
        X509Certificate2 certificate,
        int id,
        int aggregateId,
        CertificateStatus status = CertificateStatus.Active)
    {
        context.DigitalCertificates.Add(new DigitalCertificate { Id = aggregateId, Code = $"DECRYPT-{id}", DisplayName = $"Decrypt {id}", CreatedBy = "test" });
        context.DigitalCertificateVersions.Add(BuildVersion(certificate, id, aggregateId, CertificatePurpose.InboundDecryption, status, true));
    }

    private static DigitalCertificateVersion BuildVersion(
        X509Certificate2 certificate,
        int id,
        int aggregateId,
        CertificatePurpose purpose,
        CertificateStatus status,
        bool hasPrivateKey)
        => new()
        {
            Id = id,
            DigitalCertificateId = aggregateId,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = purpose,
            HolderType = CertificateHolderType.Participant,
            Status = status,
            MaterialType = CertificateMaterialType.PrivateKeyPair,
            VersionNumber = 1,
            FileName = "test.pfx",
            Subject = certificate.Subject,
            Issuer = certificate.Issuer,
            SerialNumber = certificate.SerialNumber,
            Thumbprint = certificate.Thumbprint!,
            FingerprintSha256 = Convert.ToHexString(SHA256.HashData(certificate.RawData)),
            NotBefore = certificate.NotBefore.ToUniversalTime(),
            NotAfter = certificate.NotAfter.ToUniversalTime(),
            HasPrivateKey = hasPrivateKey,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "sha256RSA",
            RawPublicCertificate = certificate.Export(X509ContentType.Cert),
            EncryptedPrivateMaterial = [1],
            PrivateMaterialStorageMode = CertificateStorageMode.DatabaseEncrypted,
            SecretRef = hasPrivateKey ? $"dbenc://{id}" : null,
            UploadedBy = "test"
        };

    private static X509Certificate2 CreateCertificate(string subject)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return ClonePrivateCertificate(generated);
    }

    private static X509Certificate2 ClonePrivateCertificate(X509Certificate2 certificate)
    {
        const string password = "managed-envelope-unit-test";
        var pfx = certificate.Export(X509ContentType.Pfx, password);
        try
        {
            return X509CertificateLoader.LoadPkcs12(pfx, password, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pfx);
        }
    }
}
