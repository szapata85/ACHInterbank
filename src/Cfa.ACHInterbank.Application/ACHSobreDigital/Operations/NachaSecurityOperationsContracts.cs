using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;

public sealed record DigitalEnvelopeOperationErrorDto(string Code, string Message, bool Retryable);

public sealed record DigitalEnvelopeCertificateSummaryDto(
    string? SigningCertificateThumbprintMasked,
    string? EncryptionCertificateThumbprintMasked,
    string? SecretRefMasked);

public sealed record DigitalEnvelopeOperationArtifactDto(
    string? ExternalFileName,
    string? ContentType,
    string? PlainHashSha256,
    string? EnvelopeHashSha256,
    bool DownloadAvailable,
    DateTime? DownloadExpiresAtUtc,
    long? SizeBytes);

public sealed record DigitalEnvelopeOperationDto(
    string OperationId,
    NachaSecurityOperationType OperationType,
    NachaSecurityOperationStatus Status,
    int? ClearingHouseId,
    string RequestedBy,
    DateTime RequestedAtUtc,
    DateTime? FinishedAtUtc,
    bool FailCloseApplied,
    bool LegacyFallbackUsed,
    DigitalEnvelopeOperationArtifactDto Artifact,
    DigitalEnvelopeOperationErrorDto? Error,
    DigitalEnvelopeCertificateSummaryDto CertificateSummary);

public sealed record OperationRequestContext(string RequestedBy, string? SourceIp);

public sealed record NachaGenerateRequest(string CycleId, bool ForceEncryption = false);

public sealed record ManualEnvelopeRequest(string OriginalFileName, byte[] FileContent);

public sealed record DownloadAuthorizationResult(bool Authorized, DateTime? ExpiresAtUtc, string? Code, string? Message);

public sealed record OperationDownloadDescriptor(string FileName, string ContentType, Stream Content, DateTime? ExpiresAtUtc);

public interface INachaSecurityOperationService
{
    Task<DigitalEnvelopeOperationDto> GeneratePlainAsync(NachaGenerateRequest request, OperationRequestContext context, CancellationToken cancellationToken = default);
    Task<DigitalEnvelopeOperationDto> GenerateEncryptedAsync(NachaGenerateRequest request, OperationRequestContext context, CancellationToken cancellationToken = default);
    Task<DigitalEnvelopeOperationDto> ManualEncryptAsync(ManualEnvelopeRequest request, OperationRequestContext context, CancellationToken cancellationToken = default);
    Task<DigitalEnvelopeOperationDto> ManualDecryptAsync(ManualEnvelopeRequest request, OperationRequestContext context, CancellationToken cancellationToken = default);
    Task<DigitalEnvelopeOperationDto?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DigitalEnvelopeOperationDto>> ListAuditAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<DownloadAuthorizationResult> AuthorizeDownloadAsync(string operationId, OperationRequestContext context, CancellationToken cancellationToken = default);
    Task<OperationDownloadDescriptor?> OpenDownloadAsync(string operationId, OperationRequestContext context, CancellationToken cancellationToken = default);
}

public sealed class OperationArtifactOptions
{
    public const string SectionName = "NachaSecurityOperations:ArtifactStore";

    public string BasePath { get; set; } = Path.Combine(Path.GetTempPath(), "achinterbank", "operations");
    public int DefaultExpirationMinutes { get; set; } = 30;
    public int MaxFileSizeMb { get; set; } = 50;
}

public interface IOperationArtifactStore
{
    Task<string> SaveAsync(string operationId, string extension, byte[] content, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
