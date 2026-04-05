using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Services;

[Scoped]
public class DigitalEnvelopeCertificateService : IDigitalEnvelopeCertificateService
{
    private readonly IDigitalEnvelopeCertificateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DigitalEnvelopeCertificateService(IDigitalEnvelopeCertificateRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<DigitalEnvelopeCertificate>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _repository.ListAsync(cancellationToken);
    }

    public async Task<DigitalEnvelopeCertificate> UpsertAsync(DigitalEnvelopeCertificate certificate, CancellationToken cancellationToken = default)
    {
        var saved = await _repository.UpsertAsync(certificate, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return saved;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
