using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaConfigDbContextImmutabilityTests
{
    [Theory]
    [InlineData("ProfileCode")]
    [InlineData("NameEs")]
    [InlineData("Description")]
    [InlineData("ClearingHouseId")]
    [InlineData("FlowTypeId")]
    [InlineData("DirectionId")]
    [InlineData("ServiceClassId")]
    [InlineData("ContextPriority")]
    [InlineData("EffectiveFrom")]
    [InlineData("VersionMajor")]
    [InlineData("VersionMinor")]
    [InlineData("PublishedAt")]
    [InlineData("PublishedBy")]
    [InlineData("SupersedesProfileId")]
    public async Task PublishedProfileGenerationAndProvenanceCannotChange(string property)
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        var entry = context.Entry(profile).Property(property);
        entry.CurrentValue = entry.CurrentValue switch
        {
            string value => value + "_changed",
            int value => value + 1,
            DateTime value => value.AddDays(1),
            _ => 987654
        };
        entry.IsModified = true;

        await RejectAsync(context, "CONFIG_PUBLISHED_PROFILE_IMMUTABLE");
    }

    [Fact]
    public async Task PublishedProfileAuditTimestampCannotChangeWithoutLifecycleTransition()
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        profile.UpdatedAt = profile.UpdatedAt.AddMinutes(1);
        await RejectAsync(context, "CONFIG_PUBLISHED_PROFILE_IMMUTABLE");
    }

    [Fact]
    public async Task LifecycleOnlyIsAllowedButCannotCarryGenerationEditOrReopenGraph()
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        var command = new NachaConfigProfileCommandService(context, new NachaConfigProfileQueryService(context));
        (await command.InactivateProfileAsync(profile.Id, "guard-test", Convert.ToBase64String(profile.RowVersion)))
            .Should().BeTrue();

        context.ChangeTracker.Clear();
        var inactive = await context.CfgProfiles.SingleAsync(row => row.Id == profile.Id);
        (await context.CatConfigStatuses.Where(row => row.Id == inactive.StatusId)
            .Select(row => row.Code).SingleAsync()).Should().Be("INACTIVO");
        (await command.InactivateProfileAsync(profile.Id, "guard-test", Convert.ToBase64String(inactive.RowVersion)))
            .Should().BeTrue();
        context.ChangeTracker.Clear();
        inactive = await context.CfgProfiles.SingleAsync(row => row.Id == profile.Id);
        inactive.ContextPriority++;
        await RejectAsync(context, "CONFIG_PUBLISHED_PROFILE_IMMUTABLE");

        context.ChangeTracker.Clear();
        var tag = await context.CfgProfileTags.FirstAsync(row => row.ProfileId == profile.Id);
        tag.TagValue += "_changed";
        await RejectAsync(context, "CONFIG_PUBLISHED_CHILD_IMMUTABLE");
    }

    [Fact]
    public async Task LifecycleTransitionCannotHideGenerationEdit()
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        var originalPriority = profile.ContextPriority;
        profile.StatusId = await context.CatConfigStatuses.Where(row => row.Code == "ARCHIVADO")
            .Select(row => row.Id).SingleAsync();
        profile.ContextPriority++;
        await RejectAsync(context, "CONFIG_PUBLISHED_PROFILE_IMMUTABLE");
        context.ChangeTracker.Clear();
        (await context.CfgProfiles.AsNoTracking().SingleAsync(row => row.Id == profile.Id))
            .ContextPriority.Should().Be(originalPriority);
    }

    [Theory]
    [InlineData("tag-update")]
    [InlineData("tag-delete")]
    [InlineData("tag-add")]
    [InlineData("record-update")]
    [InlineData("record-add")]
    [InlineData("variant-update")]
    [InlineData("variant-delete")]
    [InlineData("field-update")]
    [InlineData("field-delete")]
    [InlineData("field-add")]
    [InlineData("rule-update")]
    [InlineData("rule-delete")]
    [InlineData("rule-add")]
    public async Task PublishedOwnedGraphCannotChange(string action)
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        var tag = await context.CfgProfileTags.FirstAsync(row => row.ProfileId == profile.Id);
        var record = await context.CfgProfileRecords.FirstAsync(row => row.ProfileId == profile.Id);
        var variant = await context.CfgLayoutVariants.FirstAsync(row => row.ProfileId == profile.Id);
        var field = await context.CfgLayoutFields.FirstAsync(row => row.LayoutVariant.ProfileId == profile.Id);
        var rule = await context.CfgFieldRules.FirstAsync(row => row.LayoutField.LayoutVariant.ProfileId == profile.Id);
        switch (action)
        {
            case "tag-update": tag.TagValue += "_changed"; break;
            case "tag-delete": context.CfgProfileTags.Remove(tag); break;
            case "tag-add": context.CfgProfileTags.Add(new CfgProfileTag { ProfileId = profile.Id, TagKey = "Extra", TagValue = "x" }); break;
            case "record-update": record.Sequence++; break;
            case "record-add": context.CfgProfileRecords.Add(new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = record.RecordCodeId, Sequence = 999 }); break;
            case "variant-update": variant.Priority++; break;
            case "variant-delete": context.CfgLayoutVariants.Remove(variant); break;
            case "field-update": field.StartPosition++; break;
            case "field-delete": context.CfgLayoutFields.Remove(field); break;
            case "field-add": context.CfgLayoutFields.Add(new CfgLayoutField { LayoutVariantId = variant.Id, SourceDefinitionId = field.SourceDefinitionId, FieldCode = "Extra", FieldNameEs = "Extra", Length = 1 }); break;
            case "rule-update": rule.ErrorCode += "_changed"; break;
            case "rule-delete": context.CfgFieldRules.Remove(rule); break;
            case "rule-add": context.CfgFieldRules.Add(new CfgFieldRule { LayoutFieldId = field.Id, RuleTypeId = rule.RuleTypeId, RuleCode = "Extra", ErrorCode = "Extra", ErrorMessageEs = "Extra" }); break;
        }
        await RejectAsync(context, "CONFIG_PUBLISHED_CHILD_IMMUTABLE");
    }

    [Fact]
    public async Task SharedDefinitionsAndRulesReferencedByPublishedProfileCannotChange()
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        var sourceId = await context.CfgLayoutFields
            .Where(field => field.LayoutVariant.ProfileId == profile.Id)
            .Select(field => field.SourceDefinitionId).FirstAsync();
        var source = await context.CfgFieldSourceDefinitions.SingleAsync(row => row.Id == sourceId);
        source.ConstantValue = "changed";
        await RejectAsync(context, "CONFIG_PUBLISHED_SOURCE_IMMUTABLE");

        context.ChangeTracker.Clear();
        var ruleSetId = await context.CfgProfileRecords
            .Where(record => record.ProfileId == profile.Id && record.SemanticRuleSetId != null)
            .Select(record => record.SemanticRuleSetId!.Value).FirstAsync();
        var ruleSet = await context.CfgRuleSets.SingleAsync(row => row.Id == ruleSetId);
        ruleSet.NameEs += " changed";
        await RejectAsync(context, "CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");

        context.ChangeTracker.Clear();
        var rule = await context.CfgRuleSetRules.FirstAsync(row => row.RuleSetId == ruleSetId);
        rule.ErrorCode += " changed";
        await RejectAsync(context, "CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");

        context.ChangeTracker.Clear();
        context.CfgRuleSetRules.Add(new CfgRuleSetRule { RuleSetId = ruleSetId, RuleTypeId = rule.RuleTypeId, RuleCode = "Extra", ErrorCode = "Extra", ErrorMessageEs = "Extra" });
        await RejectAsync(context, "CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");

        context.ChangeTracker.Clear();
        source = await context.CfgFieldSourceDefinitions.SingleAsync(row => row.Id == sourceId);
        context.CfgFieldSourceDefinitions.Remove(source);
        await RejectAsync(context, "CONFIG_PUBLISHED_SOURCE_IMMUTABLE");

        context.ChangeTracker.Clear();
        ruleSet = await context.CfgRuleSets.SingleAsync(row => row.Id == ruleSetId);
        context.CfgRuleSets.Remove(ruleSet);
        await RejectAsync(context, "CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");
    }

    [Fact]
    public async Task HistoryIsAppendOnly()
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        var snapshot = await context.HistConfigSnapshots.FirstAsync(row => row.ProfileId == profile.Id && row.SnapshotType == "PUBLISH");
        snapshot.SnapshotJson = "{";
        await RejectAsync(context, "CONFIG_SNAPSHOT_IMMUTABLE");
        context.ChangeTracker.Clear();

        snapshot = await context.HistConfigSnapshots.FirstAsync(row => row.ProfileId == profile.Id && row.SnapshotType == "PUBLISH");
        context.HistConfigSnapshots.Remove(snapshot);
        await RejectAsync(context, "CONFIG_SNAPSHOT_IMMUTABLE");
        context.ChangeTracker.Clear();

        context.HistConfigChanges.Add(new HistConfigChange { ProfileId = profile.Id, EntityName = "Test", EntityId = "1", ChangeType = "TEST", ChangedBy = "test", ChangedAtUtc = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var history = await context.HistConfigChanges.OrderByDescending(row => row.Id).FirstAsync();
        history.ChangeType = "changed";
        await RejectAsync(context, "CONFIG_HISTORY_IMMUTABLE");
        context.ChangeTracker.Clear();
        history = await context.HistConfigChanges.OrderByDescending(row => row.Id).FirstAsync();
        context.HistConfigChanges.Remove(history);
        await RejectAsync(context, "CONFIG_HISTORY_IMMUTABLE");
    }

    [Fact]
    public async Task PublishedIdentityCannotReceiveArbitrarySecondPublishArtifact()
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        context.HistConfigSnapshots.Add(new HistConfigSnapshot
        {
            ProfileId = profile.Id, VersionMajor = profile.VersionMajor,
            VersionMinor = profile.VersionMinor, SnapshotType = "PUBLISH",
            SnapshotJson = "{}", CreatedAtUtc = DateTime.UtcNow, CreatedBy = "test"
        });
        await RejectAsync(context, "CONFIG_SNAPSHOT_IMMUTABLE");
    }

    [Fact]
    public async Task DetachedMutationsAndAllSaveOverloadsFail()
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        var tag = await context.CfgProfileTags.AsNoTracking().FirstAsync(row => row.ProfileId == profile.Id);
        context.Attach(new CfgProfileTag { Id = tag.Id, ProfileId = profile.Id, TagKey = tag.TagKey, TagValue = "detached" }).State = EntityState.Modified;
        Action sync = () => { context.SaveChanges(); };
        Assert.Throws<NachaConfigException>(sync).ErrorCode.Should().Be("CONFIG_PUBLISHED_CHILD_IMMUTABLE");
        context.ChangeTracker.Clear();

        var field = await context.CfgLayoutFields.AsNoTracking().FirstAsync(row => row.LayoutVariant.ProfileId == profile.Id);
        context.Attach(new CfgLayoutField { Id = field.Id, LayoutVariantId = field.LayoutVariantId, FieldCode = field.FieldCode, FieldNameEs = "detached", SourceDefinitionId = field.SourceDefinitionId }).State = EntityState.Modified;
        Action syncBool = () => { context.SaveChanges(false); };
        Assert.Throws<NachaConfigException>(syncBool).ErrorCode.Should().Be("CONFIG_PUBLISHED_CHILD_IMMUTABLE");
        context.ChangeTracker.Clear();

        var ruleSetId = await context.CfgProfileRecords.Where(row => row.ProfileId == profile.Id && row.SemanticRuleSetId != null)
            .Select(row => row.SemanticRuleSetId!.Value).FirstAsync();
        context.Attach(new CfgRuleSet { Id = ruleSetId, RuleSetCode = "detached", NameEs = "detached" }).State = EntityState.Modified;
        await RejectAsync(context, "CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");
        context.ChangeTracker.Clear();

        var sourceId = await context.CfgLayoutFields.Where(row => row.LayoutVariant.ProfileId == profile.Id)
            .Select(row => row.SourceDefinitionId).FirstAsync();
        context.Attach(new CfgFieldSourceDefinition { Id = sourceId, ConstantValue = "detached" }).State = EntityState.Modified;
        var asyncBool = await Assert.ThrowsAsync<NachaConfigException>(() => context.SaveChangesAsync(false));
        asyncBool.ErrorCode.Should().Be("CONFIG_PUBLISHED_SOURCE_IMMUTABLE");
        context.ChangeTracker.Clear();

        (await context.CfgProfileTags.AsNoTracking().SingleAsync(row => row.Id == tag.Id)).TagValue.Should().Be(tag.TagValue);
    }

    [Fact]
    public async Task SuccessorDraftCanChangeAndPublishWithoutChangingPredecessor()
    {
        await using var context = await CreateSeededContextAsync();
        var predecessor = await PublishedProfile(context);
        var originalPriority = predecessor.ContextPriority;
        var command = new NachaConfigProfileCommandService(context, new NachaConfigProfileQueryService(context));
        var clone = await command.CloneProfileAsync(predecessor.Id, new NachaConfigCloneProfileRequest
        {
            NuevoProfileCode = "TEST_SUCCESSOR",
            NuevoNombreEs = "Successor",
            EffectiveFrom = predecessor.EffectiveFrom.AddDays(1),
            ExpectedRowVersion = Convert.ToBase64String(predecessor.RowVersion)
        }, "test");
        clone.Should().NotBeNull();

        context.ChangeTracker.Clear();
        var successor = await context.CfgProfiles.SingleAsync(profile => profile.ProfileCode == "TEST_SUCCESSOR");
        successor.SupersedesProfileId.Should().Be(predecessor.Id);
        var tag = await context.CfgProfileTags.FirstAsync(row => row.ProfileId == successor.Id);
        tag.TagValue = "edited";
        var field = await context.CfgLayoutFields.Include(row => row.SourceDefinition)
            .FirstAsync(row => row.LayoutVariant.ProfileId == successor.Id);
        field.FieldNameEs = "Edited";
        field.SourceDefinition.ConstantValue = "2";
        context.CfgProfileTags.Add(new CfgProfileTag { ProfileId = successor.Id, TagKey = "Extra", TagValue = "x" });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var addedTag = await context.CfgProfileTags.SingleAsync(row => row.ProfileId == successor.Id && row.TagKey == "Extra");
        context.CfgProfileTags.Remove(addedTag);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        successor = await context.CfgProfiles.SingleAsync(profile => profile.Id == successor.Id);
        successor.StatusId = await context.CatConfigStatuses.Where(status => status.Code == "PUBLICADO")
            .Select(status => status.Id).SingleAsync();
        successor.PublishedAt = DateTime.UtcNow;
        successor.PublishedBy = "test";
        successor.VersionMinor++;
        context.HistConfigSnapshots.Add(new HistConfigSnapshot
        {
            ProfileId = successor.Id, VersionMajor = successor.VersionMajor,
            VersionMinor = successor.VersionMinor, SnapshotType = "PUBLISH",
            SnapshotJson = "{}", CreatedAtUtc = DateTime.UtcNow, CreatedBy = "test"
        });
        await context.SaveChangesAsync();

        (await context.CfgProfiles.AsNoTracking().SingleAsync(profile => profile.Id == predecessor.Id))
            .ContextPriority.Should().Be(originalPriority);
        (await context.HistConfigSnapshots.CountAsync(row => row.ProfileId == successor.Id && row.SnapshotType == "PUBLISH"))
            .Should().Be(1);
        context.ChangeTracker.Clear();
        var successorTag = await context.CfgProfileTags.FirstAsync(row => row.ProfileId == successor.Id);
        successorTag.TagValue = "too late";
        await RejectAsync(context, "CONFIG_PUBLISHED_CHILD_IMMUTABLE");
    }

    [Fact]
    public async Task DraftOnlyRuleSetAndSourceCanChange()
    {
        await using var context = await CreateSeededContextAsync();
        var sourceTypeId = await context.CatDataSourceTypes.Select(row => row.Id).FirstAsync();
        var source = new CfgFieldSourceDefinition { DataSourceTypeId = sourceTypeId, ConstantValue = "draft" };
        var ruleSet = new CfgRuleSet { RuleSetCode = "DRAFT_ONLY", NameEs = "Draft", Scope = "RECORD" };
        context.CfgFieldSourceDefinitions.Add(source);
        context.CfgRuleSets.Add(ruleSet);
        await context.SaveChangesAsync();
        source.ConstantValue = "draft-edited";
        ruleSet.NameEs = "Draft edited";
        await context.SaveChangesAsync();
        (await context.CfgFieldSourceDefinitions.AsNoTracking().SingleAsync(row => row.Id == source.Id))
            .ConstantValue.Should().Be("draft-edited");
        (await context.CfgRuleSets.AsNoTracking().SingleAsync(row => row.Id == ruleSet.Id))
            .NameEs.Should().Be("Draft edited");
    }

    [Fact]
    public async Task DisposableFixtureCanModelOutOfBandLiveCorruption()
    {
        await using var context = await CreateSeededContextAsync();
        var profile = await PublishedProfile(context);
        var snapshotJson = await context.HistConfigSnapshots.Where(row => row.ProfileId == profile.Id)
            .Select(row => row.SnapshotJson).SingleAsync();
        var field = await context.CfgLayoutFields.FirstAsync(row => row.LayoutVariant.ProfileId == profile.Id);
        var originalLength = field.Length;
        field.Length++;
        profile.Description = null;
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        (await context.CfgLayoutFields.AsNoTracking().SingleAsync(row => row.Id == field.Id))
            .Length.Should().Be(originalLength + 1);
        (await context.HistConfigSnapshots.AsNoTracking().SingleAsync(row => row.ProfileId == profile.Id))
            .SnapshotJson.Should().Be(snapshotJson);
        (await context.CfgProfiles.AsNoTracking().SingleAsync(row => row.Id == profile.Id))
            .Description.Should().BeNull();
    }

    private static Task<CfgProfile> PublishedProfile(AchDbContext context)
        => context.CfgProfiles.FirstAsync(profile => context.HistConfigSnapshots
            .Any(snapshot => snapshot.ProfileId == profile.Id && snapshot.SnapshotType == "PUBLISH")
            && context.CfgProfileRecords.Any(record => record.ProfileId == profile.Id && record.SemanticRuleSetId != null));

    private static async Task RejectAsync(AchDbContext context, string code)
    {
        var error = await Assert.ThrowsAsync<NachaConfigException>(() => context.SaveChangesAsync());
        error.ErrorCode.Should().Be(code);
    }

    private static async Task<AchDbContext> CreateSeededContextAsync()
    {
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite("Data Source=:memory:;Foreign Keys=False").Options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        if (!await context.CatClearingHouses.AnyAsync(row => row.Code == "ACH"))
            context.CatClearingHouses.Add(new CatClearingHouse { Code = "ACH", Name = "ACH", IsActive = true });
        if (!await context.CatFlowTypes.AnyAsync(row => row.Code == "ORIGINAL"))
            context.CatFlowTypes.Add(new CatFlowType { Code = "ORIGINAL", NameEs = "Original", IsActive = true });
        if (!await context.CatDirections.AnyAsync(row => row.Code == "SALIDA"))
            context.CatDirections.Add(new CatDirection { Code = "SALIDA", NameEs = "Salida", IsActive = true });
        if (!await context.CatServiceClasses.AnyAsync(row => row.Code == "PPD"))
            context.CatServiceClasses.Add(new CatServiceClass { Code = "PPD", NameEs = "PPD", IsActive = true });
        if (!await context.CatConfigStatuses.AnyAsync())
            context.CatConfigStatuses.AddRange(
                new CatConfigStatus { Code = "BORRADOR", IsEditable = true, IsPublishable = true },
                new CatConfigStatus { Code = "PUBLICADO" },
                new CatConfigStatus { Code = "INACTIVO" },
                new CatConfigStatus { Code = "ARCHIVADO" });
        if (!await context.CatRecordCodes.AnyAsync(row => row.Code == "5"))
            context.CatRecordCodes.Add(new CatRecordCode { Code = "5", NameEs = "Batch", IsMandatoryBase = true });
        if (!await context.CatDataSourceTypes.AnyAsync(row => row.Code == "CONSTANTE"))
            context.CatDataSourceTypes.Add(new CatDataSourceType { Code = "CONSTANTE", NameEs = "Constante" });
        if (!await context.CatRuleTypes.AnyAsync(row => row.Code == "REQUIRED"))
            context.CatRuleTypes.Add(new CatRuleType { Code = "REQUIRED", NameEs = "Required" });
        await context.SaveChangesAsync();

        var achId = await context.CatClearingHouses.Where(row => row.Code == "ACH").Select(row => row.Id).SingleAsync();
        var flowId = await context.CatFlowTypes.Where(row => row.Code == "ORIGINAL").Select(row => row.Id).SingleAsync();
        var directionId = await context.CatDirections.Where(row => row.Code == "SALIDA").Select(row => row.Id).SingleAsync();
        var serviceId = await context.CatServiceClasses.Where(row => row.Code == "PPD").Select(row => row.Id).SingleAsync();
        var draftId = await context.CatConfigStatuses.Where(row => row.Code == "BORRADOR").Select(row => row.Id).SingleAsync();
        var publishedId = await context.CatConfigStatuses.Where(row => row.Code == "PUBLICADO").Select(row => row.Id).SingleAsync();
        var recordCodeId = await context.CatRecordCodes.Where(row => row.Code == "5").Select(row => row.Id).SingleAsync();
        var sourceTypeId = await context.CatDataSourceTypes.Where(row => row.Code == "CONSTANTE").Select(row => row.Id).SingleAsync();
        var ruleTypeId = await context.CatRuleTypes.Where(row => row.Code == "REQUIRED").Select(row => row.Id).SingleAsync();

        var profile = new CfgProfile
        {
            ProfileCode = "TEST_PROFILE", NameEs = "Test", Description = "Test", ClearingHouseId = achId,
            FlowTypeId = flowId, DirectionId = directionId, ServiceClassId = serviceId, StatusId = draftId,
            EffectiveFrom = new DateTime(2026, 1, 1), VersionMajor = 1
        };
        context.CfgProfiles.Add(profile);
        var source = new CfgFieldSourceDefinition { DataSourceTypeId = sourceTypeId, ConstantValue = "1" };
        context.CfgFieldSourceDefinitions.Add(source);
        var ruleSet = new CfgRuleSet { RuleSetCode = "TEST_RULE_SET", NameEs = "Test", Scope = "RECORD" };
        context.CfgRuleSets.Add(ruleSet);
        await context.SaveChangesAsync();

        context.CfgProfileTags.Add(new CfgProfileTag { ProfileId = profile.Id, TagKey = "Test", TagValue = "1" });
        context.CfgProfileRecords.Add(new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = recordCodeId, Sequence = 1, SemanticRuleSetId = ruleSet.Id });
        var variant = new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = recordCodeId, VariantCode = "T5", NameEs = "Test", StatusId = publishedId, EffectiveFrom = profile.EffectiveFrom, TotalLength = 106 };
        context.CfgLayoutVariants.Add(variant);
        context.CfgRuleSetRules.Add(new CfgRuleSetRule { RuleSetId = ruleSet.Id, RuleTypeId = ruleTypeId, RuleCode = "TEST", ErrorCode = "TEST", ErrorMessageEs = "Test" });
        await context.SaveChangesAsync();

        var field = new CfgLayoutField { LayoutVariantId = variant.Id, SourceDefinitionId = source.Id, FieldCode = "F1", FieldNameEs = "Test", StartPosition = 1, Length = 1 };
        context.CfgLayoutFields.Add(field);
        await context.SaveChangesAsync();
        context.CfgFieldRules.Add(new CfgFieldRule { LayoutFieldId = field.Id, RuleTypeId = ruleTypeId, RuleCode = "TEST", ErrorCode = "TEST", ErrorMessageEs = "Test" });
        await context.SaveChangesAsync();

        profile.StatusId = publishedId;
        profile.PublishedAt = DateTime.UtcNow;
        profile.PublishedBy = "test";
        context.HistConfigSnapshots.Add(new HistConfigSnapshot
        {
            ProfileId = profile.Id, VersionMajor = 1, VersionMinor = 0,
            SnapshotType = "PUBLISH", SnapshotJson = "{}", CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return context;
    }
}
