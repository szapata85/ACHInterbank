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
public class CertificateCatalogService : ICertificateCatalogService
{
    private readonly AchDbContext _context;
    private readonly int _expirationWarningDays;

    public CertificateCatalogService(
        AchDbContext context,
        IOptions<CertificateManagementOptions>? options = null)
    {
        _context = context;
        _expirationWarningDays = Math.Max(0, options?.Value.ExpirationWarningDays ?? 30);
    }

    public async Task<IReadOnlyList<CertificateVersionDto>> GetCertificatesAsync(CertificateFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Include(x => x.FinancialInstitution)
            .Include(x => x.ClearingHouse)
            .AsQueryable();

        if (filter.ClearingHouseId.HasValue) query = query.Where(x => x.ClearingHouseId == filter.ClearingHouseId.Value);
        if (filter.Environment.HasValue) query = query.Where(x => x.Environment == filter.Environment.Value);
        if (filter.Purpose.HasValue) query = query.Where(x => x.Purpose == filter.Purpose.Value);
        if (filter.HolderType.HasValue) query = query.Where(x => x.HolderType == filter.HolderType.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);

        var items = await query.OrderByDescending(x => x.UploadedAtUtc).ToListAsync(cancellationToken);
        var versionIds = items.Select(x => x.Id).ToArray();
        var referenced = new HashSet<int>();
        referenced.UnionWith(await _context.CertificateUsageLogs.AsNoTracking()
            .Where(x => versionIds.Contains(x.CertificateVersionId))
            .Select(x => x.CertificateVersionId)
            .Distinct()
            .ToListAsync(cancellationToken));
        referenced.UnionWith((await _context.DigitalEnvelopeOperationLogs.AsNoTracking()
            .Where(x => x.CertificateVersionId.HasValue && versionIds.Contains(x.CertificateVersionId.Value))
            .Select(x => x.CertificateVersionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken)));
        referenced.UnionWith(await _context.CertificateRotationHistories.AsNoTracking()
            .Where(x => versionIds.Contains(x.PreviousVersionId) || versionIds.Contains(x.NewVersionId))
            .Select(x => x.PreviousVersionId)
            .Distinct()
            .ToListAsync(cancellationToken));
        referenced.UnionWith(await _context.CertificateRotationHistories.AsNoTracking()
            .Where(x => versionIds.Contains(x.PreviousVersionId) || versionIds.Contains(x.NewVersionId))
            .Select(x => x.NewVersionId)
            .Distinct()
            .ToListAsync(cancellationToken));
        referenced.UnionWith(await _context.DigitalCertificateVersions.AsNoTracking()
            .Where(x => x.ReplacedByVersionId.HasValue && versionIds.Contains(x.ReplacedByVersionId.Value))
            .Select(x => x.ReplacedByVersionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken));

        var now = DateTime.UtcNow;
        return items.Select(x =>
        {
            var operational = x.Status == CertificateStatus.Active && x.NotBefore <= now && x.NotAfter > now;
            return x.ToDto(_expirationWarningDays, !operational && !referenced.Contains(x.Id));
        }).ToList();
    }

    public async Task<IReadOnlyList<CertificateVersionDto>> GetVersionsAsync(int digitalCertificateId, CancellationToken cancellationToken = default)
    {
        var items = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Include(x => x.FinancialInstitution)
            .Include(x => x.ClearingHouse)
            .Where(x => x.DigitalCertificateId == digitalCertificateId)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(cancellationToken);

        return items.Select(x => x.ToDto(_expirationWarningDays)).ToList();
    }
}
