namespace Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;

public sealed record CertificatePrivateMaterialStoreRequest(
    int ClearingHouseId,
    string Environment,
    string Purpose,
    int Version,
    byte[] RawPkcs12,
    string Password,
    string Actor);

public sealed record CertificatePrivateMaterialStoreResult(
    string SecretRef,
    string SecretRefMasked,
    string Backend);

public interface ICertificatePrivateMaterialStore
{
    Task<CertificatePrivateMaterialStoreResult> StorePkcs12Async(
        CertificatePrivateMaterialStoreRequest request,
        CancellationToken cancellationToken = default);
}
