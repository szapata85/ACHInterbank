using System.Text.Json.Nodes;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaPublicationSnapshotCoverageSeederTests : IClassFixture<OfficialNachaGenerationFixture>
{
    private readonly OfficialNachaGenerationFixture _fixture;

    public NachaPublicationSnapshotCoverageSeederTests(OfficialNachaGenerationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SupportedOrdinaryDomain_SelectsVersionedDimensionsAndExcludesLegacy()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var profiles = await SelectionProfilesAsync(context);
        var achOriginal = profiles.Single(profile => profile.ClearingHouse.Code == "ACH"
                                                     && profile.FlowType.Code == "ORIGINAL"
                                                     && profile.Direction.Code == "SALIDA");
        var legacy = Clone(achOriginal, 90001, 1, 0, new DateTime(2020, 1, 1), null, 100);
        legacy.ProfileCode = "LEGACY_ACH_SALIDA_ORIGINAL_V1_0";
        profiles.Add(legacy);

        var winners = NachaOrdinaryProductionProfileScope.FindMandatoryWinners(profiles);
        winners.Should().HaveCount(12);
        winners.Should().OnlyContain(profile => profile.FlowType.Code == "ORIGINAL" || profile.FlowType.Code == "PRENOTIFICACION");
        winners.Should().OnlyContain(profile => profile.ClearingHouse.Code == "ACH" ? profile.VersionMajor == 35 && profile.VersionMinor == 0
            : profile.ClearingHouse.Code == "CENIT" && (profile.Direction.Code == "SALIDA" || profile.VersionMajor == 1 && profile.VersionMinor == 0));
        winners.Should().ContainSingle(profile => profile.ClearingHouse.Code == "ACH" && profile.Direction.Code == "SALIDA"
            && profile.FlowType.Code == "ORIGINAL" && profile.VersionMajor == 35);
        winners.Should().ContainSingle(profile => profile.ClearingHouse.Code == "ACH" && profile.Direction.Code == "SALIDA"
            && profile.FlowType.Code == "PRENOTIFICACION" && profile.VersionMajor == 35);
        winners.Should().Contain(profile => profile.ClearingHouse.Code == "ACH" && profile.Direction.Code == "ENTRADA"
            && profile.VersionMajor == 35);
        winners.Should().Contain(profile => profile.ClearingHouse.Code == "CENIT" && profile.Direction.Code == "ENTRADA"
            && profile.VersionMajor == 1 && profile.VersionMinor == 0);
        winners.Should().NotContain(profile => profile.Id == legacy.Id);
    }

    [Fact]
    public async Task CenitUnpinnedDomain_UsesEffectiveBoundariesPriorityAndHighestVersion()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var source = (await SelectionProfilesAsync(context)).Single(profile => profile.ClearingHouse.Code == "CENIT"
            && profile.FlowType.Code == "ORIGINAL" && profile.Direction.Code == "SALIDA" && profile.ServiceClass is null);
        var old = Clone(source, 90010, 1, 0, new DateTime(2026, 1, 1), new DateTime(2026, 2, 28), 100);
        var newer = Clone(source, 90011, 2, 0, new DateTime(2026, 3, 1), null, 100);
        var superseded = Clone(source, 90012, 1, 1, new DateTime(2026, 3, 1), null, 100);
        var lowPriority = Clone(source, 90013, 3, 0, new DateTime(2026, 1, 1), null, 200);
        var preferred = Clone(source, 90014, 1, 5, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), 50);
        var scope = new NachaOrdinaryProductionProfileScope("CENIT", "ORIGINAL", "SALIDA", "PPD", null, null);

        scope.FindWinners([old, newer, superseded, lowPriority, preferred])
            .Select(profile => profile.Id).Should().BeEquivalentTo([old.Id, newer.Id, preferred.Id]);
        var fixedScope = scope with { RequestedVersionMajor = 1, RequestedVersionMinor = 0 };
        fixedScope.FindWinners([old, newer, superseded, lowPriority, preferred])
            .Select(profile => profile.Id).Should().Equal(old.Id);
    }

    [Fact]
    public async Task ServiceSpecificityAndAmbiguity_MatchResolverCandidateSelection()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var profiles = await SelectionProfilesAsync(context);
        var generic = profiles.Single(profile => profile.ClearingHouse.Code == "CENIT"
            && profile.FlowType.Code == "ORIGINAL" && profile.Direction.Code == "SALIDA" && profile.ServiceClass is null);
        var ctxSource = profiles.Single(profile => profile.ClearingHouse.Code == "CENIT"
            && profile.FlowType.Code == "ORIGINAL" && profile.Direction.Code == "SALIDA" && profile.ServiceClass?.Code == "CTX");
        var ctx = Clone(ctxSource, 90020, 2, 0, new DateTime(2026, 1, 1), null, 100);
        var ppdScope = new NachaOrdinaryProductionProfileScope("CENIT", "ORIGINAL", "SALIDA", "PPD", null, null);
        var ctxScope = ppdScope with { ServiceClassCode = "CTX" };

        ppdScope.FindWinners([generic, ctx]).Select(profile => profile.Id).Should().Equal(generic.Id);
        ctxScope.FindWinners([generic, ctx]).Select(profile => profile.Id).Should().Equal(ctx.Id);

        var duplicate = Clone(ctx, 90021, 2, 0, ctx.EffectiveFrom, null, ctx.ContextPriority);
        var action = () => ctxScope.FindWinners([generic, ctx, duplicate]);
        action.Should().Throw<InvalidOperationException>().WithMessage("ORDINARY_PROFILE_AMBIGUOUS*");
    }

    [Fact]
    public async Task MissingV1_BackfillsCurrentPublishedGraphWithoutChangingGenerationDefinitions()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var row = await TargetSnapshotAsync(context);
        var originalJson = row.SnapshotJson;
        var originalProfile = NachaPublicationSnapshotSerializer.Read(originalJson).Snapshot!.Profile;
        context.HistConfigSnapshots.Remove(row);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await Seeder(context).SeedAsync();
        var result = await TargetSnapshotsAsync(context);
        result.Should().ContainSingle();
        result[0].CreatedBy.Should().Be("system-v1-coverage-backfill");
        result[0].SnapshotJson.Should().Be(originalJson);
        var read = NachaPublicationSnapshotSerializer.Read(result[0].SnapshotJson);
        read.Status.Should().Be(NachaPublicationSnapshotReadStatus.Supported);
        read.Snapshot!.Profile.Should().BeEquivalentTo(originalProfile);
        read.Snapshot.Records.Single(record => record.RecordCode == "5").SemanticRuleSet!.ResolvedDeclarations.Should().BeEquivalentTo(
            [new NachaPublicationSnapshotSemanticDeclaration("200", true, true),
             new NachaPublicationSnapshotSemanticDeclaration("220", true, false),
             new NachaPublicationSnapshotSemanticDeclaration("225", false, true)]);

        await Seeder(context).SeedAsync();
        (await TargetSnapshotsAsync(context)).Should().ContainSingle();
        (await TargetSnapshotsAsync(context))[0].SnapshotJson.Should().Be(originalJson);
    }

    [Fact]
    public async Task LegacyPublishRow_IsPreservedAlongsideNewV1()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var row = await TargetSnapshotAsync(context);
        row.SnapshotJson = "{\"profileCode\":\"historical\"}";
        await context.SaveChangesAsync();
        var legacyId = row.Id;
        var legacyCreated = row.CreatedAtUtc;
        context.ChangeTracker.Clear();

        await Seeder(context).SeedAsync();
        var rows = await TargetSnapshotsAsync(context);
        rows.Should().HaveCount(2);
        rows.Single(snapshot => snapshot.Id == legacyId).SnapshotJson.Should().Be("{\"profileCode\":\"historical\"}");
        rows.Single(snapshot => snapshot.Id == legacyId).CreatedAtUtc.Should().Be(legacyCreated);
        rows.Count(snapshot => NachaPublicationSnapshotSerializer.Read(snapshot.SnapshotJson).IsSupported).Should().Be(1);
    }

    [Fact]
    public async Task ExistingV1_IsStrictNoOp()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var before = await context.HistConfigSnapshots.AsNoTracking().OrderBy(row => row.Id)
            .Select(row => new { row.Id, row.SnapshotJson, row.CreatedAtUtc, row.CreatedBy }).ToArrayAsync();
        var historyCount = await context.HistConfigChanges.CountAsync();

        await Seeder(context).SeedAsync();

        var after = await context.HistConfigSnapshots.AsNoTracking().OrderBy(row => row.Id)
            .Select(row => new { row.Id, row.SnapshotJson, row.CreatedAtUtc, row.CreatedBy }).ToArrayAsync();
        after.Should().Equal(before);
        (await context.HistConfigChanges.CountAsync()).Should().Be(historyCount);
    }

    [Theory]
    [InlineData("duplicate", "SNAPSHOT_COVERAGE_DUPLICATE_V1")]
    [InlineData("malformed", "SNAPSHOT_COVERAGE_MALFORMED")]
    [InlineData("unsupported", "SNAPSHOT_COVERAGE_UNSUPPORTEDVERSION")]
    [InlineData("identity", "SNAPSHOT_COVERAGE_IDENTITY_MISMATCH")]
    public async Task InvalidPublishArtifacts_FailClosedWithoutMutation(string caseName, string error)
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var row = await TargetSnapshotAsync(context);
        if (caseName == "duplicate")
        {
            context.HistConfigSnapshots.Add(new HistConfigSnapshot
            {
                ProfileId = row.ProfileId, VersionMajor = row.VersionMajor, VersionMinor = row.VersionMinor,
                SnapshotType = "PUBLISH", SnapshotJson = row.SnapshotJson,
                CreatedAtUtc = row.CreatedAtUtc, CreatedBy = "test"
            });
        }
        else if (caseName == "identity")
        {
            var json = JsonNode.Parse(row.SnapshotJson)!;
            json["profile"]!["profileCode"] = "WRONG_IDENTITY";
            row.SnapshotJson = json.ToJsonString();
        }
        else
        {
            row.SnapshotJson = caseName == "malformed" ? "not-json" : "{\"snapshotFormatVersion\":999}";
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var count = await context.HistConfigSnapshots.CountAsync();

        var action = () => Seeder(context).SeedAsync();
        (await action.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain(error);
        (await context.HistConfigSnapshots.CountAsync()).Should().Be(count);
    }

    [Fact]
    public async Task InvalidPublishedGraph_FailsWithoutBackfillOrGraphRepair()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var row = await TargetSnapshotAsync(context);
        context.HistConfigSnapshots.Remove(row);
        var record = await context.CfgProfileRecords.SingleAsync(item => item.ProfileId == row.ProfileId && item.RecordCode.Code == "5");
        record.SemanticRuleSetId = null;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var action = () => Seeder(context).SeedAsync();
        (await action.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("SNAPSHOT_COVERAGE_BUILD_INVALID");
        (await TargetSnapshotsAsync(context)).Should().BeEmpty();
        (await context.CfgProfileRecords.SingleAsync(item => item.Id == record.Id)).SemanticRuleSetId.Should().BeNull();
    }

    [Fact]
    public async Task PublishedLegacyVersion_IsOutsideDomainAndRemainsUntouched()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var ach = (await SelectionProfilesAsync(context)).Single(profile => profile.ClearingHouse.Code == "ACH"
            && profile.FlowType.Code == "ORIGINAL" && profile.Direction.Code == "SALIDA");
        var legacy = Clone(ach, 0, 1, 0, new DateTime(2020, 1, 1), null, 100);
        legacy.ProfileCode = "LEGACY_ACH_SALIDA_ORIGINAL_V1_0";
        legacy.PublishedAt = new DateTime(2020, 1, 1);
        legacy.PublishedBy = "system-backfill";
        legacy.ClearingHouse = null!;
        legacy.FlowType = null!;
        legacy.Direction = null!;
        legacy.ServiceClass = null;
        legacy.Status = null!;
        legacy.Tags = new List<CfgProfileTag>();
        context.CfgProfiles.Add(legacy);
        await context.SaveChangesAsync();
        context.HistConfigSnapshots.Add(new HistConfigSnapshot
        {
            ProfileId = legacy.Id, VersionMajor = 1, VersionMinor = 0, SnapshotType = "PUBLISH",
            SnapshotJson = "{\"profileCode\":\"legacy\"}", CreatedAtUtc = new DateTime(2020, 1, 1), CreatedBy = "system-backfill"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await Seeder(context).SeedAsync();
        var unchanged = await context.CfgProfiles.AsNoTracking().SingleAsync(profile => profile.Id == legacy.Id);
        unchanged.StatusId.Should().Be(ach.StatusId);
        unchanged.VersionMajor.Should().Be(1);
        unchanged.VersionMinor.Should().Be(0);
        (await context.HistConfigSnapshots.Where(row => row.ProfileId == legacy.Id).ToArrayAsync())
            .Should().ContainSingle(row => row.SnapshotJson == "{\"profileCode\":\"legacy\"}");
    }

    [Fact]
    public void SeederOrder_FollowsOfficialAndNamingSeeders()
    {
        var order = typeof(NachaPublicationSnapshotCoverageSeeder).GetProperty(nameof(NachaPublicationSnapshotCoverageSeeder.Order));
        order.Should().NotBeNull();
        // Constructor-free order check uses the repository's explicit numeric seeder sequence.
        using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite("Data Source=:memory:").Options);
        new NachaConfigBackfillSeeder(context).Order.Should().Be(8);
        new NachaConfigOfficialProfilesSeeder(context).Order.Should().Be(9);
        new NachaFileNamingRuleSeeder(context).Order.Should().Be(10);
        Seeder(context).Order.Should().Be(11);
    }

    private static NachaPublicationSnapshotCoverageSeeder Seeder(AchDbContext context)
        => new(context);

    private static async Task<List<CfgProfile>> SelectionProfilesAsync(AchDbContext context)
        => await context.CfgProfiles.AsNoTracking().Include(profile => profile.Status)
            .Include(profile => profile.ClearingHouse).Include(profile => profile.FlowType)
            .Include(profile => profile.Direction).Include(profile => profile.ServiceClass)
            .Include(profile => profile.Tags).ToListAsync();

    private static async Task<HistConfigSnapshot> TargetSnapshotAsync(AchDbContext context)
    {
        var profile = await context.CfgProfiles.SingleAsync(item => item.ClearingHouse.Code == "ACH"
            && item.FlowType.Code == "ORIGINAL" && item.Direction.Code == "SALIDA" && item.VersionMajor == 35);
        return await context.HistConfigSnapshots.SingleAsync(row => row.ProfileId == profile.Id && row.SnapshotType == "PUBLISH");
    }

    private static async Task<HistConfigSnapshot[]> TargetSnapshotsAsync(AchDbContext context)
    {
        var profileId = await context.CfgProfiles.Where(item => item.ClearingHouse.Code == "ACH"
            && item.FlowType.Code == "ORIGINAL" && item.Direction.Code == "SALIDA" && item.VersionMajor == 35)
            .Select(item => item.Id).SingleAsync();
        return await context.HistConfigSnapshots.AsNoTracking().Where(row => row.ProfileId == profileId && row.SnapshotType == "PUBLISH")
            .OrderBy(row => row.Id).ToArrayAsync();
    }

    private static CfgProfile Clone(CfgProfile source, int id, int major, int minor, DateTime from, DateTime? to, int priority)
        => new()
        {
            Id = id, ProfileCode = $"TEST_{id}_{major}_{minor}", NameEs = source.NameEs,
            ClearingHouseId = source.ClearingHouseId, ClearingHouse = source.ClearingHouse,
            FlowTypeId = source.FlowTypeId, FlowType = source.FlowType,
            DirectionId = source.DirectionId, Direction = source.Direction,
            ServiceClassId = source.ServiceClassId, ServiceClass = source.ServiceClass,
            StatusId = source.StatusId, Status = source.Status,
            VersionMajor = major, VersionMinor = minor, EffectiveFrom = from, EffectiveTo = to,
            ContextPriority = priority, Tags = source.Tags.ToList()
        };
}
