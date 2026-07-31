using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

internal static class CertificateManagementMapper
{
    public static CertificateVersionDto ToDto(this DigitalCertificateVersion v, int warningDays = 30, bool canDelete = false)
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
            v.FileName,
            v.FinancialInstitutionId,
            v.FinancialInstitution?.Name,
            v.ClearingHouse?.Name,
            CertificateFunctionalStatusEvaluator.Evaluate(v, DateTime.UtcNow, warningDays),
            CertificateFunctionalStatusEvaluator.DaysRemaining(v.NotAfter, DateTime.UtcNow),
            v.RevocationReason,
            v.RevokedBy,
            canDelete);
}

internal static class CertificateFunctionalStatusEvaluator
{
    public static CertificateFunctionalStatus Evaluate(
        DigitalCertificateVersion version,
        DateTime utcNow,
        int warningDays)
    {
        if (version.Status == CertificateStatus.Revoked) return CertificateFunctionalStatus.Revoked;
        if (version.Status == CertificateStatus.Replaced) return CertificateFunctionalStatus.Replaced;
        if (utcNow < version.NotBefore) return CertificateFunctionalStatus.PendingValidity;
        if (utcNow > version.NotAfter) return CertificateFunctionalStatus.Expired;
        if (version.Status != CertificateStatus.Active) return CertificateFunctionalStatus.Inactive;
        return version.NotAfter <= utcNow.AddDays(Math.Max(0, warningDays))
            ? CertificateFunctionalStatus.ExpiringSoon
            : CertificateFunctionalStatus.Valid;
    }

    public static int? DaysRemaining(DateTime notAfter, DateTime utcNow)
    {
        if (notAfter < utcNow) return 0;
        return Math.Max(0, (int)Math.Ceiling((notAfter - utcNow).TotalDays));
    }
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

        var requiresPrivate = version.Purpose is CertificatePurpose.OutboundSigning
            or CertificatePurpose.InboundDecryption
            or CertificatePurpose.CfaSigningAndDecryption;
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
    private readonly int _expirationWarningDays;

    public CertificateLoadService(
        AchDbContext context,
        ICertificateSecretProtector secretProtector,
        ICertificatePrivateMaterialProtector? privateMaterialProtector = null,
        IOptions<CertificateManagementOptions>? options = null)
    {
        _context = context;
        _secretProtector = secretProtector;
        _privateMaterialProtector = privateMaterialProtector;
        _expirationWarningDays = Math.Max(0, options?.Value.ExpirationWarningDays ?? 30);
    }

    public async Task<CertificatePreviewDto> PreviewManagedCertificateAsync(
        PreviewManagedCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        var owner = await ResolveManagedOwnerAsync(request.Purpose, request.ClearingHouseId, cancellationToken);
        using var certificate = LoadManagedCertificate(request);
        ValidateManagedCertificatePurpose(request.Purpose, certificate);

        var transient = BuildVersionFromCertificate(
            owner.FinancialInstitutionId,
            owner.ClearingHouseId,
            CertificateEnvironment.Production,
            request.Purpose,
            request.Purpose == CertificatePurpose.CfaSigningAndDecryption
                ? CertificateHolderType.Participant
                : CertificateHolderType.ClearingHouse,
            certificate,
            "preview",
            request.FileName,
            certificate.HasPrivateKey ? CertificateMaterialType.PrivateKeyPair : CertificateMaterialType.PublicCertificate,
            certificate.HasPrivateKey ? CertificateStorageMode.DatabaseEncrypted : CertificateStorageMode.DatabaseEncrypted,
            null);
        transient.Status = CertificateStatus.Active;

        var now = DateTime.UtcNow;
        var warnings = new List<string>();
        if (now < transient.NotBefore)
        {
            warnings.Add("El certificado todavía no ha llegado a su fecha de inicio.");
        }
        else if (now > transient.NotAfter)
        {
            warnings.Add("El certificado ya se encuentra vencido.");
        }
        else if (transient.NotAfter <= now.AddDays(_expirationWarningDays))
        {
            warnings.Add("Este certificado requiere renovación próximamente.");
        }

        var functionalStatus = CertificateFunctionalStatusEvaluator.Evaluate(
            transient,
            now,
            _expirationWarningDays);

        return new CertificatePreviewDto(
            request.Purpose,
            owner.FinancialInstitutionId,
            owner.FinancialInstitutionName,
            owner.ClearingHouseId,
            owner.ClearingHouseName,
            transient.Subject,
            transient.Issuer,
            transient.SerialNumber,
            transient.NormalizedThumbprint,
            transient.NotBefore,
            transient.NotAfter,
            transient.HasPrivateKey,
            transient.KeyAlgorithm,
            transient.KeySize,
            transient.SignatureAlgorithm,
            functionalStatus,
            CertificateFunctionalStatusEvaluator.DaysRemaining(transient.NotAfter, now),
            request.Purpose == CertificatePurpose.CfaSigningAndDecryption && transient.HasPrivateKey,
            functionalStatus is CertificateFunctionalStatus.Valid or CertificateFunctionalStatus.ExpiringSoon,
            warnings);
    }

    public async Task<CertificateVersionDto> SaveManagedCertificateAsync(
        SaveManagedCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        var owner = await ResolveManagedOwnerAsync(request.Purpose, request.ClearingHouseId, cancellationToken);
        using var certificate = LoadManagedCertificate(new PreviewManagedCertificateRequest(
            request.Purpose,
            request.ClearingHouseId,
            request.Content,
            request.Password,
            request.FileName));
        ValidateManagedCertificatePurpose(request.Purpose, certificate);
        await EnsureNotDuplicateManagedAsync(certificate, request.Purpose, owner, cancellationToken);

        var functionalStatus = CertificateFunctionalStatusEvaluator.Evaluate(
            new DigitalCertificateVersion
            {
                Status = CertificateStatus.Active,
                NotBefore = certificate.NotBefore.ToUniversalTime(),
                NotAfter = certificate.NotAfter.ToUniversalTime()
            },
            DateTime.UtcNow,
            _expirationWarningDays);
        if (functionalStatus == CertificateFunctionalStatus.Expired)
        {
            throw new CertificateValidationException("El certificado ya se encuentra vencido.");
        }
        if (functionalStatus == CertificateFunctionalStatus.PendingValidity)
        {
            throw new CertificateValidationException("El certificado todavía no ha llegado a su fecha de inicio.");
        }

        var isCfa = request.Purpose == CertificatePurpose.CfaSigningAndDecryption;
        byte[]? encryptedPrivateMaterial = null;
        string? secretRef = null;
        if (isCfa)
        {
            if (_privateMaterialProtector is null)
            {
                throw new CertificateValidationException("El protector de material privado no está configurado.");
            }

            encryptedPrivateMaterial = _privateMaterialProtector.Protect(request.Content, request.Password ?? string.Empty);
            secretRef = $"dbenc://{Guid.NewGuid():N}";
        }

        var code = isCfa
            ? "CFA-IDENTIDAD-DIGITAL"
            : $"{NormalizeCode(owner.ClearingHouseCode)}-VALIDACION";
        var displayName = isCfa
            ? "Certificado de CFA"
            : $"Certificado de {owner.ClearingHouseName}";
        var aggregate = await EnsureAggregateAsync(code, displayName, request.UploadedBy, cancellationToken);
        var nextVersion = await GetNextVersionAsync(
            aggregate.Id,
            owner.FinancialInstitutionId,
            owner.ClearingHouseId,
            CertificateEnvironment.Production,
            request.Purpose,
            isCfa ? CertificateHolderType.Participant : CertificateHolderType.ClearingHouse,
            cancellationToken);

        var entity = BuildVersionFromCertificate(
            owner.FinancialInstitutionId,
            owner.ClearingHouseId,
            CertificateEnvironment.Production,
            request.Purpose,
            isCfa ? CertificateHolderType.Participant : CertificateHolderType.ClearingHouse,
            certificate,
            request.UploadedBy,
            request.FileName,
            isCfa ? CertificateMaterialType.PrivateKeyPair : CertificateMaterialType.PublicCertificate,
            CertificateStorageMode.DatabaseEncrypted,
            secretRef);
        entity.DigitalCertificateId = aggregate.Id;
        entity.VersionNumber = nextVersion;
        entity.Status = CertificateStatus.Active;
        entity.ActivatedAtUtc = DateTime.UtcNow;
        entity.RawPublicCertificate = certificate.RawData;
        entity.EncryptedPrivateMaterial = encryptedPrivateMaterial;

        var currentActive = await _context.DigitalCertificateVersions
            .Where(x => x.Status == CertificateStatus.Active
                        && x.Environment == entity.Environment
                        && x.Purpose == entity.Purpose
                        && x.HolderType == entity.HolderType
                        && x.FinancialInstitutionId == entity.FinancialInstitutionId
                        && x.ClearingHouseId == entity.ClearingHouseId)
            .ToListAsync(cancellationToken);

        _context.DigitalCertificateVersions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var previous in currentActive)
        {
            previous.Status = CertificateStatus.Replaced;
            previous.ReplacedByVersionId = entity.Id;
            _context.CertificateRotationHistories.Add(new CertificateRotationHistory
            {
                PreviousVersionId = previous.Id,
                NewVersionId = entity.Id,
                Reason = "Reemplazo operativo por carga de un certificado más reciente.",
                RotatedBy = request.UploadedBy
            });
        }

        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = entity.Id,
            Action = currentActive.Count > 0 ? "replacement" : "upload",
            CertificateThumbprint = entity.NormalizedThumbprint,
            CertificateDisplayName = displayName,
            LoadSource = isCfa ? "private-upload" : "public-upload",
            ValidationResult = "SUCCESS",
            LoadedBy = request.UploadedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        var persisted = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Include(x => x.FinancialInstitution)
            .Include(x => x.ClearingHouse)
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        return persisted.ToDto(_expirationWarningDays, canDelete: false);
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

    private async Task<ManagedCertificateOwner> ResolveManagedOwnerAsync(
        CertificatePurpose purpose,
        int? clearingHouseId,
        CancellationToken cancellationToken)
    {
        if (purpose == CertificatePurpose.CfaSigningAndDecryption)
        {
            var defaults = await _context.FinancialInstitutions
                .AsNoTracking()
                .Where(x => x.IsDefaultSource)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(cancellationToken);

            if (defaults.Count == 0)
            {
                throw new CertificateValidationException("No se encontró una entidad financiera configurada como origen.");
            }
            if (defaults.Count > 1)
            {
                throw new CertificateValidationException("Existe más de una entidad financiera configurada como origen. Corrige la configuración antes de continuar.");
            }

            return new ManagedCertificateOwner(
                defaults[0].Id,
                defaults[0].Name,
                null,
                null,
                null);
        }

        if (purpose != CertificatePurpose.ClearingHouseValidation)
        {
            throw new CertificateValidationException("El certificado no es compatible con el uso seleccionado.");
        }
        if (!clearingHouseId.HasValue || clearingHouseId.Value <= 0)
        {
            throw new CertificateValidationException("Selecciona la cámara compensadora propietaria del certificado.");
        }

        var clearingHouse = await _context.ClearingHouses
            .AsNoTracking()
            .Where(x => x.Id == clearingHouseId.Value && x.IsActive)
            .Select(x => new { x.Id, x.Name, x.Code })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CertificateValidationException("La cámara compensadora seleccionada no existe o no está activa.");

        return new ManagedCertificateOwner(
            null,
            null,
            clearingHouse.Id,
            clearingHouse.Name,
            clearingHouse.Code);
    }

    private static X509Certificate2 LoadManagedCertificate(PreviewManagedCertificateRequest request)
    {
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        try
        {
            if (request.Purpose == CertificatePurpose.CfaSigningAndDecryption)
            {
                if (extension is not (".pfx" or ".p12")
                    || X509Certificate2.GetCertContentType(request.Content) != X509ContentType.Pkcs12)
                {
                    throw new CertificateValidationException("El certificado de CFA debe ser un archivo PFX o P12.");
                }
                if (string.IsNullOrEmpty(request.Password))
                {
                    throw new CertificateValidationException("Ingresa la contraseña del certificado.");
                }

                return X509CertificateLoader.LoadPkcs12(
                    request.Content,
                    request.Password,
                    X509KeyStorageFlags.EphemeralKeySet);
            }

            if (extension is not (".cer" or ".crt" or ".pem")
                || X509Certificate2.GetCertContentType(request.Content) == X509ContentType.Pkcs12)
            {
                throw new CertificateValidationException("El certificado de la cámara debe ser un archivo CER, CRT o PEM.");
            }

            return X509CertificateLoader.LoadCertificate(request.Content);
        }
        catch (CertificateValidationException)
        {
            throw;
        }
        catch (CryptographicException) when (request.Purpose == CertificatePurpose.CfaSigningAndDecryption)
        {
            throw new CertificateValidationException("La contraseña suministrada no permite abrir el archivo PFX.");
        }
        catch (CryptographicException)
        {
            throw new CertificateValidationException("El archivo seleccionado no contiene un certificado válido.");
        }
    }

    private static void ValidateManagedCertificatePurpose(
        CertificatePurpose purpose,
        X509Certificate2 certificate)
    {
        if (purpose == CertificatePurpose.CfaSigningAndDecryption)
        {
            if (!certificate.HasPrivateKey)
            {
                throw new CertificateValidationException("El certificado de CFA debe contener una llave privada.");
            }
            ValidateCertificate(certificate, requiresPrivateKey: true, validateDates: false);
            VerifyCertificateAndPrivateKeyMatch(certificate);
            return;
        }

        if (purpose == CertificatePurpose.ClearingHouseValidation)
        {
            if (certificate.HasPrivateKey)
            {
                throw new CertificateValidationException("El certificado de cámara debe contener únicamente la llave pública.");
            }
            ValidateCertificate(certificate, requiresPrivateKey: false, validateDates: false, requiresDigitalSignature: true);
            return;
        }

        throw new CertificateValidationException("El certificado no es compatible con el uso seleccionado.");
    }

    private async Task EnsureNotDuplicateManagedAsync(
        X509Certificate2 certificate,
        CertificatePurpose purpose,
        ManagedCertificateOwner owner,
        CancellationToken cancellationToken)
    {
        var normalizedThumbprint = NormalizeThumbprint(certificate.Thumbprint);
        var duplicates = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Where(x => x.NormalizedThumbprint == normalizedThumbprint || x.Thumbprint == normalizedThumbprint)
            .ToListAsync(cancellationToken);
        if (duplicates.Count == 0)
        {
            return;
        }

        foreach (var revoked in duplicates.Where(x => x.Status == CertificateStatus.Revoked))
        {
            if (await HasOperationalReferencesAsync(revoked.Id, cancellationToken))
            {
                await AuditRejectedDuplicateAsync(
                    normalizedThumbprint,
                    owner,
                    "CERTIFICATE_REVOKED_WITH_REFERENCES",
                    cancellationToken);
                throw new CertificateConflictException("Este certificado fue revocado y tiene operaciones asociadas. Por seguridad, no puede volver a activarse. Carga un certificado de reemplazo.");
            }
        }

        var sameManagedPurpose = duplicates.FirstOrDefault(x => x.Purpose == purpose);
        if (sameManagedPurpose is null)
        {
            // Legacy records used separate signing, decryption, encryption and
            // validation purposes. The managed purpose is a new operational
            // context and must be registered with its resolved owner.
            return;
        }

        await AuditRejectedDuplicateAsync(
            normalizedThumbprint,
            owner,
            "CERTIFICATE_DUPLICATE",
            cancellationToken);

        throw new CertificateConflictException("Este certificado ya se encuentra registrado para el uso seleccionado.");
    }

    private async Task AuditRejectedDuplicateAsync(
        string normalizedThumbprint,
        ManagedCertificateOwner owner,
        string errorCode,
        CancellationToken cancellationToken)
    {
        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = null,
            Action = "duplicate-rejected",
            CertificateThumbprint = normalizedThumbprint,
            CertificateDisplayName = owner.FinancialInstitutionName ?? owner.ClearingHouseName,
            LoadSource = "managed-upload",
            ValidationResult = "REJECTED",
            ValidationErrorsJson = $"{{\"code\":\"{errorCode}\"}}",
            LoadedBy = "application"
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasOperationalReferencesAsync(int versionId, CancellationToken cancellationToken)
        => await _context.CertificateUsageLogs.AsNoTracking().AnyAsync(x => x.CertificateVersionId == versionId, cancellationToken)
           || await _context.DigitalEnvelopeOperationLogs.AsNoTracking().AnyAsync(x => x.CertificateVersionId == versionId, cancellationToken)
           || await _context.CertificateRotationHistories.AsNoTracking().AnyAsync(
               x => x.PreviousVersionId == versionId || x.NewVersionId == versionId,
               cancellationToken)
           || await _context.DigitalCertificateVersions.AsNoTracking().AnyAsync(
               x => x.ReplacedByVersionId == versionId,
               cancellationToken);

    private sealed record ManagedCertificateOwner(
        int? FinancialInstitutionId,
        string? FinancialInstitutionName,
        int? ClearingHouseId,
        string? ClearingHouseName,
        string? ClearingHouseCode);

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

    private async Task<int> GetNextVersionAsync(
        int digitalCertificateId,
        int? financialInstitutionId,
        int? clearingHouseId,
        CertificateEnvironment environment,
        CertificatePurpose purpose,
        CertificateHolderType holderType,
        CancellationToken cancellationToken)
    {
        var max = await _context.DigitalCertificateVersions
            .Where(x => x.DigitalCertificateId == digitalCertificateId
                        && x.FinancialInstitutionId == financialInstitutionId
                        && x.ClearingHouseId == clearingHouseId
                        && x.Environment == environment
                        && x.Purpose == purpose
                        && x.HolderType == holderType)
            .MaxAsync(x => (int?)x.VersionNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    private Task<int> GetNextVersionAsync(
        int digitalCertificateId,
        int clearingHouseId,
        CertificateEnvironment environment,
        CertificatePurpose purpose,
        CertificateHolderType holderType,
        CancellationToken cancellationToken)
        => GetNextVersionAsync(
            digitalCertificateId,
            null,
            clearingHouseId,
            environment,
            purpose,
            holderType,
            cancellationToken);

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

    private static void ValidateCertificate(
        X509Certificate2 certificate,
        bool requiresPrivateKey,
        bool validateDates = true,
        bool requiresDigitalSignature = false)
    {
        var now = DateTime.UtcNow;
        if (validateDates && certificate.NotBefore.ToUniversalTime() > now)
        {
            throw new CertificateValidationException("El certificado todavía no ha llegado a su fecha de inicio.");
        }
        if (validateDates && certificate.NotAfter.ToUniversalTime() <= now)
        {
            throw new CertificateValidationException("El certificado ya se encuentra vencido.");
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
        var validUsage = requiresDigitalSignature
            ? usages.HasFlag(X509KeyUsageFlags.DigitalSignature)
              || usages.HasFlag(X509KeyUsageFlags.NonRepudiation)
            : requiresPrivateKey
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

    private static DigitalCertificateVersion BuildVersionFromCertificate(
        int? financialInstitutionId,
        int? clearingHouseId,
        CertificateEnvironment environment,
        CertificatePurpose purpose,
        CertificateHolderType holderType,
        X509Certificate2 cert,
        string actor,
        string fileName,
        CertificateMaterialType materialType,
        CertificateStorageMode storageMode,
        string? secretRef)
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
            FinancialInstitutionId = financialInstitutionId,
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
            Thumbprint = NormalizeThumbprint(cert.Thumbprint),
            NormalizedThumbprint = NormalizeThumbprint(cert.Thumbprint),
            FingerprintSha256 = fingerprint,
            NotBefore = cert.NotBefore.ToUniversalTime(),
            NotAfter = cert.NotAfter.ToUniversalTime(),
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

    private static DigitalCertificateVersion BuildVersionFromCertificate(
        int clearingHouseId,
        CertificateEnvironment environment,
        CertificatePurpose purpose,
        CertificateHolderType holderType,
        X509Certificate2 cert,
        string actor,
        string fileName,
        CertificateMaterialType materialType,
        CertificateStorageMode storageMode,
        string? secretRef)
        => BuildVersionFromCertificate(
            null,
            clearingHouseId,
            environment,
            purpose,
            holderType,
            cert,
            actor,
            fileName,
            materialType,
            storageMode,
            secretRef);

    internal static string NormalizeThumbprint(string? value)
        => string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static string NormalizeCode(string? value)
    {
        var characters = (value ?? "CAMARA")
            .ToUpperInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        return string.Join('-', new string(characters)
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
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
        var now = DateTime.UtcNow;
        var version = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.Environment == environment
                        && x.Purpose == purpose
                        && x.HolderType == holderType
                        && x.Status == CertificateStatus.Active
                        && x.NotBefore <= now
                        && x.NotAfter > now)
            .OrderByDescending(x => x.VersionNumber)
            .ThenByDescending(x => x.ActivatedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
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
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var activeVersion in currentActive)
        {
            activeVersion.Status = CertificateStatus.Replaced;
            activeVersion.ReplacedByVersionId = version.Id;
            _context.CertificateRotationHistories.Add(new CertificateRotationHistory
            {
                PreviousVersionId = activeVersion.Id,
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
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new CertificateValidationException("Ingresa el motivo de la revocación.");
        }
        if (request.Reason.Trim().Length > 500)
        {
            throw new CertificateValidationException("El motivo de la revocación no puede superar 500 caracteres.");
        }

        var version = await _context.DigitalCertificateVersions
            .Include(x => x.DigitalCertificate)
            .FirstAsync(x => x.Id == request.VersionId, cancellationToken);
        version.Status = CertificateStatus.Revoked;
        version.RevokedAtUtc = DateTime.UtcNow;
        version.RevocationReason = request.Reason.Trim();
        version.RevokedBy = request.RevokedBy;

        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = version.Id,
            Action = "revocation",
            CertificateThumbprint = version.NormalizedThumbprint,
            CertificateDisplayName = version.DigitalCertificate.DisplayName,
            LoadSource = "revocation",
            ValidationResult = "SUCCESS",
            ValidationErrorsJson = System.Text.Json.JsonSerializer.Serialize(new { reason = request.Reason.Trim() }),
            LoadedBy = request.RevokedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        return version.ToDto();
    }
}

[Scoped]
public class CertificateDeletionService : ICertificateDeletionService
{
    private readonly AchDbContext _context;

    public CertificateDeletionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteCertificateVersionResultDto> DeleteVersionAsync(
        DeleteCertificateVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        var version = await _context.DigitalCertificateVersions
            .Include(x => x.DigitalCertificate)
            .SingleOrDefaultAsync(x => x.Id == request.VersionId, cancellationToken)
            ?? throw new CertificateValidationException("No se encontró el certificado solicitado.");

        var hasOperationalReferences =
            await _context.CertificateUsageLogs.AnyAsync(x => x.CertificateVersionId == version.Id, cancellationToken)
            || await _context.DigitalEnvelopeOperationLogs.AnyAsync(x => x.CertificateVersionId == version.Id, cancellationToken)
            || await _context.CertificateRotationHistories.AnyAsync(
                x => x.PreviousVersionId == version.Id || x.NewVersionId == version.Id,
                cancellationToken)
            || await _context.DigitalCertificateVersions.AnyAsync(
                x => x.ReplacedByVersionId == version.Id,
                cancellationToken);

        if (hasOperationalReferences)
        {
            throw new CertificateConflictException("Este certificado no puede eliminarse porque ya fue utilizado. Debe conservarse para mantener la trazabilidad.");
        }

        var now = DateTime.UtcNow;
        var isOperational = version.Status == CertificateStatus.Active
                            && version.NotBefore <= now
                            && version.NotAfter > now;
        if (isOperational)
        {
            throw new CertificateConflictException("Revoca el certificado antes de eliminarlo de forma segura.");
        }

        var loadAudits = await _context.CertificateLoadAudits
            .Where(x => x.CertificateVersionId == version.Id)
            .ToListAsync(cancellationToken);
        foreach (var audit in loadAudits)
        {
            audit.CertificateThumbprint ??= version.NormalizedThumbprint;
            audit.CertificateDisplayName ??= version.DigitalCertificate.DisplayName;
            audit.CertificateVersionId = null;
        }

        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = null,
            Action = "deletion",
            CertificateThumbprint = version.NormalizedThumbprint,
            CertificateDisplayName = version.DigitalCertificate.DisplayName,
            LoadSource = "administration",
            ValidationResult = "SUCCESS",
            LoadedBy = request.DeletedBy
        });

        var aggregate = version.DigitalCertificate;
        _context.DigitalCertificateVersions.Remove(version);
        await _context.SaveChangesAsync(cancellationToken);

        if (!await _context.DigitalCertificateVersions.AnyAsync(
                x => x.DigitalCertificateId == aggregate.Id,
                cancellationToken))
        {
            _context.DigitalCertificates.Remove(aggregate);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (version.EncryptedPrivateMaterial is { Length: > 0 })
        {
            CryptographicOperations.ZeroMemory(version.EncryptedPrivateMaterial);
        }

        return new DeleteCertificateVersionResultDto(request.VersionId, true);
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
    private readonly int _expirationWarningDays;

    public CertificateCatalogService(
        AchDbContext context,
        IOptions<CertificateManagementOptions>? options = null)
    {
        _context = context;
        _expirationWarningDays = Math.Max(0, options?.Value.ExpirationWarningDays ?? 30);
    }

    public async Task<IReadOnlyList<CertificateVersionDto>> GetCertificatesAsync(CertificateFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Include(x => x.FinancialInstitution)
            .Include(x => x.ClearingHouse)
            .AsQueryable();

        if (filter.ClearingHouseId.HasValue) query = query.Where(x => x.ClearingHouseId == filter.ClearingHouseId.Value);
        if (filter.Environment.HasValue) query = query.Where(x => x.Environment == filter.Environment.Value);
        if (filter.Purpose.HasValue) query = query.Where(x => x.Purpose == filter.Purpose.Value);
        if (filter.HolderType.HasValue) query = query.Where(x => x.HolderType == filter.HolderType.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);

        var items = await query.OrderByDescending(x => x.UploadedAtUtc).ToListAsync(cancellationToken);
        var versionIds = items.Select(x => x.Id).ToArray();
        var referenced = new HashSet<int>();
        referenced.UnionWith(await _context.CertificateUsageLogs.AsNoTracking()
            .Where(x => versionIds.Contains(x.CertificateVersionId))
            .Select(x => x.CertificateVersionId)
            .Distinct()
            .ToListAsync(cancellationToken));
        referenced.UnionWith((await _context.DigitalEnvelopeOperationLogs.AsNoTracking()
            .Where(x => x.CertificateVersionId.HasValue && versionIds.Contains(x.CertificateVersionId.Value))
            .Select(x => x.CertificateVersionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken)));
        referenced.UnionWith(await _context.CertificateRotationHistories.AsNoTracking()
            .Where(x => versionIds.Contains(x.PreviousVersionId) || versionIds.Contains(x.NewVersionId))
            .Select(x => x.PreviousVersionId)
            .Distinct()
            .ToListAsync(cancellationToken));
        referenced.UnionWith(await _context.CertificateRotationHistories.AsNoTracking()
            .Where(x => versionIds.Contains(x.PreviousVersionId) || versionIds.Contains(x.NewVersionId))
            .Select(x => x.NewVersionId)
            .Distinct()
            .ToListAsync(cancellationToken));
        referenced.UnionWith(await _context.DigitalCertificateVersions.AsNoTracking()
            .Where(x => x.ReplacedByVersionId.HasValue && versionIds.Contains(x.ReplacedByVersionId.Value))
            .Select(x => x.ReplacedByVersionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken));

        var now = DateTime.UtcNow;
        return items.Select(x =>
        {
            var operational = x.Status == CertificateStatus.Active && x.NotBefore <= now && x.NotAfter > now;
            return x.ToDto(_expirationWarningDays, !operational && !referenced.Contains(x.Id));
        }).ToList();
    }

    public async Task<IReadOnlyList<CertificateVersionDto>> GetVersionsAsync(int digitalCertificateId, CancellationToken cancellationToken = default)
    {
        var items = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Include(x => x.FinancialInstitution)
            .Include(x => x.ClearingHouse)
            .Where(x => x.DigitalCertificateId == digitalCertificateId)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(cancellationToken);

        return items.Select(x => x.ToDto(_expirationWarningDays)).ToList();
    }
}
