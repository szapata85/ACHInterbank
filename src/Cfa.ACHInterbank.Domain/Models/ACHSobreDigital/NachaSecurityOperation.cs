namespace Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

public enum NachaSecurityOperationStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
    Rejected = 4,
    Expired = 5
}

public enum NachaSecurityOperationType
{
    NachaGeneratePlain = 0,
    NachaGenerateEncrypted = 1,
    ManualEnvelopeEncrypt = 2,
    ManualEnvelopeDecrypt = 3,
    DownloadArtifact = 4,
    InteroperabilityRun = 5
}

public class NachaSecurityOperation
{
    public long Id { get; set; }
    public string OperationId { get; set; } = string.Empty;
    public NachaSecurityOperationType OperationType { get; set; }
    public NachaSecurityOperationStatus Status { get; set; }
    public string RequestedBy { get; set; } = "system";
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public int? ClearingHouseId { get; set; }
    public string? Environment { get; set; }
    public string? ExternalFileName { get; set; }
    public string? PlainHashSha256 { get; set; }
    public string? EnvelopeHashSha256 { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessageSanitized { get; set; }
    public bool LegacyFallbackUsed { get; set; }
    public bool FailCloseApplied { get; set; }
    public string? SigningCertificateThumbprintMasked { get; set; }
    public string? EncryptionCertificateThumbprintMasked { get; set; }
    public bool DownloadAvailable { get; set; }
    public DateTime? DownloadAuthorizedUntilUtc { get; set; }
    public DateTime? DownloadExpiresAtUtc { get; set; }
    public string? ArtifactRelativePath { get; set; }
    public string? ArtifactContentType { get; set; }
    public long? ArtifactSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
