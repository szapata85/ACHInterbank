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
            v.RevokedAtUtc,
            v.FileName);
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

        if (mode != CertificateStorageMode.DatabaseEncrypted)
        {
            throw new InvalidOperationException("Solo se admite material privado cifrado en la base de datos.");
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

        var requiresSecretRef = version.PrivateMaterialStorageMode is CertificateStorageMode.ExternalSecretReference or CertificateStorageMode.KeyVaultReference;
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
    private readonly ICertificatePrivateMaterialProtector? _privateMaterialProtector;

    public CertificateLoadService(
        AchDbContext context,
        ICertificateSecretProtector secretProtector,
        ICertificatePrivateMaterialProtector? privateMaterialProtector = null)
    {
        _context = context;
        _secretProtector = secretProtector;
        _privateMaterialProtector = privateMaterialProtector;
    }

    public async Task<CertificateVersionDto> LoadPublicCertificateAsync(LoadPublicCertificateRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePublicContext(request.Purpose, request.HolderType);
        await EnsureClearingHouseExistsAsync(request.ClearingHouseId, cancellationToken);

        X509Certificate2 cert;
        try
        {
            if (X509Certificate2.GetCertContentType(request.RawCertificate) == X509ContentType.Pkcs12)
            {
                throw new CertificateValidationException("Un PKCS#12 no puede registrarse como certificado público.");
            }
            cert = X509CertificateLoader.LoadCertificate(request.RawCertificate);
        }
        catch (CertificateValidationException)
        {
            throw;
        }
        catch (CryptographicException)
        {
            throw new CertificateValidationException("El archivo no contiene un certificado X.509 público válido.");
        }

        using (cert)
        {
            if (cert.HasPrivateKey)
            {
                throw new CertificateValidationException("El certificado público no puede contener llave privada.");
            }
            ValidateCertificate(cert, requiresPrivateKey: false);
            await EnsureNotDuplicateAsync(cert, request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cancellationToken);

        var aggregate = await EnsureAggregateAsync(request.Code, request.DisplayName, request.UploadedBy, cancellationToken);
        var nextVersion = await GetNextVersionAsync(aggregate.Id, request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cancellationToken);

            var entity = BuildVersionFromCertificate(request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cert, request.UploadedBy, request.FileName, CertificateMaterialType.PublicCertificate, CertificateStorageMode.DatabaseEncrypted, null);
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
    }

    public async Task<CertificateVersionDto> RegisterPrivateCertificateAsync(RegisterPrivateCertificateRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePrivateContext(request.Purpose, request.HolderType);
        await EnsureClearingHouseExistsAsync(request.ClearingHouseId, cancellationToken);
        await _secretProtector.EnsureAcceptableAsync(request.StorageMode, request.SecretRef, request.Password, cancellationToken);

        X509Certificate2 cert;
        try
        {
            if (X509Certificate2.GetCertContentType(request.RawPkcs12) != X509ContentType.Pkcs12)
            {
                throw new CertificateValidationException("El archivo privado debe ser PKCS#12 (.pfx o .p12).");
            }
            cert = X509CertificateLoader.LoadPkcs12(request.RawPkcs12, request.Password, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (CertificateValidationException)
        {
            throw;
        }
        catch (CryptographicException)
        {
            throw new CertificateValidationException("La contraseña es incorrecta o el archivo PKCS#12 no es válido.");
        }

        using (cert)
        {
            if (!cert.HasPrivateKey)
            {
                throw new CertificateValidationException("El PKCS#12 no contiene una llave privada accesible.");
            }
            ValidateCertificate(cert, requiresPrivateKey: true);
            VerifyCertificateAndPrivateKeyMatch(cert);
            await EnsureNotDuplicateAsync(cert, request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cancellationToken);

            byte[]? encryptedPrivateMaterial = null;
            var resolvedSecretRef = request.SecretRef;
            if (request.StorageMode == CertificateStorageMode.DatabaseEncrypted)
            {
                if (_privateMaterialProtector is null)
                {
                    throw new CertificateValidationException("El protector de material privado no está configurado.");
                }
                encryptedPrivateMaterial = _privateMaterialProtector.Protect(request.RawPkcs12, request.Password);
                resolvedSecretRef = $"dbenc://{Guid.NewGuid():N}";
            }

            var aggregate = await EnsureAggregateAsync(request.Code, request.DisplayName, request.UploadedBy, cancellationToken);
            var nextVersion = await GetNextVersionAsync(aggregate.Id, request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cancellationToken);

            var entity = BuildVersionFromCertificate(request.ClearingHouseId, request.Environment, request.Purpose, request.HolderType, cert, request.UploadedBy, request.FileName, CertificateMaterialType.PrivateKeyPair, request.StorageMode, resolvedSecretRef);
            entity.DigitalCertificateId = aggregate.Id;
            entity.VersionNumber = nextVersion;
            entity.RawPublicCertificate = cert.RawData;
            entity.EncryptedPrivateMaterial = encryptedPrivateMaterial;

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

    private async Task EnsureClearingHouseExistsAsync(int clearingHouseId, CancellationToken cancellationToken)
    {
        if (!await _context.ClearingHouses.AsNoTracking().AnyAsync(x => x.Id == clearingHouseId, cancellationToken))
        {
            throw new CertificateValidationException("La cámara compensadora indicada no existe.");
        }
    }

    private async Task EnsureNotDuplicateAsync(
        X509Certificate2 certificate,
        int clearingHouseId,
        CertificateEnvironment environment,
        CertificatePurpose purpose,
        CertificateHolderType holderType,
        CancellationToken cancellationToken)
    {
        var fingerprint = SHA256.HashData(certificate.RawData);
        var fingerprintHex = Convert.ToHexString(fingerprint);
        var duplicate = await _context.DigitalCertificateVersions.AsNoTracking().AnyAsync(
            x => x.FingerprintSha256 == fingerprintHex
                 && x.ClearingHouseId == clearingHouseId
                 && x.Environment == environment
                 && x.Purpose == purpose
                 && x.HolderType == holderType,
            cancellationToken);
        if (duplicate)
        {
            throw new CertificateConflictException("El certificado ya está registrado para el mismo titular, propósito y ambiente.");
        }
    }

    private static void ValidatePublicContext(CertificatePurpose purpose, CertificateHolderType holderType)
    {
        if (purpose is not (CertificatePurpose.OutboundEncryption or CertificatePurpose.InboundSignatureValidation)
            || holderType != CertificateHolderType.ClearingHouse)
        {
            throw new CertificateValidationException("El certificado público debe corresponder a una cámara compensadora y a un propósito público permitido.");
        }
    }

    private static void ValidatePrivateContext(CertificatePurpose purpose, CertificateHolderType holderType)
    {
        if (purpose is not (CertificatePurpose.OutboundSigning or CertificatePurpose.InboundDecryption)
            || holderType != CertificateHolderType.Participant)
        {
            throw new CertificateValidationException("El certificado privado debe corresponder a la entidad participante y a un propósito que requiera llave privada.");
        }
    }

    private static void ValidateCertificate(X509Certificate2 certificate, bool requiresPrivateKey)
    {
        var now = DateTime.UtcNow;
        if (certificate.NotBefore.ToUniversalTime() > now)
        {
            throw new CertificateValidationException("El certificado aún no está vigente.");
        }
        if (certificate.NotAfter.ToUniversalTime() <= now)
        {
            throw new CertificateValidationException("El certificado está vencido.");
        }

        using var rsa = certificate.GetRSAPublicKey();
        using var ecdsa = certificate.GetECDsaPublicKey();
        if (rsa is not null && rsa.KeySize < 2048)
        {
            throw new CertificateValidationException("La llave RSA debe tener al menos 2048 bits.");
        }
        if (ecdsa is not null && ecdsa.KeySize < 256)
        {
            throw new CertificateValidationException("La llave ECDSA debe tener al menos 256 bits.");
        }
        if (rsa is null && ecdsa is null)
        {
            throw new CertificateValidationException("El algoritmo de llave pública no está permitido.");
        }

        var signatureAlgorithm = certificate.SignatureAlgorithm?.FriendlyName ?? certificate.SignatureAlgorithm?.Value ?? string.Empty;
        if (signatureAlgorithm.Contains("sha1", StringComparison.OrdinalIgnoreCase)
            || signatureAlgorithm.Contains("md5", StringComparison.OrdinalIgnoreCase))
        {
            throw new CertificateValidationException("El algoritmo de firma del certificado no está permitido.");
        }

        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
        if (keyUsage is null)
        {
            return;
        }

        var usages = keyUsage.KeyUsages;
        var validUsage = requiresPrivateKey
            ? usages.HasFlag(X509KeyUsageFlags.DigitalSignature) || usages.HasFlag(X509KeyUsageFlags.NonRepudiation)
            : usages.HasFlag(X509KeyUsageFlags.KeyEncipherment)
              || usages.HasFlag(X509KeyUsageFlags.DataEncipherment)
              || usages.HasFlag(X509KeyUsageFlags.KeyAgreement);
        if (!validUsage)
        {
            throw new CertificateValidationException(requiresPrivateKey
                ? "El certificado no permite firma digital."
                : "El certificado no permite cifrado o acuerdo de llave.");
        }
    }

    private static void VerifyCertificateAndPrivateKeyMatch(X509Certificate2 certificate)
    {
        var challenge = RandomNumberGenerator.GetBytes(32);
        using var rsaPrivate = certificate.GetRSAPrivateKey();
        using var rsaPublic = certificate.GetRSAPublicKey();
        if (rsaPrivate is not null && rsaPublic is not null)
        {
            var signature = rsaPrivate.SignData(challenge, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (rsaPublic.VerifyData(challenge, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) return;
        }

        using var ecdsaPrivate = certificate.GetECDsaPrivateKey();
        using var ecdsaPublic = certificate.GetECDsaPublicKey();
        if (ecdsaPrivate is not null && ecdsaPublic is not null)
        {
            var signature = ecdsaPrivate.SignData(challenge, HashAlgorithmName.SHA256);
            if (ecdsaPublic.VerifyData(challenge, signature, HashAlgorithmName.SHA256)) return;
        }

        throw new CertificateValidationException("La llave privada no corresponde al certificado público.");
    }

    private static DigitalCertificateVersion BuildVersionFromCertificate(int clearingHouseId, CertificateEnvironment environment, CertificatePurpose purpose, CertificateHolderType holderType, X509Certificate2 cert, string actor, string fileName, CertificateMaterialType materialType, CertificateStorageMode storageMode, string? secretRef)
    {
        string fingerprint;
        using (var sha256 = SHA256.Create())
        {
            fingerprint = Convert.ToHexString(sha256.ComputeHash(cert.RawData));
        }

        using var rsa = cert.GetRSAPublicKey();
        using var ecdsa = cert.GetECDsaPublicKey();
        var requiresExternalBinding = materialType == CertificateMaterialType.PrivateKeyPair
            && storageMode is CertificateStorageMode.ExternalSecretReference or CertificateStorageMode.KeyVaultReference
            && string.IsNullOrWhiteSpace(secretRef);

        return new DigitalCertificateVersion
        {
            ClearingHouseId = clearingHouseId,
            Environment = environment,
            Purpose = purpose,
            HolderType = holderType,
            Status = requiresExternalBinding ? CertificateStatus.PendingSecretBinding : CertificateStatus.Draft,
            MaterialType = materialType,
            FileName = Path.GetFileName(fileName),
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            SerialNumber = cert.SerialNumber,
            Thumbprint = cert.Thumbprint ?? string.Empty,
            FingerprintSha256 = fingerprint,
            NotBefore = cert.NotBefore,
            NotAfter = cert.NotAfter,
            HasPrivateKey = cert.HasPrivateKey,
            KeyAlgorithm = rsa?.SignatureAlgorithm ?? ecdsa?.SignatureAlgorithm ?? cert.PublicKey.Oid?.FriendlyName ?? "Unknown",
            KeySize = rsa?.KeySize ?? ecdsa?.KeySize ?? 0,
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

    public async Task LogUsageAsync(int versionId, string operationType, string operationId, string result, string? errorCode, string actor, string? contextJson = null, CancellationToken cancellationToken = default)
    {
        _context.CertificateUsageLogs.Add(new CertificateUsageLog
        {
            CertificateVersionId = versionId,
            OperationType = operationType,
            OperationId = operationId,
            ContextJson = contextJson,
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
