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
public class CertificateRotationService : ICertificateRotationService
{
    private readonly AchDbContext _context;

    public CertificateRotationService(AchDbContext context)
    {
        _context = context;
    }

    public async Task RotateAsync(int previousVersionId, int newVersionId, string reason, string actor, CancellationToken cancellationToken = default)
    {
        _context.CertificateRotationHistories.Add(new CertificateRotationHistory
        {
            PreviousVersionId = previousVersionId,
            NewVersionId = newVersionId,
            Reason = reason,
            RotatedBy = actor,
            RotatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
