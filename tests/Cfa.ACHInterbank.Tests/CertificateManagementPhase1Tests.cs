using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class CertificateManagementPhase1Tests
{
    private static AchDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AchDbContext(options);
    }

    private static (byte[] Cer, byte[] Pfx) CreateTestCertificate(string subject = "CN=Test Cert")
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var pfx = cert.Export(X509ContentType.Pkcs12, "test-pass");
        var cer = cert.Export(X509ContentType.Cert);
        return (cer, pfx);
    }

    [Fact]
    public async Task LoadPublicCertificate_ShouldExtractMetadata()
    {
        using var context = CreateContext(nameof(LoadPublicCertificate_ShouldExtractMetadata));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService(), new FakeStore());
        var (cer, _) = CreateTestCertificate();

        var result = await service.LoadPublicCertificateAsync(new LoadPublicCertificateRequest(
            "CERT-PUB", "Public", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundEncryption, CertificateHolderType.ClearingHouse, cer, "tester"));

        result.Subject.Should().Contain("CN=Test Cert");
        result.Thumbprint.Should().NotBeNullOrWhiteSpace();
        result.FingerprintSha256.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterPrivateCertificate_ShouldExtractMetadataWithoutPersistingPassword()
    {
        using var context = CreateContext(nameof(RegisterPrivateCertificate_ShouldExtractMetadataWithoutPersistingPassword));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService(), new FakeStore());
        var (_, pfx) = CreateTestCertificate("CN=Private Test");

        var result = await service.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            "CERT-PRV", "Private", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, pfx, "test-pass", "tester", CertificateStorageMode.ExternalSecretReference, "kv://cert/001"));

        result.HasPrivateKey.Should().BeTrue();
        context.DigitalCertificateVersions.Single().SecretRef.Should().Be("kv://cert/001");
    }

    [Fact]
    public async Task RegisterPrivateCertificate_ShouldRejectInvalidPassword()
    {
        using var context = CreateContext(nameof(RegisterPrivateCertificate_ShouldRejectInvalidPassword));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService(), new FakeStore());
        var (_, pfx) = CreateTestCertificate();

        var act = () => service.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            "CERT-PRV", "Private", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, pfx, "wrong", "tester", CertificateStorageMode.ExternalSecretReference, "kv://cert/001"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RegisterPrivateCertificate_OpenBaoMode_ShouldGenerateAndPersistSecretRef()
    {
        using var context = CreateContext(nameof(RegisterPrivateCertificate_OpenBaoMode_ShouldGenerateAndPersistSecretRef));
        var store = new FakeStore();
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService(), store);
        var (_, pfx) = CreateTestCertificate("CN=OpenBao Test");

        var result = await service.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            "CERT-OB", "Private", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, pfx, "test-pass", "tester", CertificateStorageMode.OpenBaoReference, null));

        result.SecretRef.Should().StartWith("openbao://");
        context.DigitalCertificateVersions.Single().PrivateMaterialStorageMode.Should().Be(CertificateStorageMode.OpenBaoReference);
    }

    [Fact]
    public async Task ActivateVersion_ShouldRejectExpiredCertificate()
    {
        using var context = CreateContext(nameof(ActivateVersion_ShouldRejectExpiredCertificate));
        var cert = new DigitalCertificate { Code = "A", DisplayName = "A" };
        context.DigitalCertificates.Add(cert);
        await context.SaveChangesAsync();

        context.DigitalCertificateVersions.Add(new DigitalCertificateVersion
        {
            DigitalCertificateId = cert.Id,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundSigning,
            HolderType = CertificateHolderType.Participant,
            Status = CertificateStatus.Draft,
            VersionNumber = 1,
            Subject = "CN=a",
            Issuer = "CN=a",
            SerialNumber = "1",
            Thumbprint = "t",
            FingerprintSha256 = "f",
            NotBefore = DateTime.UtcNow.AddDays(-10),
            NotAfter = DateTime.UtcNow.AddDays(-1),
            HasPrivateKey = true,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256"
        });
        await context.SaveChangesAsync();

        var service = new CertificateActivationService(context, new CertificateValidationService(context));
        var act = () => service.ActivateVersionAsync(new ActivateCertificateVersionRequest(context.DigitalCertificateVersions.Single().Id, "tester"));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ActivateVersion_ShouldRequirePrivateKeyForOutboundSigning()
    {
        using var context = CreateContext(nameof(ActivateVersion_ShouldRequirePrivateKeyForOutboundSigning));
        var cert = new DigitalCertificate { Code = "A", DisplayName = "A" };
        context.DigitalCertificates.Add(cert);
        await context.SaveChangesAsync();

        context.DigitalCertificateVersions.Add(new DigitalCertificateVersion
        {
            DigitalCertificateId = cert.Id,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundSigning,
            HolderType = CertificateHolderType.Participant,
            Status = CertificateStatus.Draft,
            VersionNumber = 1,
            Subject = "CN=a",
            Issuer = "CN=a",
            SerialNumber = "1",
            Thumbprint = "t",
            FingerprintSha256 = "f",
            NotBefore = DateTime.UtcNow.AddDays(-1),
            NotAfter = DateTime.UtcNow.AddDays(1),
            HasPrivateKey = false,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256"
        });
        await context.SaveChangesAsync();

        var service = new CertificateActivationService(context, new CertificateValidationService(context));
        var act = () => service.ActivateVersionAsync(new ActivateCertificateVersionRequest(context.DigitalCertificateVersions.Single().Id, "tester"));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ActivateVersion_ShouldReplacePreviousActiveVersion()
    {
        using var context = CreateContext(nameof(ActivateVersion_ShouldReplacePreviousActiveVersion));
        var cert = new DigitalCertificate { Code = "A", DisplayName = "A" };
        context.DigitalCertificates.Add(cert);
        await context.SaveChangesAsync();

        var baseVersion = new DigitalCertificateVersion
        {
            DigitalCertificateId = cert.Id,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundSigning,
            HolderType = CertificateHolderType.Participant,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=a",
            Issuer = "CN=a",
            SerialNumber = "1",
            Thumbprint = "t1",
            FingerprintSha256 = "f1",
            NotBefore = DateTime.UtcNow.AddDays(-2),
            NotAfter = DateTime.UtcNow.AddDays(5),
            HasPrivateKey = true,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            SecretRef = "kv://a"
        };
        var newVersion = new DigitalCertificateVersion
        {
            DigitalCertificateId = cert.Id,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundSigning,
            HolderType = CertificateHolderType.Participant,
            Status = CertificateStatus.Draft,
            VersionNumber = 2,
            Subject = "CN=b",
            Issuer = "CN=b",
            SerialNumber = "2",
            Thumbprint = "t2",
            FingerprintSha256 = "f2",
            NotBefore = DateTime.UtcNow.AddDays(-1),
            NotAfter = DateTime.UtcNow.AddDays(10),
            HasPrivateKey = true,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            SecretRef = "kv://b"
        };
        context.DigitalCertificateVersions.AddRange(baseVersion, newVersion);
        await context.SaveChangesAsync();

        var service = new CertificateActivationService(context, new CertificateValidationService(context));
        await service.ActivateVersionAsync(new ActivateCertificateVersionRequest(newVersion.Id, "tester"));

        context.DigitalCertificateVersions.Single(x => x.Id == baseVersion.Id).Status.Should().Be(CertificateStatus.Replaced);
        context.DigitalCertificateVersions.Single(x => x.Id == newVersion.Id).Status.Should().Be(CertificateStatus.Active);
        context.CertificateRotationHistories.Should().ContainSingle();
    }

    [Fact]
    public async Task Selection_ShouldReturnActiveVersionByContext()
    {
        using var context = CreateContext(nameof(Selection_ShouldReturnActiveVersionByContext));
        var cert = new DigitalCertificate { Code = "A", DisplayName = "A" };
        context.DigitalCertificates.Add(cert);
        await context.SaveChangesAsync();
        context.DigitalCertificateVersions.Add(new DigitalCertificateVersion
        {
            DigitalCertificateId = cert.Id,
            ClearingHouseId = 7,
            Environment = CertificateEnvironment.Production,
            Purpose = CertificatePurpose.OutboundEncryption,
            HolderType = CertificateHolderType.ClearingHouse,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=a", Issuer = "CN=a", SerialNumber = "1", Thumbprint = "t", FingerprintSha256 = "f",
            NotBefore = DateTime.UtcNow.AddDays(-1), NotAfter = DateTime.UtcNow.AddDays(3), HasPrivateKey = false,
            KeyAlgorithm = "RSA", KeySize = 2048, SignatureAlgorithm = "SHA256"
        });
        await context.SaveChangesAsync();

        var service = new CertificateSelectionService(context);
        var result = await service.SelectActiveAsync(7, CertificateEnvironment.Production, CertificatePurpose.OutboundEncryption, CertificateHolderType.ClearingHouse);

        result.Should().NotBeNull();
        result!.Status.Should().Be(CertificateStatus.Active);
    }

    [Fact]
    public async Task RevokeVersion_ShouldChangeStatusAndAudit()
    {
        using var context = CreateContext(nameof(RevokeVersion_ShouldChangeStatusAndAudit));
        var cert = new DigitalCertificate { Code = "A", DisplayName = "A" };
        context.DigitalCertificates.Add(cert);
        await context.SaveChangesAsync();
        var version = new DigitalCertificateVersion
        {
            DigitalCertificateId = cert.Id,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundEncryption,
            HolderType = CertificateHolderType.ClearingHouse,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=a", Issuer = "CN=a", SerialNumber = "1", Thumbprint = "t", FingerprintSha256 = "f",
            NotBefore = DateTime.UtcNow.AddDays(-1), NotAfter = DateTime.UtcNow.AddDays(3), HasPrivateKey = false,
            KeyAlgorithm = "RSA", KeySize = 2048, SignatureAlgorithm = "SHA256"
        };
        context.DigitalCertificateVersions.Add(version);
        await context.SaveChangesAsync();

        var service = new CertificateActivationService(context, new CertificateValidationService(context));
        var dto = await service.RevokeVersionAsync(new RevokeCertificateVersionRequest(version.Id, "tester", "compromised"));

        dto.Status.Should().Be(CertificateStatus.Revoked);
        context.CertificateLoadAudits.Should().ContainSingle(x => x.LoadSource == "revocation");
    }

    [Fact]
    public void Api_ShouldNotReturnPasswordOrPrivateMaterial()
    {
        var props = typeof(CertificateManagementController.CertificateVersionApiDto).GetProperties().Select(p => p.Name).ToList();
        props.Should().NotContain(new[] { "Password", "RawPrivateKey", "SecretRef", "PrivateMaterial" });
        props.Should().Contain("SecretRefMasked");
    }

    [Fact]
    public async Task Logs_ShouldNotContainSecrets()
    {
        using var context = CreateContext(nameof(Logs_ShouldNotContainSecrets));
        var cert = new DigitalCertificate { Code = "A", DisplayName = "A" };
        context.DigitalCertificates.Add(cert);
        await context.SaveChangesAsync();
        var version = new DigitalCertificateVersion
        {
            DigitalCertificateId = cert.Id,
            ClearingHouseId = 1,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundEncryption,
            HolderType = CertificateHolderType.ClearingHouse,
            Status = CertificateStatus.Active,
            VersionNumber = 1,
            Subject = "CN=a", Issuer = "CN=a", SerialNumber = "1", Thumbprint = "t", FingerprintSha256 = "f",
            NotBefore = DateTime.UtcNow.AddDays(-1), NotAfter = DateTime.UtcNow.AddDays(3), HasPrivateKey = false,
            KeyAlgorithm = "RSA", KeySize = 2048, SignatureAlgorithm = "SHA256"
        };
        context.DigitalCertificateVersions.Add(version);
        await context.SaveChangesAsync();

        var logger = new CertificateUsageLoggerService(context);
        await logger.LogUsageAsync(version.Id, "Encrypt", "op-1", "FAIL", "INVALID_CERT", "proc");

        var log = context.CertificateUsageLogs.Single();
        log.ErrorCode.Should().NotBeNull();
        log.ErrorCode!.ToLowerInvariant().Should().NotContain("pass");
    }

    [Fact]
    public void MigrationModel_ShouldHaveRowVersionAndIndexes()
    {
        using var context = CreateContext(nameof(MigrationModel_ShouldHaveRowVersionAndIndexes));
        var entity = context.Model.FindEntityType(typeof(DigitalCertificateVersion));
        entity.Should().NotBeNull();
        entity!.FindProperty(nameof(DigitalCertificateVersion.RowVersion)).Should().NotBeNull();
        entity.GetIndexes().Any(i => i.Properties.Any(p => p.Name == nameof(DigitalCertificateVersion.Thumbprint))).Should().BeTrue();
    }

    private sealed class FakeStore : ICertificatePrivateMaterialStore
    {
        public Task<CertificatePrivateMaterialStoreResult> StorePkcs12Async(CertificatePrivateMaterialStoreRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new CertificatePrivateMaterialStoreResult("openbao://certificates/test/ch-1/outboundsigning/v1", "****ng/v1", "openbao"));
    }
}
