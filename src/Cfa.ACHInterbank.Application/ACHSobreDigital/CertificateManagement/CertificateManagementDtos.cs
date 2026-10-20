using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;

public record LoadPublicCertificateRequest(
    string Code,
    string DisplayName,
    int ClearingHouseId,
    CertificateEnvironment Environment,
    CertificatePurpose Purpose,
    CertificateHolderType HolderType,
    byte[] RawCertificate,
    string UploadedBy,
    string FileName = "");

public record RegisterPrivateCertificateRequest(
    string Code,
    string DisplayName,
    int ClearingHouseId,
    CertificateEnvironment Environment,
    CertificatePurpose Purpose,
    CertificateHolderType HolderType,
    byte[] RawPkcs12,
    string Password,
    string UploadedBy,
    CertificateStorageMode StorageMode,
    string? SecretRef,
    string FileName = "");

public record ActivateCertificateVersionRequest(int VersionId, string ActivatedBy);

public record RevokeCertificateVersionRequest(int VersionId, string RevokedBy, string Reason);

public record PreviewManagedCertificateRequest(
    CertificatePurpose Purpose,
    int? ClearingHouseId,
    byte[] Content,
    string? Password,
    string FileName);

public record SaveManagedCertificateRequest(
    CertificatePurpose Purpose,
    int? ClearingHouseId,
    byte[] Content,
    string? Password,
    string FileName,
    string UploadedBy);

public record DeleteCertificateVersionRequest(int VersionId, string DeletedBy);

public record CertificateFilterDto(
    int? ClearingHouseId,
    CertificateEnvironment? Environment,
    CertificatePurpose? Purpose,
    CertificateHolderType? HolderType,
    CertificateStatus? Status);

public record CertificateValidationResultDto(bool IsValid, IReadOnlyList<string> Errors);

public record CertificateVersionDto(
    int Id,
    string Code,
    string DisplayName,
    int? ClearingHouseId,
    CertificateEnvironment Environment,
    CertificatePurpose Purpose,
    CertificateHolderType HolderType,
    CertificateStatus Status,
    int VersionNumber,
    string Subject,
    string Issuer,
    string SerialNumber,
    string Thumbprint,
    string FingerprintSha256,
    DateTime NotBefore,
    DateTime NotAfter,
    bool HasPrivateKey,
    string KeyAlgorithm,
    int KeySize,
    string SignatureAlgorithm,
    string? SecretRef,
    DateTime UploadedAtUtc,
    string UploadedBy,
    DateTime? ActivatedAtUtc,
    DateTime? RevokedAtUtc,
    string FileName = "",
    int? FinancialInstitutionId = null,
    string? FinancialInstitutionName = null,
    string? ClearingHouseName = null,
    CertificateFunctionalStatus FunctionalStatus = CertificateFunctionalStatus.Inactive,
    int? DaysRemaining = null,
    string? RevocationReason = null,
    string? RevokedBy = null,
    bool CanDelete = false);

/// <summary>Resultado de previsualización de un certificado antes de persistirlo.</summary>
/// <param name="Purpose">Uso funcional evaluado para el certificado.</param>
/// <param name="FinancialInstitutionId">Identificador de la entidad financiera asociada cuando aplica.</param>
/// <param name="FinancialInstitutionName">Nombre de la entidad financiera asociada cuando aplica.</param>
/// <param name="ClearingHouseId">Identificador de la cámara asociada cuando aplica.</param>
/// <param name="ClearingHouseName">Nombre de la cámara asociada cuando aplica.</param>
/// <param name="Subject">Subject extraído del certificado.</param>
/// <param name="Issuer">Issuer extraído del certificado.</param>
/// <param name="SerialNumber">Número de serie extraído del certificado.</param>
/// <param name="Thumbprint">Thumbprint extraído del certificado.</param>
/// <param name="NotBefore">Fecha inicial de vigencia contenida en el certificado.</param>
/// <param name="NotAfter">Fecha final de vigencia contenida en el certificado.</param>
/// <param name="HasPrivateKey">Indica si el archivo evaluado contiene llave privada.</param>
/// <param name="KeyAlgorithm">Algoritmo de llave pública identificado por el servicio.</param>
/// <param name="KeySize">Tamaño de llave identificado por el servicio.</param>
/// <param name="SignatureAlgorithm">Algoritmo de firma identificado en el certificado.</param>
/// <param name="FunctionalStatus">Estado funcional calculado durante la previsualización.</param>
/// <param name="DaysRemaining">Días restantes calculados cuando el servicio puede determinarlos.</param>
/// <param name="CanSignAndDecrypt">Indica si el certificado evaluado puede usarse para firma y descifrado según la validación ejecutada.</param>
/// <param name="IsValid">Resultado consolidado de la validación realizada por el servicio.</param>
/// <param name="Warnings">Advertencias generadas por la validación cuando existen.</param>
public record CertificatePreviewDto(
    CertificatePurpose Purpose,
    int? FinancialInstitutionId,
    string? FinancialInstitutionName,
    int? ClearingHouseId,
    string? ClearingHouseName,
    string Subject,
    string Issuer,
    string SerialNumber,
    string Thumbprint,
    DateTime NotBefore,
    DateTime NotAfter,
    bool HasPrivateKey,
    string KeyAlgorithm,
    int KeySize,
    string SignatureAlgorithm,
    CertificateFunctionalStatus FunctionalStatus,
    int? DaysRemaining,
    bool CanSignAndDecrypt,
    bool IsValid,
    IReadOnlyList<string> Warnings);

public record DeleteCertificateVersionResultDto(int VersionId, bool Deleted);

public record CertificateAuditDto(
    long Id,
    int? CertificateVersionId,
    string LoadSource,
    string ValidationResult,
    string? ValidationErrorsJson,
    DateTime LoadedAtUtc,
    string LoadedBy);
