using System.Data;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public sealed class NachaPublicationSnapshotCoverageSeeder : IDbSeeder
{
    private const string CoverageActor = "system-v1-coverage-backfill";
    private readonly AchDbContext _context;

    public NachaPublicationSnapshotCoverageSeeder(AchDbContext context)
    {
        _context = context;
    }

    // Official profiles and their lifecycle normalization run at 9; naming rules run at 10.
    public int Order => 11;

    public async Task SeedAsync()
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var candidates = await FindCandidatesAsync();
            foreach (var candidate in candidates)
            {
                var snapshots = await LoadPublishSnapshotsAsync(candidate.Id);
                if (CheckCoverage(candidate, snapshots))
                {
                    continue;
                }

                // Existing official publications use the serializer's structural and semantic
                // contract. Draft-only administrative validation is not their publication path.
                var profile = await NachaCompleteProfileQuery.Create(_context).AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == candidate.Id)
                    ?? throw new InvalidOperationException($"SNAPSHOT_COVERAGE_PROFILE_MISSING: {Identity(candidate)}");
                if (!profile.PublishedAt.HasValue || profile.PublishedAt.Value == default
                    || string.IsNullOrWhiteSpace(profile.PublishedBy))
                {
                    throw new InvalidOperationException($"SNAPSHOT_COVERAGE_PROVENANCE_MISSING: {Identity(profile)}");
                }

                NachaPublicationSnapshot artifact;
                try
                {
                    // Legacy SQL/SQLite DateTime materialization can erase Kind; PublishedAt is
                    // persisted by publication paths as UTC and must retain that provenance.
                    var publishedAtUtc = profile.PublishedAt.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(profile.PublishedAt.Value, DateTimeKind.Utc)
                        : profile.PublishedAt.Value.ToUniversalTime();
                    artifact = NachaPublicationSnapshotSerializer.Build(profile, profile.VersionMajor,
                        profile.VersionMinor, publishedAtUtc, profile.PublishedBy);
                }
                catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
                {
                    throw new InvalidOperationException(
                        $"SNAPSHOT_COVERAGE_BUILD_INVALID: {Identity(profile)}: {exception.Message}", exception);
                }
                var json = NachaPublicationSnapshotSerializer.Serialize(artifact);
                var read = NachaPublicationSnapshotSerializer.ReadForTraceLineage(json);
                if (!read.IsSupported || read.Snapshot is null)
                {
                    throw new InvalidOperationException(
                        $"SNAPSHOT_COVERAGE_V1_INCOMPLETE: {Identity(profile)}: {read.Status}: {read.Error}");
                }
                EnsureIdentity(profile, read.Snapshot.Profile);
                _context.HistConfigSnapshots.Add(new HistConfigSnapshot
                {
                    ProfileId = profile.Id,
                    VersionMajor = profile.VersionMajor,
                    VersionMinor = profile.VersionMinor,
                    SnapshotType = "PUBLISH",
                    SnapshotJson = json,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = CoverageActor
                });
            }

            await _context.SaveChangesAsync();
            foreach (var candidate in await FindCandidatesAsync())
            {
                if (!CheckCoverage(candidate, await LoadPublishSnapshotsAsync(candidate.Id)))
                {
                    throw new InvalidOperationException($"SNAPSHOT_COVERAGE_MISSING: {Identity(candidate)}");
                }
            }
            await transaction.CommitAsync();
        });
    }

    private async Task<IReadOnlyList<CfgProfile>> FindCandidatesAsync()
    {
        var profiles = await _context.CfgProfiles.AsNoTracking()
            .Include(profile => profile.Status)
            .Include(profile => profile.ClearingHouse)
            .Include(profile => profile.FlowType)
            .Include(profile => profile.Direction)
            .Include(profile => profile.ServiceClass)
            .Include(profile => profile.Tags)
            .ToListAsync();
        return NachaOrdinaryProductionProfileScope.FindMandatoryWinners(profiles);
    }

    private Task<List<HistConfigSnapshot>> LoadPublishSnapshotsAsync(int profileId)
        => _context.HistConfigSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.ProfileId == profileId && snapshot.SnapshotType == "PUBLISH")
            .ToListAsync();

    // True means exactly one identity-compatible Supported V1. Legacy rows remain untouched.
    private static bool CheckCoverage(CfgProfile profile, IReadOnlyList<HistConfigSnapshot> snapshots)
    {
        var supported = 0;
        foreach (var row in snapshots.Where(row => row.VersionMajor == profile.VersionMajor
                                                   && row.VersionMinor == profile.VersionMinor))
        {
            var read = NachaPublicationSnapshotSerializer.Read(row.SnapshotJson);
            switch (read.Status)
            {
                case NachaPublicationSnapshotReadStatus.Supported:
                    EnsureIdentity(profile, read.Snapshot!.Profile);
                    supported++;
                    break;
                case NachaPublicationSnapshotReadStatus.LegacyOrIncomplete:
                    break;
                default:
                    throw new InvalidOperationException(
                        $"SNAPSHOT_COVERAGE_{read.Status.ToString().ToUpperInvariant()}: {Identity(profile)}: " +
                        $"SnapshotId={row.Id}: {read.Error}");
            }
        }
        if (supported > 1)
        {
            throw new InvalidOperationException($"SNAPSHOT_COVERAGE_DUPLICATE_V1: {Identity(profile)}: Count={supported}");
        }
        return supported == 1;
    }

    private static void EnsureIdentity(CfgProfile profile, NachaPublicationSnapshotProfile snapshot)
    {
        if (snapshot.ProfileId != profile.Id
            || !Equal(snapshot.ProfileCode, profile.ProfileCode)
            || snapshot.VersionMajor != profile.VersionMajor
            || snapshot.VersionMinor != profile.VersionMinor
            || !Equal(snapshot.ClearingHouseCode, profile.ClearingHouse.Code)
            || !Equal(snapshot.FlowTypeCode, profile.FlowType.Code)
            || !Equal(snapshot.DirectionCode, profile.Direction.Code)
            || !Equal(snapshot.ServiceClassCode, profile.ServiceClass?.Code))
        {
            throw new InvalidOperationException($"SNAPSHOT_COVERAGE_IDENTITY_MISMATCH: {Identity(profile)}");
        }
    }

    private static bool Equal(string? left, string? right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string Identity(CfgProfile profile)
        => $"ProfileId={profile.Id}, ProfileCode={profile.ProfileCode}, Version={profile.VersionMajor}.{profile.VersionMinor}";
}
