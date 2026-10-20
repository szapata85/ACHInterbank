using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class CertificateManagementPhase1Tests
{
    private static AchDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new AchDbContext(options);
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "1",
            ClearingHouseId = 1
        });
        var defaultInstitution = new FinancialInstitution
        {
            Id = 7,
            Name = "CFA",
            IsDefaultSource = true,
            RoutingNumber = "00000",
            TransitCode = "000"
        };
        defaultInstitution.CalculateCheckDigit();
        context.FinancialInstitutions.Add(defaultInstitution);
        context.SaveChanges();
        return context;
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
    public async Task ManagedCfaCertificate_ShouldUseOfficialDatesAndDefaultSourceOwner()
    {
        using var context = CreateContext(nameof(ManagedCfaCertificate_ShouldUseOfficialDatesAndDefaultSourceOwner));
        var protector = new DataProtectionCertificatePrivateMaterialProtector(new EphemeralDataProtectionProvider());
        var service = new CertificateLoadService(
            context,
            new CertificateSecretProtectorService(),
            protector,
            Options.Create(new CertificateManagementOptions { ExpirationWarningDays = 30 }));
        var startsAt = new DateTimeOffset(2026, 1, 15, 5, 30, 0, TimeSpan.Zero);
        var expiresAt = new DateTimeOffset(2027, 1, 15, 5, 30, 0, TimeSpan.Zero);
        var (_, pfx) = CreateTestCertificateWithDates(startsAt, expiresAt);

        var result = await service.SaveManagedCertificateAsync(new SaveManagedCertificateRequest(
            CertificatePurpose.CfaSigningAndDecryption,
            null,
            pfx,
            "test-pass",
            "CFA.pfx",
            "tester"));

        var stored = await context.DigitalCertificateVersions.SingleAsync();
        result.FinancialInstitutionId.Should().Be(7);
        result.ClearingHouseId.Should().BeNull();
        result.HasPrivateKey.Should().BeTrue();
        stored.NotBefore.Should().Be(startsAt.UtcDateTime);
        stored.NotAfter.Should().Be(expiresAt.UtcDateTime);
        stored.EncryptedPrivateMaterial.Should().NotBeNullOrEmpty();
        stored.EncryptedPrivateMaterial.Should().NotEqual(pfx);
    }

    [Fact]
    public async Task ManagedClearingHouseCertificate_ShouldAssociatePublicCertificateOnly()
    {
        using var context = CreateContext(nameof(ManagedClearingHouseCertificate_ShouldAssociatePublicCertificateOnly));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService());
        var (cer, _) = CreateTestCertificate();

        var result = await service.SaveManagedCertificateAsync(new SaveManagedCertificateRequest(
            CertificatePurpose.ClearingHouseValidation,
            1,
            cer,
            null,
            "ACHcolombia.cer",
            "tester"));

        result.ClearingHouseId.Should().Be(1);
        result.FinancialInstitutionId.Should().BeNull();
        result.HasPrivateKey.Should().BeFalse();
        result.Purpose.Should().Be(CertificatePurpose.ClearingHouseValidation);
    }

    [Fact]
    public async Task ManagedCertificate_ShouldAllowLegacyPurposeMigrationButRejectManagedDuplicate()
    {
        using var context = CreateContext(nameof(ManagedCertificate_ShouldAllowLegacyPurposeMigrationButRejectManagedDuplicate));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService());
        var (cer, _) = CreateTestCertificate();

        await service.LoadPublicCertificateAsync(new LoadPublicCertificateRequest(
            "LEGACY-ACH",
            "Certificado histÃ³rico",
            1,
            CertificateEnvironment.Production,
            CertificatePurpose.OutboundEncryption,
            CertificateHolderType.ClearingHouse,
            cer,
            "tester",
            "ACHcolombia.cer"));

        var managedRequest = new SaveManagedCertificateRequest(
            CertificatePurpose.ClearingHouseValidation,
            1,
            cer,
            null,
            "ACHcolombia.cer",
            "tester");

        var managed = await service.SaveManagedCertificateAsync(managedRequest);
        managed.Purpose.Should().Be(CertificatePurpose.ClearingHouseValidation);

        var duplicate = () => service.SaveManagedCertificateAsync(managedRequest);
        await duplicate.Should().ThrowAsync<CertificateConflictException>()
            .WithMessage("*ya se encuentra registrado*");
        context.CertificateLoadAudits.Should().Contain(x => x.Action == "duplicate-rejected");
    }

    [Fact]
    public async Task ManagedCfaCertificate_ShouldRejectMissingOrAmbiguousDefaultSource()
    {
        using var context = CreateContext(nameof(ManagedCfaCertificate_ShouldRejectMissingOrAmbiguousDefaultSource));
        var (_, pfx) = CreateTestCertificate();
        var protector = new DataProtectionCertificatePrivateMaterialProtector(new EphemeralDataProtectionProvider());
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService(), protector);

        context.FinancialInstitutions.Single().IsDefaultSource = false;
        await context.SaveChangesAsync();
        var missing = () => service.SaveManagedCertificateAsync(new SaveManagedCertificateRequest(
            CertificatePurpose.CfaSigningAndDecryption, null, pfx, "test-pass", "CFA.pfx", "tester"));
        await missing.Should().ThrowAsync<CertificateValidationException>()
            .WithMessage("*configurada como origen*");

        context.FinancialInstitutions.Single().IsDefaultSource = true;
        var alternate = new FinancialInstitution
        {
            Name = "Origen alterno",
            IsDefaultSource = true,
            RoutingNumber = "11111",
            TransitCode = "111"
        };
        alternate.CalculateCheckDigit();
        context.FinancialInstitutions.Add(alternate);
        await context.SaveChangesAsync();
        var ambiguous = () => service.SaveManagedCertificateAsync(new SaveManagedCertificateRequest(
            CertificatePurpose.CfaSigningAndDecryption, null, pfx, "test-pass", "CFA.pfx", "tester"));
        await ambiguous.Should().ThrowAsync<CertificateValidationException>()
            .WithMessage("*más de una*");
    }

    [Fact]
    public async Task RevokedUnusedCertificate_ShouldBeDeletableAndUploadableAgain()
    {
        using var context = CreateContext(nameof(RevokedUnusedCertificate_ShouldBeDeletableAndUploadableAgain));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService());
        var (cer, _) = CreateTestCertificate();
        var request = new SaveManagedCertificateRequest(
            CertificatePurpose.ClearingHouseValidation, 1, cer, null, "ACHcolombia.cer", "tester");
        var saved = await service.SaveManagedCertificateAsync(request);
        var activation = new CertificateActivationService(context, new CertificateValidationService(context));

        var emptyReason = () => activation.RevokeVersionAsync(
            new RevokeCertificateVersionRequest(saved.Id, "tester", " "));
        await emptyReason.Should().ThrowAsync<CertificateValidationException>();

        var revoked = await activation.RevokeVersionAsync(
            new RevokeCertificateVersionRequest(saved.Id, "tester", "Rotación controlada"));
        revoked.NotBefore.Should().Be(saved.NotBefore);
        revoked.NotAfter.Should().Be(saved.NotAfter);
        revoked.RevocationReason.Should().Be("Rotación controlada");

        var deletion = new CertificateDeletionService(context);
        var deleted = await deletion.DeleteVersionAsync(
            new DeleteCertificateVersionRequest(saved.Id, "tester"));
        deleted.Deleted.Should().BeTrue();

        var reuploaded = await service.SaveManagedCertificateAsync(request);
        reuploaded.Id.Should().NotBe(saved.Id);
        context.CertificateLoadAudits.Should().Contain(x => x.Action == "deletion");
    }

    private static (byte[] Cer, byte[] Pfx) CreateTestCertificateWithDates(
        DateTimeOffset notBefore,
        DateTimeOffset notAfter)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Known Dates",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        return (
            certificate.Export(X509ContentType.Cert),
            certificate.Export(X509ContentType.Pkcs12, "test-pass"));
    }

    [Fact]
    public async Task LoadPublicCertificate_ShouldExtractMetadata()
    {
        using var context = CreateContext(nameof(LoadPublicCertificate_ShouldExtractMetadata));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService());
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
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService(),
            new DataProtectionCertificatePrivateMaterialProtector(new EphemeralDataProtectionProvider()));
        var (_, pfx) = CreateTestCertificate("CN=Private Test");

        var result = await service.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            "CERT-PRV", "Private", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, pfx, "test-pass", "tester", CertificateStorageMode.DatabaseEncrypted, null));

        result.HasPrivateKey.Should().BeTrue();
        context.DigitalCertificateVersions.Single().SecretRef.Should().StartWith("dbenc://");
    }

    [Fact]
    public async Task RegisterPrivateCertificate_ShouldRejectInvalidPassword()
    {
        using var context = CreateContext(nameof(RegisterPrivateCertificate_ShouldRejectInvalidPassword));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService());
        var (_, pfx) = CreateTestCertificate();

        var act = () => service.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            "CERT-PRV", "Private", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, pfx, "wrong", "tester", CertificateStorageMode.DatabaseEncrypted, null));

        await act.Should().ThrowAsync<CertificateValidationException>();
    }

    [Fact]
    public async Task RegisterPrivateCertificate_ShouldRejectExternalReferenceMode()
    {
        using var context = CreateContext(nameof(RegisterPrivateCertificate_ShouldRejectExternalReferenceMode));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService());
        var (_, pfx) = CreateTestCertificate("CN=Private Test");

        var act = () => service.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            "CERT-PRV-REF", "Private", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, pfx, "test-pass", "tester", CertificateStorageMode.ExternalSecretReference, "kv://certificates/test/ch-1/outboundsigning/v1"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*base de datos*");
        context.DigitalCertificateVersions.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterPrivateCertificate_ShouldPersistOnlyAuthenticatedEncryptedMaterial()
    {
        using var context = CreateContext(nameof(RegisterPrivateCertificate_ShouldPersistOnlyAuthenticatedEncryptedMaterial));
        var protector = new DataProtectionCertificatePrivateMaterialProtector(new EphemeralDataProtectionProvider());
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService(), protector);
        var (_, pfx) = CreateTestCertificate("CN=Protected Private Test");

        var result = await service.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            "CERT-PRV-DB", "Private protected", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning,
            CertificateHolderType.Participant, pfx, "test-pass", "tester", CertificateStorageMode.DatabaseEncrypted, null, "private.pfx"));

        var stored = context.DigitalCertificateVersions.Single();
        stored.EncryptedPrivateMaterial.Should().NotBeNullOrEmpty();
        stored.EncryptedPrivateMaterial.Should().NotEqual(pfx);
        stored.PrivateMaterialStorageMode.Should().Be(CertificateStorageMode.DatabaseEncrypted);
        stored.SecretRef.Should().StartWith("dbenc://");
        stored.HasPrivateKey.Should().BeTrue();
        result.FileName.Should().Be("private.pfx");

        var provider = new DatabaseEncryptedCertificateSecretProvider(context, protector);
        var resolved = await provider.ResolveAsync(new CertificateSecretResolutionRequest(
            stored.Id, stored.Purpose, stored.PrivateMaterialStorageMode, stored.SecretRef!, "tester"));
        resolved.Success.Should().BeTrue();
        resolved.Material!.HasPrivateKey.Should().BeTrue();
        resolved.Material.Certificate.Dispose();
    }

    [Fact]
    public async Task LoadPublicCertificate_ShouldRejectDuplicateFingerprintInSameContext()
    {
        using var context = CreateContext(nameof(LoadPublicCertificate_ShouldRejectDuplicateFingerprintInSameContext));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService());
        var (cer, _) = CreateTestCertificate();
        var request = new LoadPublicCertificateRequest(
            "CERT-PUB-DUP", "Public", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundEncryption,
            CertificateHolderType.ClearingHouse, cer, "tester", "public.cer");

        await service.LoadPublicCertificateAsync(request);
        var duplicate = () => service.LoadPublicCertificateAsync(request);

        await duplicate.Should().ThrowAsync<CertificateConflictException>();
    }

    [Fact]
    public async Task CertificateTypes_ShouldRejectPfxAsPublicAndCerAsPrivate()
    {
        using var context = CreateContext(nameof(CertificateTypes_ShouldRejectPfxAsPublicAndCerAsPrivate));
        var service = new CertificateLoadService(context, new CertificateSecretProtectorService(),
            new DataProtectionCertificatePrivateMaterialProtector(new EphemeralDataProtectionProvider()));
        var (cer, pfx) = CreateTestCertificate();

        var publicAct = () => service.LoadPublicCertificateAsync(new LoadPublicCertificateRequest(
            "PUB", "Public", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundEncryption,
            CertificateHolderType.ClearingHouse, pfx, "tester", "wrong.pfx"));
        var privateAct = () => service.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            "PRV", "Private", 1, CertificateEnvironment.Test, CertificatePurpose.OutboundSigning,
            CertificateHolderType.Participant, cer, "test-pass", "tester", CertificateStorageMode.DatabaseEncrypted, null, "wrong.cer"));

        await publicAct.Should().ThrowAsync<CertificateValidationException>();
        await privateAct.Should().ThrowAsync<CertificateValidationException>();
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
    public async Task Selection_ShouldIgnoreExpiredAndChooseLatestValidVersionDeterministically()
    {
        using var context = CreateContext(nameof(Selection_ShouldIgnoreExpiredAndChooseLatestValidVersionDeterministically));
        var cert = new DigitalCertificate { Code = "RECIPIENT", DisplayName = "Recipient" };
        context.DigitalCertificates.Add(cert);
        await context.SaveChangesAsync();
        var now = DateTime.UtcNow;

        DigitalCertificateVersion BuildVersion(int versionNumber, DateTime notAfter) => new()
        {
            DigitalCertificateId = cert.Id,
            ClearingHouseId = 7,
            Environment = CertificateEnvironment.Test,
            Purpose = CertificatePurpose.OutboundEncryption,
            HolderType = CertificateHolderType.ClearingHouse,
            Status = CertificateStatus.Active,
            VersionNumber = versionNumber,
            Subject = $"CN=v{versionNumber}",
            Issuer = "CN=issuer",
            SerialNumber = versionNumber.ToString(),
            Thumbprint = $"thumb-{versionNumber}",
            FingerprintSha256 = $"fingerprint-{versionNumber}",
            NotBefore = now.AddDays(-5),
            NotAfter = notAfter,
            HasPrivateKey = false,
            KeyAlgorithm = "RSA",
            KeySize = 2048,
            SignatureAlgorithm = "SHA256",
            ActivatedAtUtc = now.AddDays(-1)
        };

        context.DigitalCertificateVersions.AddRange(
            BuildVersion(1, now.AddDays(5)),
            BuildVersion(2, now.AddMinutes(-1)),
            BuildVersion(3, now.AddDays(5)));
        await context.SaveChangesAsync();

        var service = new CertificateSelectionService(context);
        var result = await service.SelectActiveAsync(
            7,
            CertificateEnvironment.Test,
            CertificatePurpose.OutboundEncryption,
            CertificateHolderType.ClearingHouse);

        result.Should().NotBeNull();
        result!.VersionNumber.Should().Be(3);
        result.NotAfter.Should().BeAfter(now);
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

}
