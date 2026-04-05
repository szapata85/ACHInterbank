using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;

public interface IDigitalEnvelopeCertificateService
{
    Task<IReadOnlyList<DigitalEnvelopeCertificate>> ListAsync(CancellationToken cancellationToken = default);
    Task<DigitalEnvelopeCertificate> UpsertAsync(DigitalEnvelopeCertificate certificate, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
