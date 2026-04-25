using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;

public enum CertificateSecretProviderType
{
    None = 0,
    InMemory = 1,
    ExternalSecretReference = 2,
    KeyVault = 3,
    Hsm = 4,
    OpenBao = 5
}

public sealed record CertificateSecretResolutionRequest(
    int CertificateVersionId,
    CertificatePurpose Purpose,
    CertificateStorageMode StorageMode,
    string SecretRef,
    string Actor);

public sealed record CertificateSecretMaterial(
    X509Certificate2 Certificate,
    string Thumbprint,
    string SerialNumber,
    string Subject,
    bool HasPrivateKey);

public sealed record CertificateSecretResolutionResult(
    bool Success,
    CertificateSecretProviderType ProviderType,
    CertificateSecretMaterial? Material,
    string? ErrorCode,
    string? ErrorMessage,
    string SecretRefMasked);

public interface ICertificateSecretProvider
{
    CertificateSecretProviderType ProviderType { get; }
    bool Supports(CertificateStorageMode storageMode);
    Task<CertificateSecretResolutionResult> ResolveAsync(CertificateSecretResolutionRequest request, CancellationToken cancellationToken = default);
}

public interface ICertificateSecretProviderResolver
{
    ICertificateSecretProvider Resolve(CertificateStorageMode storageMode);
}

public interface ICertificateSecretResolver
{
    Task<CertificateSecretResolutionResult> ResolveAsync(CertificateSecretResolutionRequest request, CancellationToken cancellationToken = default);
}

public interface IInMemoryCertificateSecretStore
{
    void Set(string secretRef, byte[] rawPkcs12, string? password);
    bool TryGet(string secretRef, out (byte[] RawPkcs12, string? Password) material);
}
