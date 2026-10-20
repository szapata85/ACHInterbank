using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;

/// <summary>Error funcional almacenado como parte del resultado de una operación de seguridad NACHA.</summary>
/// <param name="Code">Código de error persistido por el servicio.</param>
/// <param name="Message">Mensaje sanitizado persistido para el error.</param>
/// <param name="Retryable">Indica si el contrato marca el error como reintentable.</param>
public sealed record DigitalEnvelopeOperationErrorDto(string Code, string Message, bool Retryable);

/// <summary>Resumen de referencias de certificado asociado a la operación de seguridad.</summary>
/// <param name="SigningCertificateThumbprintMasked">Thumbprint enmascarado del certificado de firma cuando fue registrado por la operación.</param>
/// <param name="EncryptionCertificateThumbprintMasked">Thumbprint enmascarado del certificado de cifrado cuando fue registrado por la operación.</param>
/// <param name="SecretRefMasked">Referencia de secreto enmascarada cuando el contrato la expone; el mapeo actual de operaciones devuelve null.</param>
public sealed record DigitalEnvelopeCertificateSummaryDto(
    string? SigningCertificateThumbprintMasked,
    string? EncryptionCertificateThumbprintMasked,
    string? SecretRefMasked);

/// <summary>Metadata del artefacto producido o asociado a una operación de seguridad NACHA.</summary>
/// <param name="ExternalFileName">Nombre externo persistido para el artefacto cuando existe.</param>
/// <param name="ContentType">Content-Type persistido para el artefacto cuando existe.</param>
/// <param name="PlainHashSha256">Hash SHA-256 del contenido plano cuando la operación lo registra.</param>
/// <param name="EnvelopeHashSha256">Hash SHA-256 del sobre o artefacto encapsulado cuando la operación lo registra.</param>
/// <param name="DownloadAvailable">Indica si la operación conserva un artefacto marcado como disponible para descarga.</param>
/// <param name="DownloadExpiresAtUtc">Fecha y hora de expiración registrada para la disponibilidad del artefacto.</param>
/// <param name="SizeBytes">Tamaño persistido del artefacto en bytes cuando está disponible.</param>
public sealed record DigitalEnvelopeOperationArtifactDto(
    string? ExternalFileName,
    string? ContentType,
    string? PlainHashSha256,
    string? EnvelopeHashSha256,
    bool DownloadAvailable,
    DateTime? DownloadExpiresAtUtc,
    long? SizeBytes);

/// <summary>Resultado y estado observable de una operación de seguridad NACHA persistida.</summary>
/// <param name="OperationId">Identificador público de la operación.</param>
/// <param name="OperationType">Tipo de operación de seguridad registrado.</param>
/// <param name="Status">Estado actual o proyectado de la operación.</param>
/// <param name="ClearingHouseId">Identificador de cámara asociado cuando fue resuelto por la operación.</param>
/// <param name="RequestedBy">Actor registrado como solicitante.</param>
/// <param name="RequestedAtUtc">Fecha y hora UTC de creación de la operación.</param>
/// <param name="FinishedAtUtc">Fecha y hora UTC de finalización cuando existe.</param>
/// <param name="FailCloseApplied">Indica si la operación registró la aplicación de fail-close.</param>
/// <param name="LegacyFallbackUsed">Indica si la operación registró uso de un fallback legacy.</param>
/// <param name="Artifact">Metadata del artefacto asociado a la operación.</param>
/// <param name="Error">Error funcional almacenado cuando la operación terminó con error.</param>
/// <param name="CertificateSummary">Resumen de referencias de certificado registrado por la operación.</param>
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
