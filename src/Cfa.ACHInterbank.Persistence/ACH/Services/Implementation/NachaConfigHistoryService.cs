using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigHistoryService : INachaConfigHistoryService
{
    private readonly AchDbContext _context;

    public NachaConfigHistoryService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NachaConfigHistoryItemDto>> GetHistoryAsync(int profileId, CancellationToken ct = default)
    {
        return await _context.HistConfigChanges
            .AsNoTracking()
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.ChangedAtUtc)
            .Select(x => new NachaConfigHistoryItemDto
            {
                Id = x.Id,
                ChangedAtUtc = x.ChangedAtUtc,
                ChangedBy = x.ChangedBy,
                ChangeType = x.ChangeType,
                EntityName = x.EntityName,
                CorrelationId = x.CorrelationId
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NachaConfigSnapshotItemDto>> GetSnapshotsAsync(int profileId, CancellationToken ct = default)
    {
        return await _context.HistConfigSnapshots
            .AsNoTracking()
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NachaConfigSnapshotItemDto
            {
                Id = x.Id,
                CreatedAtUtc = x.CreatedAtUtc,
                CreatedBy = x.CreatedBy,
                SnapshotType = x.SnapshotType,
                VersionMajor = x.VersionMajor,
                VersionMinor = x.VersionMinor
            })
            .ToListAsync(ct);
    }
}
