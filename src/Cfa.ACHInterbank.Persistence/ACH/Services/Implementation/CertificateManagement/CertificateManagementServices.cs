using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

internal static class CertificateManagementMapper
{
    public static CertificateVersionDto ToDto(this DigitalCertificateVersion v)
        => new(
            v.Id,
            v.DigitalCertificate.Code,
            v.DigitalCertificate.DisplayName,
            v.ClearingHouseId,
            v.Environment,
            v.Purpose,
            v.HolderType,
            v.Status,
            v.VersionNumber,
            v.Subject,
            v.Issuer,
            v.SerialNumber,
            v.Thumbprint,
            v.FingerprintSha256,
            v.NotBefore,
            v.NotAfter,
            v.HasPrivateKey,
            v.KeyAlgorithm,
            v.KeySize,
            v.SignatureAlgorithm,
            v.SecretRef,
            v.UploadedAtUtc,
            v.UploadedBy,
            v.ActivatedAtUtc,
            v.RevokedAtUtc);
}

[Scoped]
public class CertificateSecretProtectorService : ICertificateSecretProtector
{
    public Task EnsureAcceptableAsync(CertificateStorageMode mode, string? secretRef, string? password, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(password))
        {
            // Password must only be used in-memory and never persisted.
        }

        var requiresRef = mode is CertificateStorageMode.ExternalSecretReference or CertificateStorageMode.KeyVaultReference;
        if (requiresRef && string.IsNullOrWhiteSpace(secretRef))
        {
            throw new InvalidOperationException("SecretRef es obligatorio para el modo de almacenamiento seleccionado.");
        }

        return Task.CompletedTask;
    }
}

[Scoped]
public class CertificateValidationService : ICertificateValidationService
{
    private readonly AchDbContext _context;

    public CertificateValidationService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<CertificateValidationResultDto> ValidateForActivationAsync(int versionId, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var version = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);

        if (version == null)
        {
            errors.Add("Versión de certificado no encontrada.");
            return new CertificateValidationResultDto(false, errors);
        }

        var now = DateTime.UtcNow;
        if (version.NotAfter <= now) errors.Add("El certificado está expirado.");
        if (version.NotBefore > now) errors.Add("El certificado no está vigente todavía.");

        var requiresPrivate = version.Purpose is CertificatePurpose.OutboundSigning or CertificatePurpose.InboundDecryption;
        if (requiresPrivate && !version.HasPrivateKey) errors.Add("El propósito requiere llave privada y el certificado no la tiene.");

        var requiresSecretRef = version.PrivateMaterialStorageMode is CertificateStorageMode.ExternalSecretReference or CertificateStorageMode.KeyVaultReference or CertificateStorageMode.OpenBaoReference;
        if (requiresPrivate && requiresSecretRef && string.IsNullOrWhiteSpace(version.SecretRef))
            errors.Add("SecretRef requerido para certificado privado en modo de referencia externa.");

        return new CertificateValidationResultDto(errors.Count == 0, errors);
    }
}

[Scoped]
public class CertificateLoadService : ICertificateLoadService
{
    private readonly AchDbContext _context;
    private readonly ICertificateSecretProtector _secretProtector;
    private readonly ICertificatePrivateMaterialStore _privateMaterialStore;

    public CertificateLoadService(
        AchDbContext context,
        ICertificateSecretProtector secretProtector,
        ICertificatePrivateMaterialStore privateMaterialStore)
    {
        _context = context;
        _secretProtector = secretProtector;
        _privateMaterialStore = privateMaterialStore;
    }

    public async Task<CertificateVersionDto> LoadPublicCertificateAsync(LoadPublicCertificateRequest request, CancellationToken cancellationToken = default)
    {
        var cert = X509CertificateLoader.LoadCertificate(request.RawCertificate);
        var aggregate = await EnsureAggregateAsync(request.Code, request.DisplayName, request.UploadedBy, cancellationToken);
        var nextVersion = await GetNextVersionAsync(aggregate.Id, request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cancellationToken);

        var entity = BuildVersionFromCertificate(request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cert, request.UploadedBy, CertificateMaterialType.PublicCertificate, CertificateStorageMode.DatabaseEncrypted, null);
        entity.DigitalCertificateId = aggregate.Id;
        entity.VersionNumber = nextVersion;
        entity.RawPublicCertificate = cert.RawData;

        _context.DigitalCertificateVersions.Add(entity);
        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersion = entity,
            LoadSource = "public-upload",
            ValidationResult = "SUCCESS",
            LoadedBy = request.UploadedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _context.Entry(entity).Reference(x => x.DigitalCertificate).LoadAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<CertificateVersionDto> RegisterPrivateCertificateAsync(RegisterPrivateCertificateRequest request, CancellationToken cancellationToken = default)
    {
        await _secretProtector.EnsureAcceptableAsync(request.StorageMode, request.SecretRef, request.Password, cancellationToken);

        X509Certificate2 cert;
        try
        {
            cert = X509CertificateLoader.LoadPkcs12(request.RawPkcs12, request.Password, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Password o material PKCS#12 inválido.");
        }

        var aggregate = await EnsureAggregateAsync(request.Code, request.DisplayName, request.UploadedBy, cancellationToken);
        var nextVersion = await GetNextVersionAsync(aggregate.Id, request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cancellationToken);

        var resolvedSecretRef = request.SecretRef;
        if (request.StorageMode == CertificateStorageMode.OpenBaoReference)
        {
            var stored = await _privateMaterialStore.StorePkcs12Async(
                new CertificatePrivateMaterialStoreRequest(
                    request.ClearingHouseId,
                    request.Environment.ToString(),
                    request.Purpose.ToString(),
                    nextVersion,
                    request.RawPkcs12,
                    request.Password,
                    request.UploadedBy),
                cancellationToken);
            resolvedSecretRef = stored.SecretRef;
        }

        var entity = BuildVersionFromCertificate(request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cert, request.UploadedBy, CertificateMaterialType.PrivateKeyPair, request.StorageMode, resolvedSecretRef);
        entity.DigitalCertificateId = aggregate.Id;
        entity.VersionNumber = nextVersion;

        _context.DigitalCertificateVersions.Add(entity);
        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersion = entity,
            LoadSource = "private-upload",
            ValidationResult = "SUCCESS",
            LoadedBy = request.UploadedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _context.Entry(entity).Reference(x => x.DigitalCertificate).LoadAsync(cancellationToken);
        return entity.ToDto();
    }

    private async Task<DigitalCertificate> EnsureAggregateAsync(string code, string displayName, string actor, CancellationToken cancellationToken)
    {
        var aggregate = await _context.DigitalCertificates.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (aggregate != null) return aggregate;

        aggregate = new DigitalCertificate
        {
            Code = code,
            DisplayName = displayName,
            CreatedBy = actor
        };
        _context.DigitalCertificates.Add(aggregate);
        await _context.SaveChangesAsync(cancellationToken);
        return aggregate;
    }

    private async Task<int> GetNextVersionAsync(int digitalCertificateId, int clearingHouseId, CertificateEnvironment environment, CertificatePurpose purpose, CertificateHolderType holderType, CancellationToken cancellationToken)
    {
        var max = await _context.DigitalCertificateVersions
            .Where(x => x.DigitalCertificateId == digitalCertificateId && x.ClearingHouseId == clearingHouseId && x.Environment == environment && x.Purpose == purpose && x.HolderType == holderType)
            .MaxAsync(x => (int?)x.VersionNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    private static DigitalCertificateVersion BuildVersionFromCertificate(int clearingHouseId, CertificateEnvironment environment, CertificatePurpose purpose, CertificateHolderType holderType, X509Certificate2 cert, string actor, CertificateMaterialType materialType, CertificateStorageMode storageMode, string? secretRef)
    {
        string fingerprint;
        using (var sha256 = SHA256.Create())
        {
            fingerprint = Convert.ToHexString(sha256.ComputeHash(cert.RawData));
        }

        using var rsa = cert.GetRSAPublicKey();

        return new DigitalCertificateVersion
        {
            ClearingHouseId = clearingHouseId,
            Environment = environment,
            Purpose = purpose,
            HolderType = holderType,
            Status = string.IsNullOrWhiteSpace(secretRef) && materialType == CertificateMaterialType.PrivateKeyPair
                ? CertificateStatus.PendingSecretBinding
                : CertificateStatus.Draft,
            MaterialType = materialType,
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            SerialNumber = cert.SerialNumber,
            Thumbprint = cert.Thumbprint ?? string.Empty,
            FingerprintSha256 = fingerprint,
            NotBefore = cert.NotBefore,
            NotAfter = cert.NotAfter,
            HasPrivateKey = cert.HasPrivateKey,
            KeyAlgorithm = rsa?.SignatureAlgorithm ?? cert.PublicKey.Oid?.FriendlyName ?? "Unknown",
            KeySize = rsa?.KeySize ?? 0,
            SignatureAlgorithm = cert.SignatureAlgorithm?.FriendlyName ?? cert.SignatureAlgorithm?.Value ?? string.Empty,
            PrivateMaterialStorageMode = storageMode,
            SecretRef = secretRef,
            UploadedBy = actor,
            UploadedAtUtc = DateTime.UtcNow
        };
    }
}

[Scoped]
public class CertificateSelectionService : ICertificateSelectionService
{
    private readonly AchDbContext _context;

    public CertificateSelectionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<CertificateVersionDto?> SelectActiveAsync(int clearingHouseId, CertificateEnvironment environment, CertificatePurpose purpose, CertificateHolderType holderType, CancellationToken cancellationToken = default)
    {
        var version = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .FirstOrDefaultAsync(x => x.ClearingHouseId == clearingHouseId
                                      && x.Environment == environment
                                      && x.Purpose == purpose
                                      && x.HolderType == holderType
                                      && x.Status == CertificateStatus.Active, cancellationToken);
        return version?.ToDto();
    }
}

[Scoped]
public class CertificateActivationService : ICertificateActivationService
{
    private readonly AchDbContext _context;
    private readonly ICertificateValidationService _validator;

    public CertificateActivationService(AchDbContext context, ICertificateValidationService validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<CertificateVersionDto> ActivateVersionAsync(ActivateCertificateVersionRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateForActivationAsync(request.VersionId, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(" | ", validation.Errors));
        }

        var version = await _context.DigitalCertificateVersions
            .Include(x => x.DigitalCertificate)
            .FirstAsync(x => x.Id == request.VersionId, cancellationToken);

        var currentActive = await _context.DigitalCertificateVersions
            .Where(x => x.ClearingHouseId == version.ClearingHouseId
                        && x.Environment == version.Environment
                        && x.Purpose == version.Purpose
                        && x.HolderType == version.HolderType
                        && x.Status == CertificateStatus.Active
                        && x.Id != version.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentActive != null)
        {
            currentActive.Status = CertificateStatus.Replaced;
            currentActive.ReplacedByVersionId = version.Id;
            _context.CertificateRotationHistories.Add(new CertificateRotationHistory
            {
                PreviousVersionId = currentActive.Id,
                NewVersionId = version.Id,
                Reason = "Activation replacement",
                RotatedBy = request.ActivatedBy
            });
        }

        version.Status = CertificateStatus.Active;
        version.ActivatedAtUtc = DateTime.UtcNow;

        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = version.Id,
            LoadSource = "activation",
            ValidationResult = "SUCCESS",
            LoadedBy = request.ActivatedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        return version.ToDto();
    }

    public async Task<CertificateVersionDto> RevokeVersionAsync(RevokeCertificateVersionRequest request, CancellationToken cancellationToken = default)
    {
        var version = await _context.DigitalCertificateVersions
            .Include(x => x.DigitalCertificate)
            .FirstAsync(x => x.Id == request.VersionId, cancellationToken);
        version.Status = CertificateStatus.Revoked;
        version.RevokedAtUtc = DateTime.UtcNow;

        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = version.Id,
            LoadSource = "revocation",
            ValidationResult = request.Reason,
            LoadedBy = request.RevokedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        return version.ToDto();
    }
}

[Scoped]
public class CertificateRotationService : ICertificateRotationService
{
    private readonly AchDbContext _context;

    public CertificateRotationService(AchDbContext context)
    {
        _context = context;
    }

    public async Task RotateAsync(int previousVersionId, int newVersionId, string reason, string actor, CancellationToken cancellationToken = default)
    {
        _context.CertificateRotationHistories.Add(new CertificateRotationHistory
        {
            PreviousVersionId = previousVersionId,
            NewVersionId = newVersionId,
            Reason = reason,
            RotatedBy = actor,
            RotatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}

[Scoped]
public class CertificateUsageLoggerService : ICertificateUsageLogger
{
    private readonly AchDbContext _context;

    public CertificateUsageLoggerService(AchDbContext context)
    {
        _context = context;
    }

    public async Task LogUsageAsync(int versionId, string operationType, string operationId, string result, string? errorCode, string actor, CancellationToken cancellationToken = default)
    {
        _context.CertificateUsageLogs.Add(new CertificateUsageLog
        {
            CertificateVersionId = versionId,
            OperationType = operationType,
            OperationId = operationId,
            Result = result,
            ErrorCode = errorCode,
            CreatedByProcess = actor,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}

[Scoped]
public class CertificateAuditService : ICertificateAuditService
{
    private readonly AchDbContext _context;

    public CertificateAuditService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CertificateAuditDto>> ListLoadAuditsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CertificateLoadAudits
            .AsNoTracking()
            .OrderByDescending(x => x.LoadedAtUtc)
            .Select(x => new CertificateAuditDto(x.Id, x.CertificateVersionId, x.LoadSource, x.ValidationResult, x.ValidationErrorsJson, x.LoadedAtUtc, x.LoadedBy))
            .ToListAsync(cancellationToken);
    }
}

[Scoped]
public class CertificateCatalogService : ICertificateCatalogService
{
    private readonly AchDbContext _context;

    public CertificateCatalogService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CertificateVersionDto>> GetCertificatesAsync(CertificateFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .AsQueryable();

        if (filter.ClearingHouseId.HasValue) query = query.Where(x => x.ClearingHouseId == filter.ClearingHouseId.Value);
        if (filter.Environment.HasValue) query = query.Where(x => x.Environment == filter.Environment.Value);
        if (filter.Purpose.HasValue) query = query.Where(x => x.Purpose == filter.Purpose.Value);
        if (filter.HolderType.HasValue) query = query.Where(x => x.HolderType == filter.HolderType.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);

        var items = await query.OrderByDescending(x => x.UploadedAtUtc).ToListAsync(cancellationToken);
        return items.Select(x => x.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<CertificateVersionDto>> GetVersionsAsync(int digitalCertificateId, CancellationToken cancellationToken = default)
    {
        var items = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Where(x => x.DigitalCertificateId == digitalCertificateId)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(cancellationToken);

        return items.Select(x => x.ToDto()).ToList();
    }
}
