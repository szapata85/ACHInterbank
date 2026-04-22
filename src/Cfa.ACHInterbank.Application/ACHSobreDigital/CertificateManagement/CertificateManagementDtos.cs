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
    string UploadedBy);

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
    string? SecretRef);

public record ActivateCertificateVersionRequest(int VersionId, string ActivatedBy);

public record RevokeCertificateVersionRequest(int VersionId, string RevokedBy, string Reason);

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
    int ClearingHouseId,
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
    DateTime? RevokedAtUtc);

public record CertificateAuditDto(
    long Id,
    int CertificateVersionId,
    string LoadSource,
    string ValidationResult,
    string? ValidationErrorsJson,
    DateTime LoadedAtUtc,
    string LoadedBy);
