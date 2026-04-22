using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;

public sealed record DigitalEnvelopeSignatureValidationRequest(
    SignedData SignedData,
    byte[] PlainContent,
    string? ExpectedSignerThumbprint = null);

public sealed record DigitalEnvelopeSignatureValidationResult(
    bool IsValid,
    bool IsVerified,
    string? SignerCertificateThumbprint,
    string? SignerCertificateSerialNumber,
    string? SignatureAlgorithm,
    string? DigestAlgorithm,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<string> Warnings);

public interface IDigitalEnvelopeSignatureValidator
{
    Task<DigitalEnvelopeSignatureValidationResult> ValidateAsync(
        DigitalEnvelopeSignatureValidationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDigitalEnvelopeSignatureAuditService
{
    Task AuditAsync(
        string result,
        string? errorCode,
        string? signerThumbprint,
        string? signerSerialNumber,
        string? signatureAlgorithm,
        bool failCloseApplied,
        bool legacyBypassUsed,
        string actor,
        CancellationToken cancellationToken = default);
}

public sealed class DigitalEnvelopeSignatureValidationException : InvalidOperationException
{
    public DigitalEnvelopeSignatureValidationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
