using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;

public sealed record ManagedDigitalEnvelopeCertificateDto(
    int Id,
    string Code,
    string DisplayName,
    string FileName,
    CertificatePurpose Purpose,
    bool HasPrivateKey,
    string ThumbprintMasked,
    DateTime NotBefore,
    DateTime NotAfter,
    bool CanEncrypt,
    bool CanDecrypt);

public sealed record ManagedDigitalEnvelopeRequest(
    int CertificateVersionId,
    string FileName,
    byte[] Content,
    string Actor);

public sealed record ManagedDigitalEnvelopeResult(
    byte[] Content,
    string FileName,
    string ContentType,
    int CertificateVersionId,
    string Thumbprint,
    string CryptographicProfile);

public interface IManagedDigitalEnvelopeService
{
    Task<IReadOnlyList<ManagedDigitalEnvelopeCertificateDto>> ListUsableCertificatesAsync(
        CancellationToken cancellationToken = default);

    Task<ManagedDigitalEnvelopeResult> EncryptAsync(
        ManagedDigitalEnvelopeRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagedDigitalEnvelopeResult> DecryptAsync(
        ManagedDigitalEnvelopeRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ManagedDigitalEnvelopeException : InvalidOperationException
{
    public ManagedDigitalEnvelopeException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
