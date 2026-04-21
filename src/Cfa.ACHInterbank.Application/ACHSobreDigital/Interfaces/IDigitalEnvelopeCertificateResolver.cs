using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;

public enum DigitalEnvelopeCertificateSource
{
    None = 0,
    CertificateManagement = 1,
    Legacy = 2
}

public sealed record DigitalEnvelopeCertificateResolutionResult(
    bool Success,
    X509Certificate2? Certificate,
    int? CertificateVersionId,
    DigitalEnvelopeCertificateSource Source,
    CertificatePurpose Purpose,
    string? Thumbprint,
    string? SerialNumber,
    string? Subject,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<string> Warnings);

public interface IDigitalEnvelopeCertificateResolver
{
    Task<DigitalEnvelopeCertificateResolutionResult> ResolveAsync(string keyCert, CancellationToken cancellationToken = default);
}
