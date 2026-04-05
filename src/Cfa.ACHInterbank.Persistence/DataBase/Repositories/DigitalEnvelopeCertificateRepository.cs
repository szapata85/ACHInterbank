using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase.Repositories;

[Scoped]
public class DigitalEnvelopeCertificateRepository : IDigitalEnvelopeCertificateRepository
{
    private readonly AchDbContext _dbContext;

    public DigitalEnvelopeCertificateRepository(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DigitalEnvelopeCertificate?> GetLatestAsync(DigitalEnvelopeCertificateType type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DigitalEnvelopeCertificates
            .AsNoTracking()
            .Where(c => c.Type == type)
            .OrderByDescending(c => c.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DigitalEnvelopeCertificate>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.DigitalEnvelopeCertificates
            .AsNoTracking()
            .OrderBy(c => c.Type)
            .ThenByDescending(c => c.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<DigitalEnvelopeCertificate> UpsertAsync(DigitalEnvelopeCertificate certificate, CancellationToken cancellationToken = default)
    {
        // replace existing certificate of the same type to keep a single active copy
        var existing = await _dbContext.DigitalEnvelopeCertificates
            .Where(c => c.Type == certificate.Type)
            .ToListAsync(cancellationToken);

        if (existing.Any())
        {
            _dbContext.DigitalEnvelopeCertificates.RemoveRange(existing);
        }

        certificate.UploadedAt = DateTime.UtcNow;
        await _dbContext.DigitalEnvelopeCertificates.AddAsync(certificate, cancellationToken);
        return certificate;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.DigitalEnvelopeCertificates.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _dbContext.DigitalEnvelopeCertificates.Remove(entity);
        }
    }
}
