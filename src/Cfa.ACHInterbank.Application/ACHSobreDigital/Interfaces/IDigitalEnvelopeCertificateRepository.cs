using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;

public interface IDigitalEnvelopeCertificateRepository
{
    Task<DigitalEnvelopeCertificate?> GetLatestAsync(DigitalEnvelopeCertificateType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DigitalEnvelopeCertificate>> ListAsync(CancellationToken cancellationToken = default);
    Task<DigitalEnvelopeCertificate> SaveAsync(DigitalEnvelopeCertificate certificate, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
