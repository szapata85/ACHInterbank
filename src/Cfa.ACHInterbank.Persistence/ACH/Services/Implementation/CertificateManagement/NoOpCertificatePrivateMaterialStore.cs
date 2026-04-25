using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

public class NoOpCertificatePrivateMaterialStore : ICertificatePrivateMaterialStore
{
    public Task<CertificatePrivateMaterialStoreResult> StorePkcs12Async(CertificatePrivateMaterialStoreRequest request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("No hay backend de vault configurado para almacenar material privado.");
    }
}
