using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;

public interface ICertificateCatalogService
{
    Task<IReadOnlyList<CertificateVersionDto>> GetCertificatesAsync(CertificateFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CertificateVersionDto>> GetVersionsAsync(int digitalCertificateId, CancellationToken cancellationToken = default);
}

public interface ICertificateLoadService
{
    Task<CertificateVersionDto> LoadPublicCertificateAsync(LoadPublicCertificateRequest request, CancellationToken cancellationToken = default);
    Task<CertificateVersionDto> RegisterPrivateCertificateAsync(RegisterPrivateCertificateRequest request, CancellationToken cancellationToken = default);
}

public interface ICertificateSelectionService
{
    Task<CertificateVersionDto?> SelectActiveAsync(int clearingHouseId, CertificateEnvironment environment, CertificatePurpose purpose, CertificateHolderType holderType, CancellationToken cancellationToken = default);
}

public interface ICertificateActivationService
{
    Task<CertificateVersionDto> ActivateVersionAsync(ActivateCertificateVersionRequest request, CancellationToken cancellationToken = default);
    Task<CertificateVersionDto> RevokeVersionAsync(RevokeCertificateVersionRequest request, CancellationToken cancellationToken = default);
}

public interface ICertificateRotationService
{
    Task RotateAsync(int previousVersionId, int newVersionId, string reason, string actor, CancellationToken cancellationToken = default);
}

public interface ICertificateValidationService
{
    Task<CertificateValidationResultDto> ValidateForActivationAsync(int versionId, CancellationToken cancellationToken = default);
}

public interface ICertificateSecretProtector
{
    Task EnsureAcceptableAsync(CertificateStorageMode mode, string? secretRef, string? password, CancellationToken cancellationToken = default);
}

public interface ICertificateAuditService
{
    Task<IReadOnlyList<CertificateAuditDto>> ListLoadAuditsAsync(CancellationToken cancellationToken = default);
}

public interface ICertificateUsageLogger
{
    Task LogUsageAsync(int versionId, string operationType, string operationId, string result, string? errorCode, string actor, string? contextJson = null, CancellationToken cancellationToken = default);
}
