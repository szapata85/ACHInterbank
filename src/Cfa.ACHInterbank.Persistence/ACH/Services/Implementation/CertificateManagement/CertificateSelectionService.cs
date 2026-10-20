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
public class CertificateSelectionService : ICertificateSelectionService
{
    private readonly AchDbContext _context;

    public CertificateSelectionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<CertificateVersionDto?> SelectActiveAsync(int clearingHouseId, CertificateEnvironment environment, CertificatePurpose purpose, CertificateHolderType holderType, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var version = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.Environment == environment
                        && x.Purpose == purpose
                        && x.HolderType == holderType
                        && x.Status == CertificateStatus.Active
                        && x.NotBefore <= now
                        && x.NotAfter > now)
            .OrderByDescending(x => x.VersionNumber)
            .ThenByDescending(x => x.ActivatedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return version?.ToDto();
    }
}
