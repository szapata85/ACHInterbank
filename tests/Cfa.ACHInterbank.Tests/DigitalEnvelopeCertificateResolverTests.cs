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
            Mock.Of<ICertificateSecretResolver>(),
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
            Mock.Of<ICertificateSecretResolver>(),
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
            Mock.Of<ICertificateSecretResolver>(),
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
            Mock.Of<ICertificateSecretResolver>(),
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
            Mock.Of<ICertificateSecretResolver>(),
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
            Mock.Of<ICertificateSecretResolver>(),
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

    [Fact]
    public async Task CertificateResolver_ShouldUsePrivateCertificateFromCertificateManagement_WhenSecretRefResolvable()
    {
        using var context = CreateContext(nameof(CertificateResolver_ShouldUsePrivateCertificateFromCertificateManagement_WhenSecretRefResolvable));
        context.DigitalCertificateVersions.Add(new DigitalCertificateVersion
        {
            Id = 555,
            DigitalCertificateId = 1,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundSigning,
            HolderType = CertificateHolderType.Participant,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=secret",
            Issuer = "CN=secret",
            SerialNumber = "S1",
            Thumbprint = "T1",
            FingerprintSha256 = "F1",
            NotBefore = DateTime.UtcNow.AddDays(-1),
            NotAfter = DateTime.UtcNow.AddDays(10),
            HasPrivateKey = true,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            PrivateMaterialStorageMode = CertificateStorageMode.ExternalSecretReference,
            SecretRef = "inmem://sign/private-001",
            RowVersion = [1]
        });
        await context.SaveChangesAsync();

        var cert = CreateSelfSignedCertificate();
        var pfx = cert.Export(X509ContentType.Pkcs12, "test-pass");
        var secretResult = new CertificateSecretResolutionResult(
            true,
            CertificateSecretProviderType.InMemory,
            new CertificateSecretMaterial(cert, cert.Thumbprint ?? string.Empty, cert.SerialNumber, cert.Subject, true),
            null,
            null,
            "****e-001");

        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        selection.Setup(x => x.SelectActiveAsync(1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateVersionDto(
                555, "CERT-1", "CERT-1", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant,
                CertificateStatus.Active, 1, cert.Subject, cert.Issuer, cert.SerialNumber, cert.Thumbprint ?? string.Empty, "F1",
                cert.NotBefore, cert.NotAfter, true, "RSA", 2048, "SHA256", "inmem://sign/private-001", DateTime.UtcNow, "tester", DateTime.UtcNow, null));

        var secretResolver = new Mock<ICertificateSecretResolver>(MockBehavior.Strict);
        secretResolver.Setup(x => x.ResolveAsync(It.IsAny<CertificateSecretResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secretResult);

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            Mock.Of<IDigitalEnvelopeCertificateRepository>(),
            new CertificateUsageLoggerService(context),
            secretResolver.Object,
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = true,
                AllowLegacyCertificateFallback = true,
                Environment = "Test",
                DefaultClearingHouseId = 1
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        var result = await resolver.ResolveAsync("CertSign");

        result.Success.Should().BeTrue();
        result.Source.Should().Be(DigitalEnvelopeCertificateSource.CertificateManagement);
        result.Certificate.Should().NotBeNull();
        result.Certificate!.HasPrivateKey.Should().BeTrue();
    }

    [Fact]
    public async Task CertificateResolver_ShouldFallbackLegacy_WhenSecretRefFailsAndFallbackAllowed()
    {
        using var context = CreateContext(nameof(CertificateResolver_ShouldFallbackLegacy_WhenSecretRefFailsAndFallbackAllowed));
        context.DigitalCertificateVersions.Add(new DigitalCertificateVersion
        {
            Id = 556,
            DigitalCertificateId = 1,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundSigning,
            HolderType = CertificateHolderType.Participant,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=secret",
            Issuer = "CN=secret",
            SerialNumber = "S2",
            Thumbprint = "T2",
            FingerprintSha256 = "F2",
            NotBefore = DateTime.UtcNow.AddDays(-1),
            NotAfter = DateTime.UtcNow.AddDays(10),
            HasPrivateKey = true,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            PrivateMaterialStorageMode = CertificateStorageMode.ExternalSecretReference,
            SecretRef = "inmem://sign/private-002",
            RawPublicCertificate = CreateSelfSignedCertificate().Export(X509ContentType.Cert),
            RowVersion = [1]
        });
        await context.SaveChangesAsync();

        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        var cert = CreateSelfSignedCertificate();
        selection.Setup(x => x.SelectActiveAsync(1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateVersionDto(
                556, "CERT-2", "CERT-2", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant,
                CertificateStatus.Active, 1, cert.Subject, cert.Issuer, cert.SerialNumber, cert.Thumbprint ?? string.Empty, "F2",
                cert.NotBefore, cert.NotAfter, true, "RSA", 2048, "SHA256", "inmem://sign/private-002", DateTime.UtcNow, "tester", DateTime.UtcNow, null));

        var secretResolver = new Mock<ICertificateSecretResolver>(MockBehavior.Strict);
        secretResolver.Setup(x => x.ResolveAsync(It.IsAny<CertificateSecretResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateSecretResolutionResult(false, CertificateSecretProviderType.InMemory, null, "SECRET_NOT_FOUND", "No secret", "****e-002"));

        var legacyPrivate = CreateSelfSignedCertificate();
        var legacyRepo = new Mock<IDigitalEnvelopeCertificateRepository>(MockBehavior.Strict);
        legacyRepo.Setup(x => x.GetLatestAsync(DigitalEnvelopeCertificateType.SigningKeyPair, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeCertificate
            {
                Type = DigitalEnvelopeCertificateType.SigningKeyPair,
                RawData = legacyPrivate.Export(X509ContentType.Pkcs12, "legacy-pass"),
                Password = "legacy-pass"
            });

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            legacyRepo.Object,
            new CertificateUsageLoggerService(context),
            secretResolver.Object,
            context,
            Options.Create(new DigitalEnvelopeCertificateOptions
            {
                UseCertificateManagement = true,
                AllowLegacyCertificateFallback = true,
                Environment = "Test",
                DefaultClearingHouseId = 1
            }),
            NullLogger<DigitalEnvelopeCertificateResolver>.Instance);

        var result = await resolver.ResolveAsync("CertSign");
        result.Success.Should().BeTrue();
        result.Source.Should().Be(DigitalEnvelopeCertificateSource.Legacy);
    }

    [Fact]
    public async Task CertificateResolver_ShouldFail_WhenSecretRefFailsAndFallbackDisabled()
    {
        using var context = CreateContext(nameof(CertificateResolver_ShouldFail_WhenSecretRefFailsAndFallbackDisabled));
        context.DigitalCertificateVersions.Add(new DigitalCertificateVersion
        {
            Id = 557,
            DigitalCertificateId = 1,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundSigning,
            HolderType = CertificateHolderType.Participant,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=secret",
            Issuer = "CN=secret",
            SerialNumber = "S3",
            Thumbprint = "T3",
            FingerprintSha256 = "F3",
            NotBefore = DateTime.UtcNow.AddDays(-1),
            NotAfter = DateTime.UtcNow.AddDays(10),
            HasPrivateKey = true,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            PrivateMaterialStorageMode = CertificateStorageMode.ExternalSecretReference,
            SecretRef = "inmem://sign/private-003",
            RawPublicCertificate = CreateSelfSignedCertificate().Export(X509ContentType.Cert),
            RowVersion = [1]
        });
        await context.SaveChangesAsync();

        var selection = new Mock<ICertificateSelectionService>(MockBehavior.Strict);
        var cert = CreateSelfSignedCertificate();
        selection.Setup(x => x.SelectActiveAsync(1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateVersionDto(
                557, "CERT-3", "CERT-3", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant,
                CertificateStatus.Active, 1, cert.Subject, cert.Issuer, cert.SerialNumber, cert.Thumbprint ?? string.Empty, "F3",
                cert.NotBefore, cert.NotAfter, true, "RSA", 2048, "SHA256", "inmem://sign/private-003", DateTime.UtcNow, "tester", DateTime.UtcNow, null));

        var secretResolver = new Mock<ICertificateSecretResolver>(MockBehavior.Strict);
        secretResolver.Setup(x => x.ResolveAsync(It.IsAny<CertificateSecretResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateSecretResolutionResult(false, CertificateSecretProviderType.InMemory, null, "SECRET_NOT_FOUND", "No secret", "****e-003"));

        var resolver = new DigitalEnvelopeCertificateResolver(
            selection.Object,
            Mock.Of<IDigitalEnvelopeCertificateRepository>(),
            new CertificateUsageLoggerService(context),
            secretResolver.Object,
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
        result.ErrorCode.Should().Be("CERT_MGMT_UNAVAILABLE");
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
