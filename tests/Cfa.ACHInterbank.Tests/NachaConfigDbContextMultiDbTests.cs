using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Xunit.Abstractions;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaConfigDbContextMultiDbTests
{
    private readonly ITestOutputHelper _output;

    public NachaConfigDbContextMultiDbTests(ITestOutputHelper output) => _output = output;

    [Fact]
    [Trait("Category", "ConfigImmutabilityMultiDb")]
    [Trait("Provider", "SqlServer")]
    public Task OfficialInventoryIsStableOnSqlServer() => AuditInventoryAsync("SqlServer");

    [Fact]
    [Trait("Category", "ConfigImmutabilityMultiDb")]
    [Trait("Provider", "PostgreSql")]
    public Task OfficialInventoryIsStableOnPostgres() => AuditInventoryAsync("Postgres");

    private async Task AuditInventoryAsync(string provider)
    {
        await using var context = CreateProviderContext(provider, GetConnection(provider));
        var profiles = await context.CfgProfiles.AsNoTracking()
            .Where(row => row.ProfileCode.StartsWith("OFFICIAL_"))
            .Select(row => new { row.Id, row.ProfileCode })
            .ToListAsync();
        var snapshots = await context.HistConfigSnapshots.AsNoTracking()
            .Where(row => row.SnapshotType == "PUBLISH"
                && row.Profile.ProfileCode.StartsWith("OFFICIAL_"))
            .Select(row => new { row.Profile.ProfileCode, row.VersionMajor, row.VersionMinor, row.SnapshotJson })
            .ToListAsync();
        profiles.Count.Should().Be(37);
        snapshots.Count.Should().Be(profiles.Count);
        var content = string.Join("\n", snapshots
            .OrderBy(row => row.ProfileCode, StringComparer.Ordinal)
            .ThenBy(row => row.VersionMajor).ThenBy(row => row.VersionMinor)
            .Select(row => $"{row.ProfileCode}:{row.VersionMajor}.{row.VersionMinor}:{row.SnapshotJson.Length}:{row.SnapshotJson}"));
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        _output.WriteLine($"Provider={provider}; OfficialProfiles={profiles.Count}; PublishSnapshots={snapshots.Count}; PublishSHA256={digest}");
        var expected = Environment.GetEnvironmentVariable("NACHA_CONFIG_EXPECTED_PUBLISH_DIGEST");
        if (!string.IsNullOrWhiteSpace(expected))
            digest.Should().Be(expected);
    }

    [Fact]
    [Trait("Category", "ConfigImmutabilityMultiDb")]
    [Trait("Provider", "SqlServer")]
    public Task GuardMatrixRunsOnSqlServer() => RunAsync("SqlServer");

    [Fact]
    [Trait("Category", "ConfigImmutabilityMultiDb")]
    [Trait("Provider", "PostgreSql")]
    public Task GuardMatrixRunsOnPostgres() => RunAsync("Postgres");

    [Fact]
    [Trait("Category", "ConfigImmutabilityOutOfBand")]
    [Trait("Provider", "SqlServer")]
    public Task PublishedRuntimeIgnoresOutOfBandSqlServerLiveMutation()
        => RunOutOfBandIsolationAsync("SqlServer");

    [Fact]
    [Trait("Category", "ConfigImmutabilityOutOfBand")]
    [Trait("Provider", "PostgreSql")]
    public Task PublishedRuntimeIgnoresOutOfBandPostgresLiveMutation()
        => RunOutOfBandIsolationAsync("Postgres");

    private static async Task RunOutOfBandIsolationAsync(string provider)
    {
        var connection = GetConnection(provider);
        await using var context = CreateProviderContext(provider, connection);
        var profileId = await context.CfgProfiles.AsNoTracking()
            .Where(row => row.ProfileCode == "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_2")
            .Select(row => row.Id).SingleAsync();
        var field = await context.CfgLayoutFields.AsNoTracking()
            .Where(row => row.LayoutVariant.ProfileId == profileId
                && row.LayoutVariant.RecordCode.Code == "5"
                && row.LayoutVariant.IsDefaultForRecord
                && row.IsEnabled && row.StartPosition + row.Length < 106)
            .OrderBy(row => row.StartPosition)
            .FirstAsync();
        var publishedBefore = await NachaProfileRecordReader.LoadPublishedAsync(context, profileId, default);
        var record = new string(Enumerable.Range(0, publishedBefore.RecordLength)
            .Select(index => (char)('A' + index % 26)).ToArray());
        var selectedBefore = publishedBefore.Read(record, "5", field.FieldCode);
        var snapshotBefore = await context.HistConfigSnapshots.AsNoTracking()
            .Where(row => row.ProfileId == profileId && row.SnapshotType == "PUBLISH")
            .Select(row => row.SnapshotJson).SingleAsync();

        var entityType = context.Model.FindEntityType(typeof(CfgLayoutField))!;
        var tableName = entityType.GetTableName()!;
        var schema = entityType.GetSchema();
        var store = StoreObjectIdentifier.Table(tableName, schema);
        var sql = context.GetService<ISqlGenerationHelper>();
        var table = sql.DelimitIdentifier(tableName, schema);
        var length = sql.DelimitIdentifier(entityType.FindProperty(nameof(CfgLayoutField.Length))!.GetColumnName(store)!);
        var id = sql.DelimitIdentifier(entityType.FindProperty(nameof(CfgLayoutField.Id))!.GetColumnName(store)!);
        (await context.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
            $"UPDATE {table} SET {length} = {length} + 1 WHERE {id} = {{0}}", field.Id)))
            .Should().Be(1);
        context.ChangeTracker.Clear();

        var liveAfter = await NachaProfileRecordReader.LoadAsync(context, profileId, default);
        var publishedAfter = await NachaProfileRecordReader.LoadPublishedAsync(context, profileId, default);
        liveAfter.Read(record, "5", field.FieldCode).Should().NotBe(selectedBefore);
        publishedAfter.Read(record, "5", field.FieldCode).Should().Be(selectedBefore);
        (await context.HistConfigSnapshots.AsNoTracking()
            .Where(row => row.ProfileId == profileId && row.SnapshotType == "PUBLISH")
            .Select(row => row.SnapshotJson).SingleAsync()).Should().Be(snapshotBefore);
    }

    private static string GetConnection(string provider)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_NACHA_CONFIG_IMMUTABILITY_MULTIDB"),
                "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("RUN_NACHA_CONFIG_IMMUTABILITY_MULTIDB=true is required.");
        var connectionVariable = provider == "SqlServer"
            ? "NACHA_CONFIG_SQLSERVER_CONNECTION_STRING"
            : "NACHA_CONFIG_POSTGRES_CONNECTION_STRING";
        return Environment.GetEnvironmentVariable(connectionVariable)
            ?? throw new InvalidOperationException($"{connectionVariable} is required.");
    }

    private static AchDbContext CreateProviderContext(string provider, string connection)
    {
        var builder = new DbContextOptionsBuilder<AchDbContext>();
        if (provider == "SqlServer")
            builder.UseSqlServer(connection, options => options.EnableRetryOnFailure());
        else
            builder.UseNpgsql(connection);
        return new AchDbContext(builder.Options);
    }

    private static async Task RunAsync(string provider)
    {
        var connection = GetConnection(provider);
        AchDbContext CreateContext() => CreateProviderContext(provider, connection);

        var code = $"TEST_CONFIG_GUARD_{Guid.NewGuid():N}";
        var fixtureMajorVersion = 100000 + Random.Shared.Next(1000000000);
        int profileId;
        int tagId;
        int variantId;
        int fieldId;
        int ruleId;
        int sourceId;
        int ruleSetId;
        int ruleSetRuleId;
        int snapshotId;
        int draftId;
        int publishedId;
        int inactiveId;
        int recordCodeId;
        int ruleTypeId;
        int sourceTypeId;
        int clearingHouseId;
        int flowId;
        int directionId;
        int serviceId;

        await using (var context = CreateContext())
        {
            var officialCount = await context.CfgProfiles.CountAsync(row => row.ProfileCode.StartsWith("OFFICIAL_")
                && context.HistConfigSnapshots.Any(snapshot => snapshot.ProfileId == row.Id
                    && snapshot.SnapshotType == "PUBLISH"));
            officialCount.Should().Be(37);
            (await context.HistConfigSnapshots.CountAsync(row => row.SnapshotType == "PUBLISH"
                && row.Profile.ProfileCode.StartsWith("OFFICIAL_")))
                .Should().Be(officialCount);
            clearingHouseId = await context.CatClearingHouses.Where(row => row.Code == "ACH").Select(row => row.Id).SingleAsync();
            flowId = await context.CatFlowTypes.Where(row => row.Code == "ORIGINAL").Select(row => row.Id).SingleAsync();
            directionId = await context.CatDirections.Where(row => row.Code == "SALIDA").Select(row => row.Id).SingleAsync();
            serviceId = await context.CatServiceClasses.Where(row => row.Code == "PPD").Select(row => row.Id).SingleAsync();
            draftId = await context.CatConfigStatuses.Where(row => row.Code == "BORRADOR").Select(row => row.Id).SingleAsync();
            publishedId = await context.CatConfigStatuses.Where(row => row.Code == "PUBLICADO").Select(row => row.Id).SingleAsync();
            inactiveId = await context.CatConfigStatuses.Where(row => row.Code == "INACTIVO").Select(row => row.Id).SingleAsync();
            recordCodeId = await context.CatRecordCodes.Where(row => row.Code == "5").Select(row => row.Id).SingleAsync();
            ruleTypeId = await context.CatRuleTypes.Select(row => row.Id).FirstAsync();
            sourceTypeId = await context.CatDataSourceTypes.Where(row => row.Code == "CONSTANTE").Select(row => row.Id).SingleAsync();

            var profile = new CfgProfile
            {
                ProfileCode = code, NameEs = "Guard provider fixture", ClearingHouseId = clearingHouseId,
                FlowTypeId = flowId, DirectionId = directionId, ServiceClassId = serviceId,
                StatusId = draftId, EffectiveFrom = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                VersionMajor = fixtureMajorVersion
            };
            var source = new CfgFieldSourceDefinition { DataSourceTypeId = sourceTypeId, ConstantValue = "1" };
            var ruleSet = new CfgRuleSet { RuleSetCode = code, NameEs = "Guard rules", Scope = "RECORD" };
            context.CfgProfiles.Add(profile);
            context.CfgFieldSourceDefinitions.Add(source);
            context.CfgRuleSets.Add(ruleSet);
            await context.SaveChangesAsync();
            profileId = profile.Id;
            sourceId = source.Id;
            ruleSetId = ruleSet.Id;

            var tag = new CfgProfileTag { ProfileId = profileId, TagKey = "Test", TagValue = "draft" };
            var variant = new CfgLayoutVariant
            {
                ProfileId = profileId, RecordCodeId = recordCodeId, VariantCode = "T5",
                NameEs = "Guard variant", StatusId = publishedId, EffectiveFrom = profile.EffectiveFrom,
                TotalLength = 106
            };
            var ruleSetRule = new CfgRuleSetRule
            {
                RuleSetId = ruleSetId, RuleTypeId = ruleTypeId, RuleCode = "TEST",
                ErrorCode = "TEST", ErrorMessageEs = "Test"
            };
            context.CfgProfileTags.Add(tag);
            context.CfgLayoutVariants.Add(variant);
            context.CfgProfileRecords.Add(new CfgProfileRecord
            {
                ProfileId = profileId, RecordCodeId = recordCodeId, Sequence = 1,
                SemanticRuleSetId = ruleSetId
            });
            context.CfgRuleSetRules.Add(ruleSetRule);
            await context.SaveChangesAsync();
            tagId = tag.Id;
            variantId = variant.Id;
            ruleSetRuleId = ruleSetRule.Id;

            var field = new CfgLayoutField
            {
                LayoutVariantId = variantId, SourceDefinitionId = sourceId,
                FieldCode = "F1", FieldNameEs = "Guard field", StartPosition = 1, Length = 1
            };
            context.CfgLayoutFields.Add(field);
            await context.SaveChangesAsync();
            fieldId = field.Id;
            var rule = new CfgFieldRule
            {
                LayoutFieldId = fieldId, RuleTypeId = ruleTypeId, RuleCode = "TEST",
                ErrorCode = "TEST", ErrorMessageEs = "Test"
            };
            context.CfgFieldRules.Add(rule);
            await context.SaveChangesAsync();
            ruleId = rule.Id;

            tag.TagValue = "draft-edited";
            source.ConstantValue = "draft-edited";
            ruleSet.NameEs = "Draft rules edited";
            await context.SaveChangesAsync();

            profile.StatusId = publishedId;
            profile.PublishedAt = DateTime.UtcNow;
            profile.PublishedBy = "provider-test";
            var snapshot = new HistConfigSnapshot
            {
                ProfileId = profileId, VersionMajor = fixtureMajorVersion, VersionMinor = 0,
                SnapshotType = "PUBLISH", SnapshotJson = "{}",
                CreatedAtUtc = DateTime.UtcNow, CreatedBy = "provider-test"
            };
            context.HistConfigSnapshots.Add(snapshot);
            await context.SaveChangesAsync();
            snapshotId = snapshot.Id;
        }

        async Task RejectAsync(Action<AchDbContext> change, string expectedCode, bool sync = false)
        {
            await using var context = CreateContext();
            change(context);
            if (sync)
            {
                Action save = () => { context.SaveChanges(); };
                Assert.Throws<NachaConfigException>(save).ErrorCode.Should().Be(expectedCode);
            }
            else
            {
                var error = await Assert.ThrowsAsync<NachaConfigException>(() => context.SaveChangesAsync());
                error.ErrorCode.Should().Be(expectedCode);
            }
        }

        await RejectAsync(context =>
        {
            var profile = context.CfgProfiles.Single(row => row.Id == profileId);
            profile.ContextPriority++;
        }, "CONFIG_PUBLISHED_PROFILE_IMMUTABLE");
        await RejectAsync(context =>
        {
            var tag = context.CfgProfileTags.Single(row => row.Id == tagId);
            tag.TagValue = "forbidden";
        }, "CONFIG_PUBLISHED_CHILD_IMMUTABLE");
        await RejectAsync(context =>
        {
            context.Attach(new CfgProfileTag { Id = tagId, ProfileId = profileId, TagKey = "Test", TagValue = "detached" })
                .State = EntityState.Modified;
        }, "CONFIG_PUBLISHED_CHILD_IMMUTABLE", sync: true);
        await RejectAsync(context =>
        {
            context.Attach(new CfgLayoutField { Id = fieldId, LayoutVariantId = variantId, FieldNameEs = "detached" })
                .State = EntityState.Modified;
        }, "CONFIG_PUBLISHED_CHILD_IMMUTABLE");
        await RejectAsync(context =>
        {
            context.HistConfigSnapshots.Single(row => row.Id == snapshotId).SnapshotJson = "changed";
        }, "CONFIG_SNAPSHOT_IMMUTABLE");
        await RejectAsync(context =>
        {
            context.HistConfigSnapshots.Remove(context.HistConfigSnapshots.Single(row => row.Id == snapshotId));
        }, "CONFIG_SNAPSHOT_IMMUTABLE");
        await RejectAsync(context =>
        {
            context.CfgFieldSourceDefinitions.Single(row => row.Id == sourceId).ConstantValue = "forbidden";
        }, "CONFIG_PUBLISHED_SOURCE_IMMUTABLE");
        await RejectAsync(context =>
        {
            context.CfgRuleSets.Single(row => row.Id == ruleSetId).NameEs = "forbidden";
        }, "CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");
        await RejectAsync(context =>
        {
            context.CfgRuleSetRules.Add(new CfgRuleSetRule
            {
                RuleSetId = ruleSetId, RuleTypeId = ruleTypeId,
                RuleCode = "FORBIDDEN", ErrorCode = "FORBIDDEN", ErrorMessageEs = "Forbidden"
            });
        }, "CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");

        await using (var context = CreateContext())
        {
            (await context.CfgProfiles.AsNoTracking().SingleAsync(row => row.Id == profileId))
                .ContextPriority.Should().Be(100);
            (await context.CfgProfileTags.AsNoTracking().SingleAsync(row => row.Id == tagId))
                .TagValue.Should().Be("draft-edited");
            (await context.CfgLayoutFields.AsNoTracking().SingleAsync(row => row.Id == fieldId))
                .FieldNameEs.Should().Be("Guard field");
            (await context.CfgFieldSourceDefinitions.AsNoTracking().SingleAsync(row => row.Id == sourceId))
                .ConstantValue.Should().Be("draft-edited");
            (await context.CfgRuleSets.AsNoTracking().SingleAsync(row => row.Id == ruleSetId))
                .NameEs.Should().Be("Draft rules edited");
            (await context.HistConfigSnapshots.AsNoTracking().SingleAsync(row => row.Id == snapshotId))
                .SnapshotJson.Should().Be("{}");
            (await context.CfgFieldRules.AsNoTracking().SingleAsync(row => row.Id == ruleId))
                .ErrorCode.Should().Be("TEST");
            (await context.CfgRuleSetRules.CountAsync(row => row.RuleSetId == ruleSetId)).Should().Be(1);

            var profile = await context.CfgProfiles.SingleAsync(row => row.Id == profileId);
            profile.StatusId = inactiveId;
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var tag = await context.CfgProfileTags.SingleAsync(row => row.Id == tagId);
            tag.TagValue = "after-inactivation";
            var error = await Assert.ThrowsAsync<NachaConfigException>(() => context.SaveChangesAsync());
            error.ErrorCode.Should().Be("CONFIG_PUBLISHED_CHILD_IMMUTABLE");
        }

        await using (var context = CreateContext())
        {
            var draftSource = new CfgFieldSourceDefinition { DataSourceTypeId = sourceTypeId, ConstantValue = "draft" };
            var draftRuleSet = new CfgRuleSet { RuleSetCode = code + "_DRAFT", NameEs = "Draft", Scope = "RECORD" };
            context.CfgFieldSourceDefinitions.Add(draftSource);
            context.CfgRuleSets.Add(draftRuleSet);
            await context.SaveChangesAsync();
            draftSource.ConstantValue = "draft-updated";
            draftRuleSet.NameEs = "Draft updated";
            await context.SaveChangesAsync();

            var successor = new CfgProfile
            {
                ProfileCode = code + "_NEXT", NameEs = "Successor", ClearingHouseId = clearingHouseId,
                FlowTypeId = flowId, DirectionId = directionId, ServiceClassId = serviceId,
                StatusId = draftId, EffectiveFrom = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                VersionMajor = fixtureMajorVersion + 1, SupersedesProfileId = profileId
            };
            context.CfgProfiles.Add(successor);
            await context.SaveChangesAsync();
            successor.NameEs = "Successor edited";
            context.CfgProfileTags.Add(new CfgProfileTag { ProfileId = successor.Id, TagKey = "Test", TagValue = "successor" });
            await context.SaveChangesAsync();
            successor.StatusId = publishedId;
            successor.PublishedAt = DateTime.UtcNow;
            successor.PublishedBy = "provider-test";
            context.HistConfigSnapshots.Add(new HistConfigSnapshot
            {
                ProfileId = successor.Id, VersionMajor = fixtureMajorVersion + 1, VersionMinor = 0,
                SnapshotType = "PUBLISH", SnapshotJson = "{}",
                CreatedAtUtc = DateTime.UtcNow, CreatedBy = "provider-test"
            });
            await context.SaveChangesAsync();
            (await context.CfgProfiles.AsNoTracking().SingleAsync(row => row.Id == profileId))
                .NameEs.Should().Be("Guard provider fixture");

            var history = new HistConfigChange
            {
                ProfileId = successor.Id, EntityName = "Test", EntityId = "1",
                ChangeType = "TEST", ChangedAtUtc = DateTime.UtcNow, ChangedBy = "provider-test"
            };
            context.HistConfigChanges.Add(history);
            await context.SaveChangesAsync();
            history.ChangeType = "forbidden";
            var error = await Assert.ThrowsAsync<NachaConfigException>(() => context.SaveChangesAsync());
            error.ErrorCode.Should().Be("CONFIG_HISTORY_IMMUTABLE");
            context.ChangeTracker.Clear();
            context.HistConfigChanges.Remove(await context.HistConfigChanges.SingleAsync(row => row.Id == history.Id));
            error = await Assert.ThrowsAsync<NachaConfigException>(() => context.SaveChangesAsync());
            error.ErrorCode.Should().Be("CONFIG_HISTORY_IMMUTABLE");
        }
    }
}
