namespace Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

public enum CertificatePurpose
{
    OutboundEncryption = 1,
    InboundDecryption = 2,
    OutboundSigning = 3,
    InboundSignatureValidation = 4
}

public enum CertificateEnvironment
{
    Test = 1,
    Production = 2
}

public enum CertificateStatus
{
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Expired = 4,
    Revoked = 5,
    Replaced = 6,
    PendingSecretBinding = 7,
    Invalid = 8
}

public enum CertificateHolderType
{
    Participant = 1,
    ClearingHouse = 2,
    ThirdPartyProvider = 3
}

public enum CertificateStorageMode
{
    DatabaseEncrypted = 1,
    ExternalSecretReference = 2,
    FileReference = 3,
    HsmReference = 4,
    KeyVaultReference = 5
}

public enum CertificateMaterialType
{
    PublicCertificate = 1,
    PrivateKeyPair = 2,
    CertificateChain = 3
}

public class DigitalCertificate
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDeletedLogical { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<DigitalCertificateVersion> Versions { get; set; } = new List<DigitalCertificateVersion>();
}

public class DigitalCertificateVersion
{
    public int Id { get; set; }
    public int DigitalCertificateId { get; set; }
    public DigitalCertificate DigitalCertificate { get; set; } = null!;
    public int ClearingHouseId { get; set; }
    public CertificateEnvironment Environment { get; set; }
    public CertificatePurpose Purpose { get; set; }
    public CertificateHolderType HolderType { get; set; }
    public CertificateStatus Status { get; set; } = CertificateStatus.Draft;
    public CertificateMaterialType MaterialType { get; set; } = CertificateMaterialType.PublicCertificate;
    public int VersionNumber { get; set; } = 1;
    public string FileName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
    public string FingerprintSha256 { get; set; } = string.Empty;
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public bool HasPrivateKey { get; set; }
    public string KeyAlgorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public string SignatureAlgorithm { get; set; } = string.Empty;
    public byte[]? RawPublicCertificate { get; set; }
    public byte[]? EncryptedPrivateMaterial { get; set; }
    public CertificateStorageMode PrivateMaterialStorageMode { get; set; } = CertificateStorageMode.ExternalSecretReference;
    public string? SecretRef { get; set; }
    public string? FileRef { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = "system";
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int? ReplacedByVersionId { get; set; }
    public DigitalCertificateVersion? ReplacedByVersion { get; set; }
    public string? ValidationSummaryJson { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class CertificateUsageLog
{
    public long Id { get; set; }
    public int CertificateVersionId { get; set; }
    public DigitalCertificateVersion CertificateVersion { get; set; } = null!;
    public string OperationType { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string? ContextJson { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByProcess { get; set; } = "system";
}

public class CertificateRotationHistory
{
    public long Id { get; set; }
    public int PreviousVersionId { get; set; }
    public DigitalCertificateVersion PreviousVersion { get; set; } = null!;
    public int NewVersionId { get; set; }
    public DigitalCertificateVersion NewVersion { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateTime RotatedAtUtc { get; set; } = DateTime.UtcNow;
    public string RotatedBy { get; set; } = "system";
    public string? TicketRef { get; set; }
}

public class CertificateLoadAudit
{
    public long Id { get; set; }
    public int CertificateVersionId { get; set; }
    public DigitalCertificateVersion CertificateVersion { get; set; } = null!;
    public string LoadSource { get; set; } = string.Empty;
    public string ValidationResult { get; set; } = string.Empty;
    public string? ValidationErrorsJson { get; set; }
    public DateTime LoadedAtUtc { get; set; } = DateTime.UtcNow;
    public string LoadedBy { get; set; } = "system";
}

public class DigitalEnvelopeOperationLog
{
    public long Id { get; set; }
    public string Direction { get; set; } = string.Empty;
    public int ClearingHouseId { get; set; }
    public CertificateEnvironment Environment { get; set; }
    public CertificatePurpose Purpose { get; set; }
    public int? CertificateVersionId { get; set; }
    public DigitalCertificateVersion? CertificateVersion { get; set; }
    public string? FileNameIn { get; set; }
    public string? FileNameOut { get; set; }
    public string? HashPlainSha256 { get; set; }
    public string? HashEncryptedSha256 { get; set; }
    public long? SizeBefore { get; set; }
    public long? SizeAfter { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string Actor { get; set; } = "system";
}
