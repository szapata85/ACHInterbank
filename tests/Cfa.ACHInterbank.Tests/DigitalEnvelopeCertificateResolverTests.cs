using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class DigitalEnvelopeCertificateResolverTests
{
    private static CertificateVersionDto BuildActiveDto(int id = 101, CertificatePurpose purpose = CertificatePurpose.OutboundEncryption)
        => new(
            id,
            "CERT-001",
            "Cert test",
            1,
            CertificateEnvironment.Test,
            purpose,
            CertificateHolderType.ClearingHouse,
            CertificateStatus.Active,
            1,
            "CN=test",
            "CN=test",
            "SERIAL",
            "THUMB",
            "FINGER",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(10),
            false,
            "RSA",
            2048,
            "SHA256",
            null,
            DateTime.UtcNow,
            "tester",
            DateTime.UtcNow,
            null);

    [Fact]
    public async Task Resolver_ShouldUseCertificateManagement_WhenEnabledAndActiveExists()
    {
        using var context = CreateContext(nameof(Resolver_ShouldUseCertificateManagement_WhenEnabledAndActiveExists));
        var rawCert = CreateSelfSignedCertificate().Export(X509ContentType.Cert);
        context.DigitalCertificateVersions.Add(new DigitalCertificateVersion
        {
            Id = 101,
            DigitalCertificateId = 1,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundEncryption,
            HolderType = CertificateHolderType.ClearingHouse,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=test",
            Issuer = "CN=test",
            SerialNumber = "SERIAL",
            Thumbprint = "THUMB",
            FingerprintSha256 = "FINGER",
            NotBefore = DateTime.UtcNow.AddDays(-1),
            NotAfter = DateTime.UtcNow.AddDays(10),
            HasPrivateKey = false,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            RawPublicCertificate = rawCert,
            RowVersion = [1]
        });
        await context.SaveChangesAsync();

        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        selection.Setup(x => x.SelectActiveAsync(1, CertificateEnvironment.Test, CertificatePurpose.OutboundEncryption, CertificateHolderType.ClearingHouse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildActiveDto());
        var legacy = new Mock<IDigitalEnvelopeCertificateRepository>(MockBehavior.Strict);
        var options = Options.Create(new DigitalEnvelopeCertificateOptions
        {
            UseCertificateManagement = true,
            AllowLegacyCertificateFallback = true,
            DefaultClearingHouseId = 1,
            Environment = "Test"
        });

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            legacy.Object,
            new CertificateUsageLoggerService(context),
            context,
            options,
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        var result = await resolver.ResolveAsync("CertCrypt");

        result.Success.Should().BeTrue();
        result.Source.Should().Be(DigitalEnvelopeCertificateSource.CertificateManagement);
        result.CertificateVersionId.Should().Be(101);
    }

    [Fact]
    public async Task Resolver_ShouldFallbackToLegacy_WhenEnabledAndNoActiveAndFallbackAllowed()
    {
        using var context = CreateContext(nameof(Resolver_ShouldFallbackToLegacy_WhenEnabledAndNoActiveAndFallbackAllowed));
        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        selection.Setup(x => x.SelectActiveAsync(It.IsAny<int>(), It.IsAny<CertificateEnvironment>(), It.IsAny<CertificatePurpose>(), It.IsAny<CertificateHolderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateVersionDto?)null);

        var legacyCert = CreateSelfSignedCertificate();
        var legacyRepo = new Mock<IDigitalEnvelopeCertificateRepository>(MockBehavior.Strict);
        legacyRepo.Setup(x => x.GetLatestAsync(DigitalEnvelopeCertificateType.EncryptionPublic, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificate
            {
                Id = 1,
                Type = DigitalEnvelopeCertificateType.EncryptionPublic,
                RawData = legacyCert.Export(X509ContentType.Cert)
            });

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            legacyRepo.Object,
            new CertificateUsageLoggerService(context),
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = true,
                AllowLegacyCertificateFallback = true
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        var result = await resolver.ResolveAsync("CertCrypt");

        result.Success.Should().BeTrue();
        result.Source.Should().Be(DigitalEnvelopeCertificateSource.Legacy);
        context.DigitalEnvelopeOperationLogs.Any(x => x.Result == "FALLBACK_LEGACY").Should().BeTrue();
    }

    [Fact]
    public async Task Resolver_ShouldFail_WhenEnabledAndNoActiveAndFallbackDisabled()
    {
        using var context = CreateContext(nameof(Resolver_ShouldFail_WhenEnabledAndNoActiveAndFallbackDisabled));
        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        selection.Setup(x => x.SelectActiveAsync(It.IsAny<int>(), It.IsAny<CertificateEnvironment>(), It.IsAny<CertificatePurpose>(), It.IsAny<CertificateHolderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateVersionDto?)null);

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            Mock.Of<IDigitalEnvelopeCertificateRepository>(),
            new CertificateUsageLoggerService(context),
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = true,
                AllowLegacyCertificateFallback = false,
                FailIfCertificateManagementUnavailable = false
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        var result = await resolver.ResolveAsync("CertCrypt");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("CERT_MGMT_UNAVAILABLE");
    }

    [Fact]
    public async Task Resolver_ShouldUseLegacy_WhenCertificateManagementDisabled()
    {
        using var context = CreateContext(nameof(Resolver_ShouldUseLegacy_WhenCertificateManagementDisabled));
        var legacyCert = CreateSelfSignedCertificate();
        var legacyRepo = new Mock<IDigitalEnvelopeCertificateRepository>(MockBehavior.Strict);
        legacyRepo.Setup(x => x.GetLatestAsync(DigitalEnvelopeCertificateType.EncryptionPublic, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificate
            {
                Type = DigitalEnvelopeCertificateType.EncryptionPublic,
                RawData = legacyCert.Export(X509ContentType.Cert)
            });

        var resolver = new DigitalEnvelopeCertificateResolver(
            Mock.Of<ICertificateSelectionService>(),
            legacyRepo.Object,
            new CertificateUsageLoggerService(context),
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = false
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        var result = await resolver.ResolveAsync("CertCrypt");
        result.Success.Should().BeTrue();
        result.Source.Should().Be(DigitalEnvelopeCertificateSource.Legacy);
    }

    [Fact]
    public async Task Resolver_ShouldLogUsage_WhenCertificateManagementUsed()
    {
        using var context = CreateContext(nameof(Resolver_ShouldLogUsage_WhenCertificateManagementUsed));
        var rawCert = CreateSelfSignedCertificate().Export(X509ContentType.Cert);
        context.DigitalCertificateVersions.Add(new DigitalCertificateVersion
        {
            Id = 101,
            DigitalCertificateId = 1,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundEncryption,
            HolderType = CertificateHolderType.ClearingHouse,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=test",
            Issuer = "CN=test",
            SerialNumber = "SERIAL",
            Thumbprint = "THUMB",
            FingerprintSha256 = "FINGER",
            NotBefore = DateTime.UtcNow.AddDays(-1),
            NotAfter = DateTime.UtcNow.AddDays(10),
            HasPrivateKey = false,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            RawPublicCertificate = rawCert,
            RowVersion = [1]
        });
        await context.SaveChangesAsync();

        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        selection.Setup(x => x.SelectActiveAsync(It.IsAny<int>(), It.IsAny<CertificateEnvironment>(), It.IsAny<CertificatePurpose>(), It.IsAny<CertificateHolderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildActiveDto());

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            Mock.Of<IDigitalEnvelopeCertificateRepository>(),
            new CertificateUsageLoggerService(context),
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = true,
                AllowLegacyCertificateFallback = true
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        await resolver.ResolveAsync("CertCrypt");

        context.CertificateUsageLogs.Any(x => x.CertificateVersionId == 101 && x.Result == "SUCCESS").Should().BeTrue();
    }

    [Fact]
    public async Task Resolver_ShouldLogWarning_WhenLegacyFallbackUsed()
    {
        using var context = CreateContext(nameof(Resolver_ShouldLogWarning_WhenLegacyFallbackUsed));
        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        selection.Setup(x => x.SelectActiveAsync(It.IsAny<int>(), It.IsAny<CertificateEnvironment>(), It.IsAny<CertificatePurpose>(), It.IsAny<CertificateHolderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateVersionDto?)null);

        var legacyCert = CreateSelfSignedCertificate();
        var legacyRepo = new Mock<IDigitalEnvelopeCertificateRepository>(MockBehavior.Strict);
        legacyRepo.Setup(x => x.GetLatestAsync(DigitalEnvelopeCertificateType.EncryptionPublic, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificate
            {
                Type = DigitalEnvelopeCertificateType.EncryptionPublic,
                RawData = legacyCert.Export(X509ContentType.Cert)
            });

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            legacyRepo.Object,
            new CertificateUsageLoggerService(context),
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = true,
                AllowLegacyCertificateFallback = true,
                LogCertificateSource = true
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        await resolver.ResolveAsync("CertCrypt");

        context.DigitalEnvelopeOperationLogs.Any(x => x.Result == "FALLBACK_LEGACY" && x.ErrorCode == "CERT_MGMT_FALLBACK").Should().BeTrue();
    }

    private static AchDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .Options;
        var context = new AchDbContext(options);
        return context;
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=ResolverTest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }
}
