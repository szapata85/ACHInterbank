using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
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

public class HistoricalDecryptPolicyTests
{
    [Fact]
    public async Task HistoricalDecrypt_ExpiredButRetained_ShouldSucceed_AndAuditHistoricalUsage()
    {
        using var context = CreateContext(nameof(HistoricalDecrypt_ExpiredButRetained_ShouldSucceed_AndAuditHistoricalUsage));
        var version = await SeedVersionAsync(context, CertificateStatus.Expired, "SER-OLD", "CN=old");
        var cert = CreateSelfSignedCertificate("CN=old");

        var resolver = BuildResolver(context, cert, success: true, new DigitalEnvelopeCertificateOptions
        {
            UseCertificateManagement = true,
            AllowLegacyCertificateFallback = false,
            Environment = "Test",
            DefaultClearingHouseId = 1,
            AllowHistoricalDecryptWhenExpired = true,
            AllowHistoricalDecryptWhenRevoked = false
        });

        var result = await resolver.ResolveHistoricalDecryptAsync(new HistoricalDecryptCertificateCriteria(version.Issuer, version.SerialNumber, version.Thumbprint, "test-user"));

        result.Success.Should().BeTrue();
        result.CertificateVersionId.Should().Be(version.Id);
        context.CertificateUsageLogs.Any(x => x.CertificateVersionId == version.Id && x.OperationType == "HistoricalDecrypt" && x.Result == "SUCCESS").Should().BeTrue();
    }

    [Fact]
    public async Task ExpiredCertificate_CannotSignNewOutbound()
    {
        using var context = CreateContext(nameof(ExpiredCertificate_CannotSignNewOutbound));
        var cert = CreateSelfSignedCertificate("CN=dummy");
        var selection = new Mock<ICertificateSelectionService>();
        selection.Setup(x => x.SelectActiveAsync(It.IsAny<int>(), It.IsAny<CertificateEnvironment>(), CertificatePurpose.OutboundSigning, It.IsAny<CertificateHolderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateVersionDto?)null);

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            Mock.Of<IDigitalEnvelopeCertificateRepository>(),
            new CertificateUsageLoggerService(context),
            Mock.Of<ICertificateSecretResolver>(),
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = true,
                AllowLegacyCertificateFallback = false,
                Environment = "Test",
                DefaultClearingHouseId = 1
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        var result = await resolver.ResolveAsync("CertSign");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExpiredCertificate_CannotEncryptNewOutbound()
    {
        using var context = CreateContext(nameof(ExpiredCertificate_CannotEncryptNewOutbound));
        var selection = new Mock<ICertificateSelectionService>();
        selection.Setup(x => x.SelectActiveAsync(It.IsAny<int>(), It.IsAny<CertificateEnvironment>(), CertificatePurpose.OutboundEncryption, It.IsAny<CertificateHolderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateVersionDto?)null);

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            Mock.Of<IDigitalEnvelopeCertificateRepository>(),
            new CertificateUsageLoggerService(context),
            Mock.Of<ICertificateSecretResolver>(),
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = true,
                AllowLegacyCertificateFallback = false,
                Environment = "Test",
                DefaultClearingHouseId = 1
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        var result = await resolver.ResolveAsync("CertCrypt");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HistoricalDecrypt_ShouldFail_WhenSecretRefCannotResolve()
    {
        using var context = CreateContext(nameof(HistoricalDecrypt_ShouldFail_WhenSecretRefCannotResolve));
        var version = await SeedVersionAsync(context, CertificateStatus.Expired, "SER-FAIL", "CN=fail");
        var resolver = BuildResolver(context, CreateSelfSignedCertificate("CN=x"), success: false, new DigitalEnvelopeCertificateOptions
        {
            UseCertificateManagement = true,
            AllowHistoricalDecryptWhenExpired = true,
            AllowHistoricalDecryptWhenRevoked = false,
            Environment = "Test",
            DefaultClearingHouseId = 1
        });

        var result = await resolver.ResolveHistoricalDecryptAsync(new HistoricalDecryptCertificateCriteria(version.Issuer, version.SerialNumber, version.Thumbprint, "test-user"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HistoricalDecrypt_ShouldUseHistoricalVersion_NotActiveReplacement()
    {
        using var context = CreateContext(nameof(HistoricalDecrypt_ShouldUseHistoricalVersion_NotActiveReplacement));
        var oldVersion = await SeedVersionAsync(context, CertificateStatus.Expired, "SER-HIST", "CN=hist");
        await SeedVersionAsync(context, CertificateStatus.Active, "SER-NEW", "CN=new");

        var cert = CreateSelfSignedCertificate("CN=hist");
        var resolver = BuildResolver(context, cert, success: true, new DigitalEnvelopeCertificateOptions
        {
            UseCertificateManagement = true,
            AllowHistoricalDecryptWhenExpired = true,
            AllowHistoricalDecryptWhenRevoked = false,
            Environment = "Test",
            DefaultClearingHouseId = 1
        });

        var result = await resolver.ResolveHistoricalDecryptAsync(new HistoricalDecryptCertificateCriteria(oldVersion.Issuer, oldVersion.SerialNumber, null, "test-user"));

        result.Success.Should().BeTrue();
        result.CertificateVersionId.Should().Be(oldVersion.Id);
    }

    [Fact]
    public async Task HistoricalDecrypt_Revoked_ShouldFail_WhenPolicyDisallows()
    {
        using var context = CreateContext(nameof(HistoricalDecrypt_Revoked_ShouldFail_WhenPolicyDisallows));
        var version = await SeedVersionAsync(context, CertificateStatus.Revoked, "SER-REV", "CN=rev");
        var resolver = BuildResolver(context, CreateSelfSignedCertificate("CN=rev"), success: true, new DigitalEnvelopeCertificateOptions
        {
            UseCertificateManagement = true,
            AllowHistoricalDecryptWhenExpired = true,
            AllowHistoricalDecryptWhenRevoked = false,
            Environment = "Test",
            DefaultClearingHouseId = 1
        });

        var result = await resolver.ResolveHistoricalDecryptAsync(new HistoricalDecryptCertificateCriteria(version.Issuer, version.SerialNumber, version.Thumbprint, "test-user"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("HISTORICAL_DECRYPT_REVOKED_FORBIDDEN");
    }

    private static DigitalEnvelopeCertificateResolver BuildResolver(AchDbContext context, X509Certificate2 cert, bool success, DigitalEnvelopeCertificateOptions options)
    {
        var secretResolver = new Mock<ICertificateSecretResolver>();
        secretResolver.Setup(x => x.ResolveAsync(It.IsAny<CertificateSecretResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success
                ? new CertificateSecretResolutionResult(true, CertificateSecretProviderType.ExternalSecretReference, new CertificateSecretMaterial(cert, cert.Thumbprint ?? string.Empty, cert.SerialNumber, cert.Subject, true), null, null, "****hist")
                : new CertificateSecretResolutionResult(false, CertificateSecretProviderType.ExternalSecretReference, null, "SECRET_PROVIDER_NOT_CONFIGURED", "failed", "****hist"));

        return new DigitalEnvelopeCertificateResolver(
            Mock.Of<ICertificateSelectionService>(),
            Mock.Of<IDigitalEnvelopeCertificateRepository>(),
            new CertificateUsageLoggerService(context),
            secretResolver.Object,
            context,
            Options.Create(options),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);
    }

    private static async Task<DigitalCertificateVersion> SeedVersionAsync(AchDbContext context, CertificateStatus status, string serial, string subject)
    {
        var certAggregate = new DigitalCertificate { Code = Guid.NewGuid().ToString("N"), DisplayName = "cert" };
        context.DigitalCertificates.Add(certAggregate);
        await context.SaveChangesAsync();

        var row = new DigitalCertificateVersion
        {
            DigitalCertificateId = certAggregate.Id,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.InboundDecryption,
            HolderType = CertificateHolderType.Participant,
            Status = status,
            VersionNumber = 1,
            Subject = subject,
            Issuer = subject,
            SerialNumber = serial,
            Thumbprint = $"TP-{serial}",
            FingerprintSha256 = "F",
            NotBefore = DateTime.UtcNow.AddYears(-1),
            NotAfter = DateTime.UtcNow.AddDays(-1),
            HasPrivateKey = true,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            PrivateMaterialStorageMode = CertificateStorageMode.ExternalSecretReference,
            SecretRef = $"kv://certificates/test/ch-1/inbounddecryption/v{serial}",
            RowVersion = [1]
        };

        context.DigitalCertificateVersions.Add(row);
        await context.SaveChangesAsync();
        return row;
    }

    private static AchDbContext CreateContext(string dbName)
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(dbName).Options);

    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }
}
