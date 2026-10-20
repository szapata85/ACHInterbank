using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

[Scoped]
public class CertificateAuditService : ICertificateAuditService
{
    private readonly AchDbContext _context;

    public CertificateAuditService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CertificateAuditDto>> ListLoadAuditsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CertificateLoadAudits
            .AsNoTracking()
            .OrderByDescending(x => x.LoadedAtUtc)
            .Select(x => new CertificateAuditDto(x.Id, x.CertificateVersionId, x.LoadSource, x.ValidationResult, x.ValidationErrorsJson, x.LoadedAtUtc, x.LoadedBy))
            .ToListAsync(cancellationToken);
    }
}
