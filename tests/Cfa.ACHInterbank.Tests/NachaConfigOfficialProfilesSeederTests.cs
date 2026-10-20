using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaConfigOfficialProfilesSeederTests : IClassFixture<OfficialNachaGenerationFixture>
{
    private static readonly string[] RequiredRecords = ["1", "5", "6", "7", "8", "9"];
    private readonly OfficialNachaGenerationFixture _fixture;

    public NachaConfigOfficialProfilesSeederTests(OfficialNachaGenerationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NachaConfigSeeds_ShouldCreatePublishedAchColombiaProfile()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, AchColOfficialNachaLayout.OutboundOriginalProfileCode);

        profile.Should().NotBeNull();
        profile!.ClearingHouse.Code.Should().Be("ACH");
        profile.Status.Code.Should().Be("PUBLICADO");
        profile.EffectiveFrom.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        profile.EffectiveTo.Should().BeNull();
        profile.Tags.Should().ContainSingle(tag =>
            tag.TagKey == NachaSettlementPolicyMetadata.TagKey
            && tag.TagValue == nameof(NachaSettlementPolicy.SettlementDate));
    }

    [Fact]
    public async Task OfficialBootstrap_ShouldCreateCompleteVersionedSnapshotForEveryPublishedProfile()
    {
        await using var context = await SeedAsync();
        var publishedProfileIds = await context.CfgProfiles
            .Where(profile => profile.Status.Code == "PUBLICADO")
            .Select(profile => profile.Id)
            .ToArrayAsync();
        var snapshots = await context.HistConfigSnapshots
            .Where(snapshot => publishedProfileIds.Contains(snapshot.ProfileId) && snapshot.SnapshotType == "PUBLISH")
            .ToArrayAsync();

        snapshots.Should().HaveCount(publishedProfileIds.Length);
        snapshots.Should().OnlyContain(snapshot => NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(snapshot.SnapshotJson).IsSupported);
        snapshots.Should().Contain(snapshot => snapshot.SnapshotJson.Length > 16_000);
    }

    [Fact]
    public async Task CompleteSnapshot_ShouldRoundTripDeterministically_AndCaptureSemanticRulesByValue()
    {
        await using var context = await SeedAsync();
        var profileId = await context.CfgProfiles
            .Where(profile => profile.ProfileCode == AchColOfficialNachaLayout.OutboundOriginalProfileCode)
            .Select(profile => profile.Id)
            .SingleAsync();
        var persisted = await context.HistConfigSnapshots
            .SingleAsync(snapshot => snapshot.ProfileId == profileId && snapshot.SnapshotType == "PUBLISH");

        var read = NachaPublicationSnapshotSerializer.Read(persisted.SnapshotJson);

        read.Status.Should().Be(NachaPublicationSnapshotReadStatus.Supported);
        NachaPublicationSnapshotSerializer.Serialize(read.Snapshot!).Should().Be(persisted.SnapshotJson);
        read.Snapshot!.GenerationCriticalTags.Select(tag => tag.Key).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        read.Snapshot.GenerationCriticalTags.Should().Contain(tag => tag.Key == NachaSettlementPolicyMetadata.TagKey);
        read.Snapshot.GenerationCriticalTags.Should().Contain(tag => tag.Key.StartsWith(NachaOutboundPolicyMetadata.Prefix, StringComparison.Ordinal));
        read.Snapshot.Records.Select(record => record.Sequence).Should().BeInAscendingOrder();
        read.Snapshot.LayoutVariants.Should().Equal(read.Snapshot.LayoutVariants
            .OrderBy(variant => variant.RecordCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(variant => variant.Priority)
            .ThenBy(variant => variant.VariantCode, StringComparer.OrdinalIgnoreCase));
        read.Snapshot.LayoutVariants.Should().Contain(variant => variant.SelectionPredicate.HasValue);
        var fields = read.Snapshot.LayoutVariants.SelectMany(variant => variant.Fields).ToArray();
        fields.Should().NotBeEmpty();
        fields.Should().OnlyContain(field => !string.IsNullOrWhiteSpace(field.Source.DataSourceTypeCode));
        fields.Should().Contain(field => field.Rules.Count > 0);
        persisted.SnapshotJson.Should().Contain("\"transformationPipeline\"");

        var record5 = read.Snapshot.Records.Single(record => record.RecordCode == "5");
        record5.SemanticRuleSetId.Should().NotBeNull();
        var semantic = record5.SemanticRuleSet!;
        semantic.ResolvedDeclarations.Should().BeEquivalentTo(
        [
            new NachaPublicationSnapshotSemanticDeclaration("200", true, true),
            new NachaPublicationSnapshotSemanticDeclaration("220", true, false),
            new NachaPublicationSnapshotSemanticDeclaration("225", false, true)
        ], options => options.WithStrictOrdering());

        var persistedRule = await context.CfgRuleSetRules
            .SingleAsync(rule => rule.RuleSetId == semantic.RuleSetId && rule.RuleCode.EndsWith("220"));
        persistedRule.RuleConfigJson = "{\"serviceClassCode\":\"220\",\"allowedDirections\":[\"DEBIT\"]}";
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var historical = NachaPublicationSnapshotSerializer.Read(persisted.SnapshotJson).Snapshot!;
        historical.Records.Single(record => record.RecordCode == "5").SemanticRuleSet!.ResolvedDeclarations
            .Single(rule => rule.ServiceClassCode == "220").AllowsCredit.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-json", NachaPublicationSnapshotReadStatus.Malformed)]
    [InlineData("{\"profileCode\":\"legacy\"}", NachaPublicationSnapshotReadStatus.LegacyOrIncomplete)]
    [InlineData("{\"snapshotFormatVersion\":999}", NachaPublicationSnapshotReadStatus.UnsupportedVersion)]
    [InlineData("{\"snapshotFormatVersion\":1}", NachaPublicationSnapshotReadStatus.LegacyOrIncomplete)]
    public void SnapshotReader_ShouldFailClosed(string json, NachaPublicationSnapshotReadStatus expectedStatus)
    {
        var result = NachaPublicationSnapshotSerializer.Read(json);

        result.Status.Should().Be(expectedStatus);
        result.Snapshot.Should().BeNull();
    }

    [Fact]
    public async Task NachaConfigSeeds_ShouldCreatePublishedCenitProfile()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");

        profile.Should().NotBeNull();
        profile!.ClearingHouse.Code.Should().Be("CENIT");
        profile.Status.Code.Should().Be("PUBLICADO");
        profile.EffectiveFrom.Should().Be(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc));
        profile.EffectiveTo.Should().BeNull();
        profile.Tags.Should().Contain(tag => tag.TagKey == "NormativeVersion" && tag.TagValue == "2026-05-07")
            .And.Contain(tag => tag.TagKey == "IsPlaceholder" && tag.TagValue == "false")
            .And.Contain(tag => tag.TagKey == "IsHomologated" && tag.TagValue == "false")
            .And.ContainSingle(tag => tag.TagKey == NachaSettlementPolicyMetadata.TagKey
                                      && tag.TagValue == nameof(NachaSettlementPolicy.JulianSettlementDate));
    }

    [Fact]
    public async Task AchColombiaAndCenitProfiles_ShouldBeIndependent()
    {
        await using var context = await SeedAsync();
        var ach = await LoadProfileAsync(context, AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        var cenit = await LoadProfileAsync(context, "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");

        ach!.Id.Should().NotBe(cenit!.Id);
        ach.ClearingHouseId.Should().NotBe(cenit.ClearingHouseId);
        ach.LayoutVariants.Select(x => x.Id).Should().NotIntersectWith(cenit.LayoutVariants.Select(x => x.Id));
        ach.LayoutVariants.SelectMany(x => x.Fields).Select(x => x.Id)
            .Should().NotIntersectWith(cenit.LayoutVariants.SelectMany(x => x.Fields).Select(x => x.Id));
    }

    [Fact]
    public async Task OfficialProfiles_ShouldSeedAndResolveChamberSpecificSemanticRuleSetsIdempotently()
    {
        await using var context = await SeedAsync();
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        var ach = await LoadProfileAsync(context, AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        var cenit = await LoadProfileAsync(context, CenitOrdinaryOutbound2026Layout.OriginalProfileCode);

        var achRecord5 = ach!.Records.Single(record => record.RecordCode.Code == "5");
        var cenitRecord5 = cenit!.Records.Single(record => record.RecordCode.Code == "5");
        achRecord5.SemanticRuleSetId.Should().NotBeNull();
        cenitRecord5.SemanticRuleSetId.Should().NotBeNull();
        achRecord5.SemanticRuleSetId.Should().NotBe(cenitRecord5.SemanticRuleSetId);
        ach.Records.Where(record => record.RecordCode.Code != "5").Should().OnlyContain(record => record.SemanticRuleSetId == null);
        cenit.Records.Where(record => record.RecordCode.Code != "5").Should().OnlyContain(record => record.SemanticRuleSetId == null);

        var achContract = NachaSemanticContractMetadata.Resolve(ach.Records);
        achContract.Status.Should().Be(NachaSemanticContractMetadataStatus.Resolved);
        achContract.Contract!.TryGetRule("200", out var rule200).Should().BeTrue();
        rule200.Should().Match<NachaServiceClassSemanticRule>(rule => rule.AllowsCredit && rule.AllowsDebit);
        achContract.Contract.TryGetRule("220", out var rule220).Should().BeTrue();
        rule220.Should().Match<NachaServiceClassSemanticRule>(rule => rule.AllowsCredit && !rule.AllowsDebit);
        achContract.Contract.TryGetRule("225", out var rule225).Should().BeTrue();
        rule225.Should().Match<NachaServiceClassSemanticRule>(rule => !rule.AllowsCredit && rule.AllowsDebit);

        (await context.CfgRuleSets.CountAsync()).Should().Be(4);
        (await context.CfgRuleSetRules.CountAsync()).Should().Be(30);
    }

    [Fact]
    public async Task PublishedProfileSeedRerun_ShouldNotRewriteGenerationDefinition()
    {
        await using var context = await SeedAsync();
        var profileCode = AchColOfficialNachaLayout.OutboundOriginalProfileCode;
        var field = await context.CfgLayoutFields
            .Where(candidate => candidate.LayoutVariant.Profile.ProfileCode == profileCode)
            .OrderBy(candidate => candidate.Id)
            .FirstAsync();
        var preservedAuditTimestamp = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        field.UpdatedAt = preservedAuditTimestamp;
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();
        preservedAuditTimestamp = await context.CfgLayoutFields
            .Where(candidate => candidate.Id == field.Id)
            .Select(candidate => candidate.UpdatedAt)
            .SingleAsync();

        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        (await context.CfgLayoutFields.AsNoTracking().SingleAsync(candidate => candidate.Id == field.Id))
            .UpdatedAt.Should().Be(preservedAuditTimestamp);
    }

    [Fact]
    public async Task ConflictingPublishedProfileSeedRerun_ShouldFailClosedWithoutOverwrite()
    {
        await using var context = await SeedAsync();
        var profileCode = AchColOfficialNachaLayout.OutboundOriginalProfileCode;
        var field = await context.CfgLayoutFields
            .Where(candidate => candidate.LayoutVariant.Profile.ProfileCode == profileCode)
            .OrderBy(candidate => candidate.Id)
            .FirstAsync();
        field.Length += 1;
        var conflictingLength = field.Length;
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();

        var call = () => new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(call);
        exception.Message.Should().Contain("NEW PROFILE VERSION REQUIRED");
        (await context.CfgLayoutFields.AsNoTracking().SingleAsync(candidate => candidate.Id == field.Id))
            .Length.Should().Be(conflictingLength);
    }

    [Fact]
    public async Task PublishedSemanticRuleSetSeedRerun_ShouldFailClosedWithoutRewrite()
    {
        await using var context = await SeedAsync();
        var rule = await context.CfgRuleSetRules
            .Where(candidate => candidate.RuleSet.RuleSetCode == "NACHA_ACH_SERVICE_CLASS_DIRECTION_V1")
            .OrderBy(candidate => candidate.Order)
            .FirstAsync();
        rule.RuleConfigJson = """{"serviceClassCode":"200","allowedDirections":["CREDIT"]}""";
        var conflictingRule = rule.RuleConfigJson;
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();

        var call = () => new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(call);
        exception.Message.Should().Contain("NEW PROFILE VERSION REQUIRED");
        (await context.CfgRuleSetRules.AsNoTracking().SingleAsync(candidate => candidate.Id == rule.Id))
            .RuleConfigJson.Should().Be(conflictingRule);
    }

    [Fact]
    public async Task DraftOfficialProfileSeedRerun_ShouldRestoreAuthoritativeDefinition()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles
            .SingleAsync(candidate => candidate.ProfileCode == AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        profile.StatusId = await context.CatConfigStatuses
            .Where(candidate => candidate.Code == "BORRADOR")
            .Select(candidate => candidate.Id)
            .SingleAsync();
        profile.PublishedAt = null;
        profile.PublishedBy = null;
        var field = await context.CfgLayoutFields
            .Where(candidate => candidate.LayoutVariant.ProfileId == profile.Id)
            .OrderBy(candidate => candidate.Id)
            .FirstAsync();
        var authoritativeLength = field.Length;
        field.Length += 1;
        // Reconstruct a genuinely unpublished draft; status alone cannot erase publication.
        context.HistConfigSnapshots.RemoveRange(await context.HistConfigSnapshots
            .Where(snapshot => snapshot.ProfileId == profile.Id && snapshot.SnapshotType == "PUBLISH")
            .ToListAsync());
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();

        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        (await context.CfgLayoutFields.AsNoTracking().SingleAsync(candidate => candidate.Id == field.Id))
            .Length.Should().Be(authoritativeLength);
        var reloaded = await context.CfgProfiles
            .Include(candidate => candidate.Status)
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == profile.Id);
        reloaded.Status.Code.Should().Be("PUBLICADO");
    }

    [Fact]
    public async Task InactivatedPublishedProfileSeedRerun_ShouldPreserveLifecycleStatus()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles
            .SingleAsync(candidate => candidate.ProfileCode == AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        profile.StatusId = await context.CatConfigStatuses
            .Where(candidate => candidate.Code == "INACTIVO")
            .Select(candidate => candidate.Id)
            .SingleAsync();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        var statusCode = await context.CfgProfiles
            .Where(candidate => candidate.Id == profile.Id)
            .Select(candidate => candidate.Status.Code)
            .SingleAsync();
        statusCode.Should().Be("INACTIVO");
    }

    [Fact]
    public async Task PublishAsync_ShouldRejectRepublishWithoutMutation()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles
            .AsNoTracking()
            .SingleAsync(candidate => candidate.ProfileCode == AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        var snapshotsBefore = await context.HistConfigSnapshots.CountAsync(candidate => candidate.ProfileId == profile.Id);
        var changesBefore = await context.HistConfigChanges.CountAsync(candidate => candidate.ProfileId == profile.Id);
        var fieldBefore = await context.CfgLayoutFields
            .Where(candidate => candidate.LayoutVariant.ProfileId == profile.Id)
            .OrderBy(candidate => candidate.Id)
            .Select(candidate => new { candidate.Id, candidate.StartPosition, candidate.Length })
            .FirstAsync();
        var publication = new NachaConfigPublicationService(context, new AlwaysValidNachaConfigValidationService());

        var call = () => publication.PublishAsync(profile.Id, "publisher", Convert.ToBase64String(profile.RowVersion));

        var exception = await Assert.ThrowsAsync<NachaConfigException>(call);
        exception.ErrorCode.Should().Be("INVALID_PROFILE_STATE");
        var reloaded = await context.CfgProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == profile.Id);
        reloaded.VersionMinor.Should().Be(profile.VersionMinor);
        reloaded.PublishedAt.Should().Be(profile.PublishedAt);
        var fieldAfter = await context.CfgLayoutFields.AsNoTracking()
            .Where(candidate => candidate.Id == fieldBefore.Id)
            .Select(candidate => new { candidate.Id, candidate.StartPosition, candidate.Length })
            .SingleAsync();
        fieldAfter.Should().BeEquivalentTo(fieldBefore);
        (await context.HistConfigSnapshots.CountAsync(candidate => candidate.ProfileId == profile.Id)).Should().Be(snapshotsBefore);
        (await context.HistConfigChanges.CountAsync(candidate => candidate.ProfileId == profile.Id)).Should().Be(changesBefore);
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishValidDraftOnce()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles
            .SingleAsync(candidate => candidate.ProfileCode == AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        profile.StatusId = await context.CatConfigStatuses
            .Where(candidate => candidate.Code == "BORRADOR")
            .Select(candidate => candidate.Id)
            .SingleAsync();
        profile.PublishedAt = null;
        profile.PublishedBy = null;
        context.HistConfigSnapshots.RemoveRange(await context.HistConfigSnapshots
            .Where(snapshot => snapshot.ProfileId == profile.Id && snapshot.SnapshotType == "PUBLISH")
            .ToListAsync());
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();
        profile = await context.CfgProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == profile.Id);
        var publication = new NachaConfigPublicationService(context, new AlwaysValidNachaConfigValidationService());

        var result = await publication.PublishAsync(
            profile.Id,
            "publisher",
            Convert.ToBase64String(profile.RowVersion));

        result.Publicado.Should().BeTrue();
        result.VersionMajor.Should().Be(profile.VersionMajor);
        result.VersionMinor.Should().Be(profile.VersionMinor + 1);
        var snapshots = await context.HistConfigSnapshots
            .Where(candidate => candidate.ProfileId == profile.Id && candidate.SnapshotType == "PUBLISH")
            .OrderBy(candidate => candidate.VersionMinor)
            .ToArrayAsync();
        snapshots.Should().HaveCount(1);
        var publicationSnapshot = NachaPublicationSnapshotSerializer.Read(snapshots[^1].SnapshotJson);
        publicationSnapshot.IsSupported.Should().BeTrue(publicationSnapshot.Error);
        NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(snapshots[^1].SnapshotJson)
            .IsSupported.Should().BeTrue();
        publicationSnapshot.Snapshot!.StandardEntryClassMappings.Should().NotBeEmpty();
        var activeCatalog = await context.CompanyEntryDescriptionCatalogs.AsNoTracking()
            .Where(item => item.IsActive).Select(item => new { item.Term, item.StandardEntryClassCode }).ToArrayAsync();
        publicationSnapshot.Snapshot.StandardEntryClassMappings.Should().BeEquivalentTo(activeCatalog);
        publicationSnapshot.Snapshot!.Profile.VersionMinor.Should().Be(profile.VersionMinor + 1);
        (await context.HistConfigChanges.CountAsync(candidate =>
            candidate.ProfileId == profile.Id && candidate.ChangeType == "PUBLISH")).Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_ShouldRollback_WhenCompleteSnapshotCannotBeBuilt()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles
            .SingleAsync(candidate => candidate.ProfileCode == AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        profile.StatusId = await context.CatConfigStatuses
            .Where(candidate => candidate.Code == "BORRADOR")
            .Select(candidate => candidate.Id)
            .SingleAsync();
        profile.PublishedAt = null;
        profile.PublishedBy = null;
        var variant = await context.CfgLayoutVariants.FirstAsync(candidate => candidate.ProfileId == profile.Id);
        variant.SelectionPredicateJson = "not-json";
        context.HistConfigSnapshots.RemoveRange(await context.HistConfigSnapshots
            .Where(snapshot => snapshot.ProfileId == profile.Id && snapshot.SnapshotType == "PUBLISH")
            .ToListAsync());
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();
        profile = await context.CfgProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == profile.Id);
        var snapshotsBefore = await context.HistConfigSnapshots.CountAsync(candidate => candidate.ProfileId == profile.Id);
        var publication = new NachaConfigPublicationService(context, new AlwaysValidNachaConfigValidationService());

        var call = () => publication.PublishAsync(profile.Id, "publisher", Convert.ToBase64String(profile.RowVersion));

        await call.Should().ThrowAsync<InvalidOperationException>().WithMessage("SNAPSHOT_JSON_INVALID:*");
        var reloaded = await context.CfgProfiles.Include(candidate => candidate.Status).AsNoTracking().SingleAsync(candidate => candidate.Id == profile.Id);
        reloaded.Status.Code.Should().Be("BORRADOR");
        reloaded.PublishedAt.Should().BeNull();
        (await context.HistConfigSnapshots.CountAsync(candidate => candidate.ProfileId == profile.Id)).Should().Be(snapshotsBefore);
        (await context.HistConfigChanges.CountAsync(candidate => candidate.ProfileId == profile.Id && candidate.ChangeType == "PUBLISH")).Should().Be(0);
    }

    [Fact]
    public async Task AchColombiaProfile_ShouldContainRecords_1_5_6_7_8_9()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, AchColOfficialNachaLayout.OutboundOriginalProfileCode);

        profile!.Records.Select(x => x.RecordCode.Code).Should().BeEquivalentTo(RequiredRecords);
    }

    [Fact]
    public async Task CenitProfile_ShouldContainRecords_1_5_6_7_8_9()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");

        profile!.Records.Select(x => x.RecordCode.Code).Should().BeEquivalentTo(RequiredRecords);
    }

    [Fact]
    public async Task AchColombiaProfile_ShouldContainLayoutFieldsForAllRecords()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, AchColOfficialNachaLayout.OutboundOriginalProfileCode);

        AssertFieldsForAllRecords(profile!);
    }

    [Fact]
    public async Task OrdinaryAchColombiaV35Profiles_ShouldExposeDirectionAndCreditAddendaLayouts()
    {
        await using var context = await SeedAsync();
        var outbound = await LoadProfileAsync(context, AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        var inbound = await LoadProfileAsync(context, AchColOfficialNachaLayout.InboundOriginalProfileCode);
        var prenote = await LoadProfileAsync(context, AchColOfficialNachaLayout.OutboundPrenotificationProfileCode);

        Constant(outbound!, "1", "IMMEDIATEDESTINATION").Should().Be("000101006");
        Constant(outbound!, "1", "IMMEDIATEORIGIN").Should().Be("000128300");
        Constant(inbound!, "1", "IMMEDIATEDESTINATION").Should().Be("000128300");
        Constant(inbound!, "1", "IMMEDIATEORIGIN").Should().Be("000101006");

        var monetaryCredit = outbound!.LayoutVariants.Single(variant =>
            variant.VariantCode == AchColOfficialNachaLayout.Type7CreditMonetaryVariant);
        monetaryCredit.Fields.Single(field => field.FieldCode == "INVOICEORACCOUNTNUMBER")
            .Should().Match<CfgLayoutField>(field => field.StartPosition == 31 && field.Length == 24);
        monetaryCredit.Fields.Single(field => field.FieldCode == "ORIGINATORFREEINFORMATION")
            .Should().Match<CfgLayoutField>(field => field.StartPosition == 57 && field.Length == 24);

        var prenoteCredit = prenote!.LayoutVariants.Single(variant =>
            variant.VariantCode == AchColOfficialNachaLayout.Type7CreditPrenotificationVariant);
        prenoteCredit.Fields.Single(field => field.FieldCode == "REFERENCE")
            .Should().Match<CfgLayoutField>(field => field.StartPosition == 31 && field.Length == 53);

        var activeOrdinary = await context.CfgProfiles
            .Include(profile => profile.ClearingHouse)
            .Include(profile => profile.FlowType)
            .Include(profile => profile.Status)
            .Include(profile => profile.Tags)
            .Where(profile => profile.ClearingHouse.Code == "ACH"
                              && (profile.FlowType.Code == "ORIGINAL" || profile.FlowType.Code == "PRENOTIFICACION")
                              && profile.Status.Code == "PUBLICADO")
            .ToListAsync();
        activeOrdinary.Should().HaveCount(8)
            .And.OnlyContain(profile => profile.VersionMajor == 35
                                        && (profile.VersionMinor == 0 || profile.VersionMinor == 1)
                                        && profile.Tags.Any(tag => tag.TagKey == "NormativeVersion" && tag.TagValue == "V35"));
        activeOrdinary.Should().NotContain(profile => profile.Tags.Any(tag => tag.TagValue.Contains("V32")));
    }

    [Fact]
    public async Task CenitProfile_ShouldContainLayoutFieldsForAllRecords()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");

        AssertFieldsForAllRecords(profile!);
    }

    [Fact]
    public async Task CenitOrdinaryProfile_ShouldSeedExactMay2026CriticalOffsets()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, CenitOrdinaryOutbound2026Layout.OriginalProfileCode);

        AssertField(profile!, "1", "FILECREATIONDATE", 24, 8);
        AssertField(profile!, "1", "FILEIDMODIFIER", 36, 1);
        AssertField(profile!, "5", "STANDARDENTRYCLASSCODE", 51, 3);
        AssertField(profile!, "5", "SETTLEMENTDATE", 80, 3);
        AssertField(profile!, "6", "AMOUNT", 30, 18);
        AssertField(profile!, "6", "INDIVIDUALIDENTIFICATION", 48, 15);
        AssertField(profile!, "6", "TRACENUMBER", 88, 15);
        AssertField(profile!, "7", "PAYMENTRELATEDINFORMATION", 4, 80);
        AssertField(profile!, "7", "SEQUENCENUMBER", 84, 4);
        AssertField(profile!, "8", "TOTALDEBITAMOUNT", 21, 18);
        AssertField(profile!, "8", "TOTALCREDITAMOUNT", 39, 18);
        AssertField(profile!, "9", "TOTALDEBITAMOUNT", 32, 18);
        AssertField(profile!, "9", "TOTALCREDITAMOUNT", 50, 18);
        profile!.LayoutVariants.Should().OnlyContain(variant => variant.TotalLength == 106);
        profile.LayoutVariants.Single(variant => variant.RecordCode.Code == "7")
            .VariantCode.Should().Be(CenitOrdinaryOutbound2026Layout.Addenda05Variant);
    }

    [Fact]
    public async Task CenitCtxProfile_ShouldSeedOfficialType5Type6AndType7Layouts()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, CenitCtxOutbound2026Layout.OriginalProfileCode);

        profile.Should().NotBeNull();
        profile!.ServiceClass!.Code.Should().Be("CTX");
        profile.Records.Select(record => record.RecordCode.Code).Should().BeEquivalentTo(RequiredRecords);
        profile.LayoutVariants.Should().OnlyContain(variant => variant.TotalLength == 106);
        AssertField(profile, "5", "STANDARDENTRYCLASSCODE", 51, 3);
        AssertField(profile, "6", "AMOUNT", 30, 18);
        AssertField(profile, "6", "ADDENDACOUNT", 63, 4);
        AssertField(profile, "6", "INDIVIDUALNAME", 67, 16);
        AssertField(profile, "6", "ADDENDARECORDINDICATOR", 87, 1);
        AssertField(profile, "6", "TRACENUMBER", 88, 15);
        AssertField(profile, "7", "PAYMENTRELATEDINFORMATION", 4, 80);
        AssertField(profile, "7", "SEQUENCENUMBER", 84, 4);
        AssertField(profile, "7", "TRACESUFFIX", 88, 7);
        Constant(profile, "7", "ADDENDATYPE").Should().Be("05");
        profile.LayoutVariants.Single(variant => variant.RecordCode.Code == "7")
            .Fields.Single(field => field.FieldCode == "SEQUENCENUMBER")
            .SourceDefinition.PropertyPath.Should().Be("SequenceNumber");
    }

    [Fact]
    public async Task CenitOutboundProfiles_ShouldPublishResolvedPartitionPolicies()
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);
        var ordinary = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RequireOutboundPolicy = true,
            RecordCodes = RequiredRecords
        });
        var ctx = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "CTX",
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RequireOutboundPolicy = true,
            RecordCodes = RequiredRecords
        });

        ordinary.Success.Should().BeTrue(string.Join(" | ", ordinary.Trace));
        ordinary.SettlementPolicy.Should().Be(NachaSettlementPolicy.JulianSettlementDate);
        ordinary.OutboundPolicy.Should().NotBeNull();
        ordinary.OutboundPolicy!.FileAllocation.Should().Be(NachaOutboundFileAllocation.CombineServicePartitionsByIndex);
        ordinary.OutboundPolicy.Services.Single(service => service.ServiceCode == "PPD")
            .Should().Match<NachaOutboundServicePartitionPolicy>(service =>
                service.Strategy == NachaOutboundServicePartitionStrategy.EntriesPerFile
                && service.MaxEntriesPerFile == 10_000);
        ordinary.OutboundPolicy.Services.Single(service => service.ServiceCode == "CCD")
            .Should().Match<NachaOutboundServicePartitionPolicy>(service =>
                service.Strategy == NachaOutboundServicePartitionStrategy.FixedEntriesPerBatch
                && service.EntriesPerBatch == 1
                && service.MaxBatchesPerFile == 10_000
                && service.MinAddendaPerEntry == 1);

        ctx.Success.Should().BeTrue(string.Join(" | ", ctx.Trace));
        ctx.OutboundPolicy.Should().NotBeNull();
        ctx.OutboundPolicy!.FileAllocation.Should().Be(NachaOutboundFileAllocation.IndependentServiceFiles);
        ctx.OutboundPolicy.Services.Single().Should().Match<NachaOutboundServicePartitionPolicy>(service =>
            service.ServiceCode == "CTX"
            && service.Strategy == NachaOutboundServicePartitionStrategy.PreserveSourceBatches
            && service.MinAddendaPerEntry == 1
            && service.MaxAddendaPerEntry == 9_999);
    }

    [Fact]
    public async Task AchOutboundProfile_ShouldFailClosed_WhenPublishedBatchPolicyIsIncomplete()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles
            .Include(candidate => candidate.Tags)
            .SingleAsync(candidate => candidate.ProfileCode == AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        context.CfgProfileTags.Remove(profile.Tags.Single(tag =>
            tag.TagKey == NachaOutboundPolicyMetadata.BatchNumberMaximumKey));
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            RequestedVersionMajor = AchColOfficialNachaLayout.ProfileVersionMajor,
            RequestedVersionMinor = AchColOfficialNachaLayout.ProfileVersionMinor,
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.OutboundPolicyInvalid);
    }

    [Fact]
    public async Task PublishedProfiles_ShouldHaveEffectiveDates()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);
        static bool IsCenitOutboundProfile(string profileCode) =>
            CenitOrdinaryOutbound2026Layout.IsProfile(profileCode)
            || CenitCtxOutbound2026Layout.IsProfile(profileCode)
            || profileCode == CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode
            || profileCode == CenitOrdinaryOutbound2026Layout.TxCodeAwarePrenotificationProfileCode
            || profileCode == CenitCtxOutbound2026Layout.TxCodeAwareOriginalProfileCode
            || profileCode == CenitCtxOutbound2026Layout.TxCodeAwarePrenotificationProfileCode;

        profiles.Should().OnlyContain(x => x.PublishedAt.HasValue);
        profiles.Where(profile => IsCenitOutboundProfile(profile.ProfileCode))
            .Should().OnlyContain(profile => profile.EffectiveFrom == new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc));
        profiles.Where(profile => !IsCenitOutboundProfile(profile.ProfileCode)
                                  && !CenitOrdinaryInbound2026Layout.IsProfile(profile.ProfileCode))
            .Should().OnlyContain(profile => profile.EffectiveFrom == new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        profiles.Should().OnlyContain(x => x.EffectiveTo == null);
    }

    [Fact]
    public async Task PublishedProfiles_ShouldHaveNormativeSource()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        profiles.Should().OnlyContain(x => x.Tags.Any(t => t.TagKey == "NormativeSource" && !string.IsNullOrWhiteSpace(t.TagValue)));
        profiles.Where(x => x.ClearingHouse.Code == "ACH")
            .Should().OnlyContain(x => x.Tags.Any(t => t.TagKey == "NormativeVersion" && t.TagValue == "V35"));
        profiles.Single(x => x.ProfileCode == "OFFICIAL_ACH_SALIDA_DEVOLUCION_V35_1_0").Tags
            .Should().Contain(t => t.TagKey == "NormativeVersion" && t.TagValue == "V35")
            .And.Contain(t => t.TagKey == "NormativeSource" && t.TagValue.Contains("sección 6.6"));
        profiles.Where(x => CenitOrdinaryOutbound2026Layout.IsProfile(x.ProfileCode))
            .Should().HaveCount(6)
            .And.OnlyContain(x => x.Tags.Any(t => t.TagKey == "NormativeVersion" && t.TagValue == "2026-05-07")
                                  && x.Tags.Any(t => t.TagKey == "NormativeSource" && t.TagValue.Contains("Formato NACHA-M CENIT"))
                                  && x.Tags.Any(t => t.TagKey == "IsPlaceholder" && t.TagValue == "false")
                                  && x.Tags.Any(t => t.TagKey == "IsHomologated" && t.TagValue == "false"));
        profiles.Single(x => x.ProfileCode == CenitReturnIn2026Layout.ProfileCode).Tags
            .Should().Contain(t => t.TagKey == "NormativeVersion" && t.TagValue == "2026-05-07")
            .And.Contain(t => t.TagKey == "NormativeSource" && t.TagValue.Contains("Formato NACHA-M CENIT"));
        profiles.Single(x => x.ProfileCode == CenitReturnOut2026Layout.ProfileCode).Tags
            .Should().Contain(t => t.TagKey == "NormativeVersion" && t.TagValue == CenitReturnOut2026Layout.NormativeVersion)
            .And.Contain(t => t.TagKey == "NormativeSource" && t.TagValue.Contains("Formato NACHA-M CENIT"));
        profiles.Where(x => x.ProfileCode == CenitReturnOfReturn2026Layout.InProfileCode
                            || x.ProfileCode == CenitReturnOfReturn2026Layout.OutProfileCode)
            .Should().HaveCount(2)
            .And.OnlyContain(x => x.Tags.Any(t => t.TagKey == "NormativeVersion"
                                                  && t.TagValue == CenitReturnOfReturn2026Layout.NormativeVersion)
                                  && x.Tags.Any(t => t.TagKey == "NormativeSource"
                                                     && t.TagValue.Contains("Formato NACHA-M CENIT")));
    }

    [Fact]
    public async Task ProfileFields_ShouldNotOverlapPositions()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        foreach (var variant in profiles.SelectMany(x => x.LayoutVariants))
        {
            var ordered = variant.Fields.OrderBy(x => x.StartPosition).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var previous = ordered[i - 1];
                var current = ordered[i];
                current.StartPosition.Should().BeGreaterThanOrEqualTo(previous.StartPosition + previous.Length);
            }
        }
    }

    [Fact]
    public async Task ProfileFields_ShouldNotHaveInvalidLengths()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        foreach (var variant in profiles.SelectMany(x => x.LayoutVariants))
        {
            variant.TotalLength.Should().Be(106);
            variant.Fields.Should().OnlyContain(x => x.Length > 0);
            variant.Fields.Max(x => x.StartPosition + x.Length - 1).Should().Be(106);
        }
    }

    [Fact]
    public async Task RequiredFields_ShouldHaveSourceConstantOrCalculation()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        foreach (var field in profiles.SelectMany(x => x.LayoutVariants).SelectMany(x => x.Fields))
        {
            var source = field.SourceDefinition;
            source.Should().NotBeNull();
            var hasSource = !string.IsNullOrWhiteSpace(source.PropertyPath)
                || !string.IsNullOrWhiteSpace(source.ConstantValue)
                || !string.IsNullOrWhiteSpace(source.ExpressionDsl);
            hasSource.Should().BeTrue($"field {field.FieldCode} must have a source, constant or calculation");
        }
    }

    [Fact]
    public async Task CalculatedFields_ShouldHaveCalculationType()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        var calculatedFields = profiles.SelectMany(x => x.LayoutVariants)
            .SelectMany(x => x.Fields)
            .Where(x => x.SourceDefinition.DataSourceType.Code == "EXPRESION")
            .ToList();

        calculatedFields.Should().NotBeEmpty();
        foreach (var field in calculatedFields)
        {
            using var document = JsonDocument.Parse(field.SourceDefinition.ExpressionDsl!);
            document.RootElement.TryGetProperty("calculationType", out var calculationType).Should().BeTrue();
            calculationType.GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task SourceFields_ShouldHaveSourceFieldPath()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        var sourceFields = profiles.SelectMany(x => x.LayoutVariants)
            .SelectMany(x => x.Fields)
            .Where(x => x.SourceDefinition.DataSourceType.Code == "ENTIDAD")
            .ToList();

        sourceFields.Should().NotBeEmpty();
        sourceFields.Should().OnlyContain(x =>
            !string.IsNullOrWhiteSpace(x.SourceDefinition.EntityName)
            && !string.IsNullOrWhiteSpace(x.SourceDefinition.PropertyPath));
    }

    [Fact]
    public async Task Profiles_ShouldBeResolvableByNachaConfigResolver_ForAchColombia()
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);

        var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeTrue();
        result.UsedFallback.Should().BeFalse();
        result.Profile!.ProfileCode.Should().Be(AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        result.Profile.VersionMajor.Should().Be(AchColOfficialNachaLayout.ProfileVersionMajor);
        result.Profile.VersionMinor.Should().Be(AchColOfficialNachaLayout.TxCodeAwareProfileVersionMinor);
        result.LayoutsByRecordCode.Keys.Should().BeEquivalentTo(RequiredRecords);
    }

    [Fact]
    public async Task Profiles_ShouldBeResolvableByNachaConfigResolver_ForCenit()
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);

        var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeTrue();
        result.UsedFallback.Should().BeFalse();
        result.Profile!.ProfileCode.Should().Be(CenitOrdinaryOutbound2026Layout.CardinalityOriginalProfileCode);
        result.LayoutsByRecordCode.Keys.Should().BeEquivalentTo(RequiredRecords);
    }

    [Fact]
    public async Task CenitCtxProfile_ShouldResolveOnlyForCtxServiceClass()
    {
        await using var context = await SeedAsync();
        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "CTX",
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeTrue(string.Join(" | ", result.Trace));
        result.Profile!.ProfileCode.Should().Be(CenitCtxOutbound2026Layout.TxCodeAwareOriginalProfileCode);
        result.LayoutsByRecordCode.Keys.Should().BeEquivalentTo(RequiredRecords);
    }

    [Theory]
    [InlineData("PPD", "ORIGINAL", null, CenitOrdinaryInbound2026Layout.OriginalProfileCode)]
    [InlineData("CCD", "ORIGINAL", null, CenitOrdinaryInbound2026Layout.OriginalProfileCode)]
    [InlineData("CTX", "ORIGINAL", "CTX", CenitOrdinaryInbound2026Layout.CtxOriginalProfileCode)]
    [InlineData("PPD", "PRENOTIFICACION", null, CenitOrdinaryInbound2026Layout.PrenotificationProfileCode)]
    [InlineData("CCD", "PRENOTIFICACION", null, CenitOrdinaryInbound2026Layout.PrenotificationProfileCode)]
    [InlineData("CTX", "PRENOTIFICACION", "CTX", CenitOrdinaryInbound2026Layout.CtxPrenotificationProfileCode)]
    public async Task CenitInboundProfiles_ShouldResolveByFlowAndPhysicalService(
        string physicalService,
        string flowType,
        string? profileService,
        string expectedProfile)
    {
        await using var context = await SeedAsync();
        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = flowType,
            DirectionCode = "ENTRADA",
            ServiceClassCode = profileService,
            RequestedVersionMajor = 1,
            RequestedVersionMinor = 0,
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeTrue($"{physicalService}: {string.Join(" | ", result.Trace)}");
        result.Profile!.ProfileCode.Should().Be(expectedProfile);
        result.LayoutsByRecordCode.Keys.Should().BeEquivalentTo(RequiredRecords);
    }

    [Fact]
    public async Task CenitInboundCtxProfile_ShouldFailClosedWhenMissingWithoutOutboundFallback()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles.SingleAsync(item =>
            item.ProfileCode == CenitOrdinaryInbound2026Layout.CtxOriginalProfileCode);
        profile.StatusId = await context.CatConfigStatuses
            .Where(status => status.Code == "INACTIVO")
            .Select(status => status.Id)
            .SingleAsync();
        await context.SaveChangesAsync();

        var result = await ResolveCenitInboundAsync(context, "ORIGINAL", "CTX");

        result.Success.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileInactive);
    }

    [Fact]
    public async Task CenitInboundCtxProfile_ShouldFailClosedWhenLayoutIsAmbiguous()
    {
        await using var context = await SeedAsync();
        var source = await context.CfgLayoutVariants.SingleAsync(variant =>
            variant.Profile.ProfileCode == CenitOrdinaryInbound2026Layout.CtxOriginalProfileCode
            && variant.RecordCode.Code == "7");
        context.CfgLayoutVariants.Add(new CfgLayoutVariant
        {
            ProfileId = source.ProfileId,
            RecordCodeId = source.RecordCodeId,
            VariantCode = "TEST_CENIT_CTX_IN_R7_AMBIGUOUS",
            NameEs = "Layout CTX entrada duplicado intencional para prueba",
            Priority = source.Priority,
            TotalLength = source.TotalLength,
            IsDefaultForRecord = source.IsDefaultForRecord,
            EffectiveFrom = source.EffectiveFrom,
            StatusId = source.StatusId,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        });
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var result = await ResolveCenitInboundAsync(context, "ORIGINAL", "CTX");

        result.Success.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileAmbiguous);
    }

    [Fact]
    public async Task CenitCtxProfile_ShouldFailClosedWhenMissing()
    {
        await using var context = await SeedAsync();
        var inactiveStatusId = await context.CatConfigStatuses
            .Where(status => status.Code == "INACTIVO")
            .Select(status => status.Id)
            .SingleAsync();
        var profiles = await context.CfgProfiles.Where(item =>
            item.ProfileCode == CenitCtxOutbound2026Layout.OriginalProfileCode
            || item.ProfileCode == CenitCtxOutbound2026Layout.TxCodeAwareOriginalProfileCode).ToListAsync();
        profiles.Should().HaveCount(2);
        foreach (var profile in profiles)
        {
            profile.StatusId = inactiveStatusId;
        }

        await context.SaveChangesAsync();

        var result = await ResolveCtxProfileAsync(context);

        result.Success.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileInactive);
    }

    [Fact]
    public async Task CenitCtxProfile_ShouldFailClosedWhenAmbiguous()
    {
        await using var context = await SeedAsync();
        var source = await context.CfgLayoutVariants.SingleAsync(variant =>
            variant.Profile.ProfileCode == CenitCtxOutbound2026Layout.TxCodeAwareOriginalProfileCode
            && variant.RecordCode.Code == "6");
        context.CfgLayoutVariants.Add(new CfgLayoutVariant
        {
            ProfileId = source.ProfileId,
            RecordCodeId = source.RecordCodeId,
            VariantCode = "TEST_CENIT_CTX_R6_AMBIGUOUS",
            NameEs = "Layout CTX duplicado intencional para prueba",
            Priority = source.Priority,
            TotalLength = source.TotalLength,
            IsDefaultForRecord = source.IsDefaultForRecord,
            EffectiveFrom = source.EffectiveFrom,
            StatusId = source.StatusId,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        });
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var result = await ResolveCtxProfileAsync(context);

        result.Success.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileAmbiguous);
    }

    [Theory]
    [InlineData("SALIDA", "ORIGINAL", AchColOfficialNachaLayout.OutboundOriginalProfileCode)]
    [InlineData("SALIDA", "PRENOTIFICACION", AchColOfficialNachaLayout.OutboundPrenotificationProfileCode)]
    [InlineData("ENTRADA", "ORIGINAL", AchColOfficialNachaLayout.InboundOriginalProfileCode)]
    [InlineData("ENTRADA", "PRENOTIFICACION", AchColOfficialNachaLayout.InboundPrenotificationProfileCode)]
    public async Task OrdinaryAchColombiaV35ProfileFamily_ShouldResolveExplicitly(
        string directionCode,
        string flowTypeCode,
        string expectedProfileCode)
    {
        await using var context = await SeedAsync();
        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = flowTypeCode,
            DirectionCode = directionCode,
            ServiceClassCode = "PPD",
            ProcessDateUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            RequestedVersionMajor = AchColOfficialNachaLayout.ProfileVersionMajor,
            RequestedVersionMinor = AchColOfficialNachaLayout.ProfileVersionMinor,
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeTrue(string.Join(" | ", result.Trace));
        result.UsedFallback.Should().BeFalse();
        result.Profile!.ProfileCode.Should().Be(expectedProfileCode);
        result.Profile.Tags.Should().Contain(tag => tag.TagKey == "NormativeVersion" && tag.TagValue == "V35");
    }

    [Theory]
    [InlineData("ENTRADA", "UNSUPPORTED")]
    [InlineData("UNSUPPORTED", "ORIGINAL")]
    public async Task OrdinaryAchColombiaV35ProfileFamily_ShouldFailForMissingDimensions(
        string directionCode,
        string flowTypeCode)
    {
        await using var context = await SeedAsync();
        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = flowTypeCode,
            DirectionCode = directionCode,
            ProcessDateUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            RequestedVersionMajor = AchColOfficialNachaLayout.ProfileVersionMajor,
            RequestedVersionMinor = AchColOfficialNachaLayout.ProfileVersionMinor,
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileNotFound);
    }

    [Fact]
    public async Task OrdinaryAchColombiaV35ProfileFamily_ShouldFailForUnsupportedVersion()
    {
        await using var context = await SeedAsync();
        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            RequestedVersionMajor = 32,
            RequestedVersionMinor = 0,
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileVersionUnsupported);
    }

    [Fact]
    public async Task OrdinaryAchColombiaV35ProfileFamily_ShouldFailForAmbiguousProfile()
    {
        await using var context = await SeedAsync();
        var source = await context.CfgProfiles.SingleAsync(profile =>
            profile.ProfileCode == AchColOfficialNachaLayout.InboundOriginalProfileCode);
        context.CfgProfiles.Add(new CfgProfile
        {
            ProfileCode = "TEST_ACH_ENTRADA_ORIGINAL_V35_AMBIGUOUS",
            NameEs = "Duplicado intencional para prueba",
            ClearingHouseId = source.ClearingHouseId,
            FlowTypeId = source.FlowTypeId,
            DirectionId = source.DirectionId,
            ServiceClassId = source.ServiceClassId,
            ContextPriority = source.ContextPriority,
            EffectiveFrom = source.EffectiveFrom,
            StatusId = source.StatusId,
            VersionMajor = source.VersionMajor,
            VersionMinor = source.VersionMinor,
            PublishedAt = source.PublishedAt,
            PublishedBy = "test",
            RowVersion = [9, 9, 9, 9]
        });
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            RequestedVersionMajor = AchColOfficialNachaLayout.ProfileVersionMajor,
            RequestedVersionMinor = AchColOfficialNachaLayout.ProfileVersionMinor,
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileAmbiguous);
    }

    [Fact]
    public async Task IncomingAchColombiaReturnProfile_ShouldBePublishedHomologatedAndVersioned()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0");

        profile.Should().NotBeNull();
        profile!.ClearingHouse.Code.Should().Be("ACH");
        profile.FlowType.Code.Should().Be("RETORNO");
        profile.Direction.Code.Should().Be("ENTRADA");
        profile.Status.Code.Should().Be("PUBLICADO");
        profile.VersionMajor.Should().Be(1);
        profile.VersionMinor.Should().Be(0);
        profile.Tags.Should().Contain(tag => tag.TagKey == "IsHomologated" && tag.TagValue == "true");
        profile.Tags.Should().Contain(tag => tag.TagKey == "NormativeVersion" && tag.TagValue == "V35");
        profile.Records.Select(record => record.RecordCode.Code).Should().BeEquivalentTo(RequiredRecords);

        var type7Variants = profile.LayoutVariants.Where(variant => variant.RecordCode.Code == "7").ToList();
        type7Variants.Should().HaveCount(3);
        type7Variants.Should().ContainSingle(variant =>
            variant.SelectionPredicateJson != null
            && variant.SelectionPredicateJson.Contains("AddendaType", StringComparison.Ordinal)
            && variant.Fields.Any(field => field.FieldCode == "RETURNREASONCODE")
            && variant.Fields.Any(field => field.FieldCode == "ORIGINALTRACENUMBER"));
    }

    [Fact]
    public async Task IncomingAchColombiaReturnProfile_ShouldResolveRealDifferentialDiscriminators()
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);

        var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "RETORNO",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords,
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MessageType"] = "DifferentialResponse",
                ["AddendaType"] = "99"
            },
            RequireHomologated = true
        });

        result.Success.Should().BeTrue(string.Join(" | ", result.Trace));
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileSelected);
        result.Profile!.ProfileCode.Should().Be("OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0");
        result.Profile.VersionMajor.Should().Be(1);
        result.Profile.VersionMinor.Should().Be(0);
        result.LayoutsByRecordCode.Keys.Should().BeEquivalentTo(RequiredRecords);
    }

    [Fact]
    public async Task IncomingCenitReturnProfile_ShouldExposeOfficial2026Contract()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, CenitReturnIn2026Layout.ProfileCode);

        profile.Should().NotBeNull();
        profile!.ClearingHouse.Code.Should().Be("CENIT");
        profile.FlowType.Code.Should().Be("RETORNO");
        profile.Direction.Code.Should().Be("ENTRADA");
        profile.Tags.Should().Contain(tag => tag.TagKey == "IsHomologated" && tag.TagValue == "true");
        profile.Tags.Should().Contain(tag => tag.TagKey == "IsPlaceholder" && tag.TagValue == "false");
        profile.Records.Select(record => record.RecordCode.Code).Should().BeEquivalentTo(RequiredRecords);
        profile.LayoutVariants.Should().HaveCount(6);

        var type6 = profile.LayoutVariants.Single(variant => variant.RecordCode.Code == "6");
        type6.Fields.Single(field => field.FieldCode == "AMOUNT").Should().Match<CfgLayoutField>(field => field.StartPosition == 30 && field.Length == 18);
        type6.Fields.Single(field => field.FieldCode == "TRACENUMBER").Should().Match<CfgLayoutField>(field => field.StartPosition == 88 && field.Length == 15);

        var type7 = profile.LayoutVariants.Single(variant => variant.RecordCode.Code == "7");
        type7.Fields.Single(field => field.FieldCode == "RETURNREASONCODE").Should().Match<CfgLayoutField>(field => field.StartPosition == 4 && field.Length == 3);
        type7.Fields.Single(field => field.FieldCode == "ORIGINALTRACENUMBER").Should().Match<CfgLayoutField>(field => field.StartPosition == 7 && field.Length == 15);
        type7.Fields.Single(field => field.FieldCode == "ADDITIONALINFORMATION").Should().Match<CfgLayoutField>(field => field.StartPosition == 38 && field.Length == 44);
        type7.Fields.Single(field => field.FieldCode == "ADDENDASEQUENCENUMBER").Should().Match<CfgLayoutField>(field => field.StartPosition == 82 && field.Length == 15);
    }

    [Fact]
    public async Task IncomingCenitReturnProfile_ShouldResolveForReturnInOnly()
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);

        var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = "RETORNO",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords,
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MessageType"] = "DifferentialResponse",
                ["AddendaType"] = "99"
            },
            RequireHomologated = true
        });

        result.Success.Should().BeTrue(string.Join(" | ", result.Trace));
        result.Profile!.ProfileCode.Should().Be(CenitReturnIn2026Layout.ProfileCode);
    }

    [Fact]
    public async Task IncomingAchColombiaReturnProfile_SeedShouldBeIdempotent()
    {
        await using var context = await SeedAsync();
        var before = await SnapshotProfileCardinalityAsync(context, "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0");

        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        var after = await SnapshotProfileCardinalityAsync(context, "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0");
        after.Should().Be(before);
        (await context.CfgProfiles.CountAsync(profile =>
            profile.ProfileCode == "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0")).Should().Be(1);
    }

    [Fact]
    public async Task IncomingOrdinaryDimensions_ShouldSelectExplicitV35Profile()
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);

        var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords,
            RequestedVersionMajor = AchColOfficialNachaLayout.ProfileVersionMajor,
            RequestedVersionMinor = AchColOfficialNachaLayout.ProfileVersionMinor
        });

        result.Success.Should().BeTrue(string.Join(" | ", result.Trace));
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileSelected);
        result.Profile!.ProfileCode.Should().Be(AchColOfficialNachaLayout.InboundOriginalProfileCode);
    }

    [Theory]
    [InlineData("ACH", AchColOfficialNachaLayout.TxCodeAwareOutboundPrenotificationProfileCode)]
    [InlineData("CENIT", CenitOrdinaryOutbound2026Layout.CardinalityPrenotificationProfileCode)]
    public async Task PrenotificationProfiles_ShouldBeResolvable(
        string clearingHouseCode,
        string expectedProfileCode)
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);

        var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = clearingHouseCode,
            FlowTypeCode = "PRENOTIFICACION",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords
        });

        result.Success.Should().BeTrue();
        result.UsedFallback.Should().BeFalse();
        result.Profile!.ProfileCode.Should().Be(expectedProfileCode);
        result.LayoutsByRecordCode.Keys.Should().BeEquivalentTo(RequiredRecords);
    }

    [Fact]
    public async Task TxCodeAwareSuccessorCohort_ShouldPublishAcceptedVersionsAndSnapshotBaseline()
    {
        await using var context = await SeedAsync();
        var expected = new[]
        {
            new { Predecessor = AchColOfficialNachaLayout.OutboundOriginalProfileCode, Successor = AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode, Major = 35, Minor = 1, Normative = "V35", Effective = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = AchColOfficialNachaLayout.OutboundPrenotificationProfileCode, Successor = AchColOfficialNachaLayout.TxCodeAwareOutboundPrenotificationProfileCode, Major = 35, Minor = 1, Normative = "V35", Effective = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = CenitOrdinaryOutbound2026Layout.OriginalProfileCode, Successor = CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode, Major = 1, Minor = 2, Normative = "2026-05-07", Effective = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = CenitOrdinaryOutbound2026Layout.PrenotificationProfileCode, Successor = CenitOrdinaryOutbound2026Layout.TxCodeAwarePrenotificationProfileCode, Major = 1, Minor = 2, Normative = "2026-05-07", Effective = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = CenitCtxOutbound2026Layout.OriginalProfileCode, Successor = CenitCtxOutbound2026Layout.TxCodeAwareOriginalProfileCode, Major = 1, Minor = 2, Normative = "2026-05-07", Effective = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = CenitCtxOutbound2026Layout.PrenotificationProfileCode, Successor = CenitCtxOutbound2026Layout.TxCodeAwarePrenotificationProfileCode, Major = 1, Minor = 2, Normative = "2026-05-07", Effective = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc) }
        };

        foreach (var item in expected)
        {
            var predecessor = await LoadProfileAsync(context, item.Predecessor);
            var successor = await LoadProfileAsync(context, item.Successor);
            successor.Should().NotBeNull();
            successor!.VersionMajor.Should().Be(item.Major);
            successor.VersionMinor.Should().Be(item.Minor);
            successor.SupersedesProfileId.Should().Be(predecessor!.Id);
            successor.EffectiveFrom.Should().Be(item.Effective);
            successor.ContextPriority.Should().Be(10);
            successor.Tags.Should().ContainSingle(tag => tag.TagKey == "NormativeVersion" && tag.TagValue == item.Normative);

            var persisted = await context.HistConfigSnapshots.SingleAsync(snapshot =>
                snapshot.ProfileId == successor.Id && snapshot.SnapshotType == "PUBLISH");
            var read = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(persisted.SnapshotJson);
            read.IsSupported.Should().BeTrue(read.Error);
            read.Snapshot!.SnapshotFormatVersion.Should().Be(1);
            var transactionCodes = NachaTransactionCodeSemanticMetadata.Resolve(read.Snapshot.Records);
            transactionCodes.Status.Should().Be(NachaTransactionCodeSemanticMetadataStatus.Resolved);
            transactionCodes.Contract!.Rules.Should().BeEquivalentTo(
                NachaTransactionCodeTaxonomy.GetSupportedOrdinaryRules(),
                options => options.WithStrictOrdering());
        }

        var inbound = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            RequestedVersionMajor = 35,
            RequestedVersionMinor = 0,
            RecordCodes = RequiredRecords
        });
        inbound.Profile!.ProfileCode.Should().Be(AchColOfficialNachaLayout.InboundOriginalProfileCode);
        inbound.TransactionCodeContract.Should().BeNull();
    }

    [Fact]
    public async Task InboundTxCodeSuccessors_ShouldPublishCompleteContractsWithPhysicalAndReaderParity()
    {
        await using var context = await SeedAsync();
        var expected = new[]
        {
            new { Predecessor = AchColOfficialNachaLayout.InboundOriginalProfileCode, Successor = AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode, Major = 35, Minor = 1, Chamber = "ACH", Service = (string?)null, Normative = "V35", Effective = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = AchColOfficialNachaLayout.InboundPrenotificationProfileCode, Successor = AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode, Major = 35, Minor = 1, Chamber = "ACH", Service = (string?)null, Normative = "V35", Effective = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = CenitOrdinaryInbound2026Layout.OriginalProfileCode, Successor = CenitOrdinaryInbound2026Layout.TxCodeAwareOriginalProfileCode, Major = 1, Minor = 1, Chamber = "CENIT", Service = (string?)null, Normative = "2026-05-07", Effective = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = CenitOrdinaryInbound2026Layout.PrenotificationProfileCode, Successor = CenitOrdinaryInbound2026Layout.TxCodeAwarePrenotificationProfileCode, Major = 1, Minor = 1, Chamber = "CENIT", Service = (string?)null, Normative = "2026-05-07", Effective = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = CenitOrdinaryInbound2026Layout.CtxOriginalProfileCode, Successor = CenitOrdinaryInbound2026Layout.TxCodeAwareCtxOriginalProfileCode, Major = 1, Minor = 1, Chamber = "CENIT", Service = (string?)"CTX", Normative = "2026-05-07", Effective = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc) },
            new { Predecessor = CenitOrdinaryInbound2026Layout.CtxPrenotificationProfileCode, Successor = CenitOrdinaryInbound2026Layout.TxCodeAwareCtxPrenotificationProfileCode, Major = 1, Minor = 1, Chamber = "CENIT", Service = (string?)"CTX", Normative = "2026-05-07", Effective = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc) }
        };
        var outboundByChamber = new Dictionary<string, NachaTransactionCodeSemanticContract>();

        foreach (var item in expected)
        {
            var predecessor = (await LoadProfileAsync(context, item.Predecessor))!;
            var successor = (await LoadProfileAsync(context, item.Successor))!;
            predecessor.VersionMajor.Should().Be(item.Major);
            predecessor.VersionMinor.Should().Be(0);
            successor.VersionMajor.Should().Be(item.Major);
            successor.VersionMinor.Should().Be(item.Minor);
            successor.Direction.Code.Should().Be("ENTRADA");
            successor.ClearingHouse.Code.Should().Be(item.Chamber);
            successor.ServiceClass?.Code.Should().Be(item.Service);
            successor.FlowType.Code.Should().Be(predecessor.FlowType.Code);
            successor.Status.Code.Should().Be("PUBLICADO");
            successor.SupersedesProfileId.Should().Be(predecessor.Id);
            successor.EffectiveFrom.Should().Be(predecessor.EffectiveFrom).And.Be(item.Effective);
            successor.ContextPriority.Should().Be(predecessor.ContextPriority).And.Be(10);
            successor.Tags.Should().ContainSingle(tag => tag.TagKey == "NormativeVersion" && tag.TagValue == item.Normative);

            var predecessorJson = await context.HistConfigSnapshots.AsNoTracking()
                .Where(snapshot => snapshot.ProfileId == predecessor.Id && snapshot.SnapshotType == "PUBLISH")
                .Select(snapshot => snapshot.SnapshotJson).SingleAsync();
            var successorJson = await context.HistConfigSnapshots.AsNoTracking()
                .Where(snapshot => snapshot.ProfileId == successor.Id && snapshot.SnapshotType == "PUBLISH")
                .Select(snapshot => snapshot.SnapshotJson).SingleAsync();
            var before = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(predecessorJson).Snapshot!;
            var after = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(successorJson).Snapshot!;
            after.SnapshotFormatVersion.Should().Be(1);
            before.Profile.ClearingHouseCode.Should().Be(after.Profile.ClearingHouseCode);
            before.Profile.FlowTypeCode.Should().Be(after.Profile.FlowTypeCode);
            before.Profile.DirectionCode.Should().Be(after.Profile.DirectionCode);
            before.Profile.ServiceClassCode.Should().Be(after.Profile.ServiceClassCode);
            before.Profile.EffectiveFrom.Should().Be(after.Profile.EffectiveFrom);
            before.Profile.ContextPriority.Should().Be(after.Profile.ContextPriority);
            before.GenerationCriticalTags.Should().BeEquivalentTo(after.GenerationCriticalTags);
            PhysicalSnapshot(after).Should().Be(PhysicalSnapshot(before));
            RecordSnapshot(after).Should().Be(RecordSnapshot(before));

            NachaTransactionCodeSemanticMetadata.Resolve(before.Records).Status
                .Should().Be(NachaTransactionCodeSemanticMetadataStatus.NotPresent);
            var metadata = NachaTransactionCodeSemanticMetadata.Resolve(after.Records);
            metadata.Status.Should().Be(NachaTransactionCodeSemanticMetadataStatus.Resolved);
            metadata.Contract!.Rules.Should().HaveCount(12);
            foreach (var rule in NachaTransactionCodeTaxonomy.GetSupportedOrdinaryRules())
            {
                metadata.Contract.TryGetRule(rule.TransactionCode, out var found).Should().BeTrue();
                found.Should().Be(rule);
            }

            if (!outboundByChamber.TryGetValue(item.Chamber, out var outbound))
            {
                var outboundCode = item.Chamber == "ACH"
                    ? AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode
                    : CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode;
                var outboundProfile = (await LoadProfileAsync(context, outboundCode))!;
                var outboundJson = await context.HistConfigSnapshots.AsNoTracking()
                    .Where(snapshot => snapshot.ProfileId == outboundProfile.Id && snapshot.SnapshotType == "PUBLISH")
                    .Select(snapshot => snapshot.SnapshotJson).SingleAsync();
                var outboundSnapshot = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(outboundJson).Snapshot!;
                outbound = NachaTransactionCodeSemanticMetadata.Resolve(outboundSnapshot.Records).Contract!;
                outboundByChamber.Add(item.Chamber, outbound);
            }
            foreach (var rule in metadata.Contract.Rules)
            {
                outbound.TryGetRule(rule.TransactionCode, out var outboundRule).Should().BeTrue();
                outboundRule.Should().Be(rule);
            }

            var predecessorReader = await NachaProfileRecordReader.LoadAsync(context, predecessor.Id, default);
            var successorReader = await NachaProfileRecordReader.LoadAsync(context, successor.Id, default);
            foreach (var (recordCode, fieldCode, value) in new[]
                     {
                         ("1", "IMMEDIATEORIGIN", "1234567890"),
                         ("5", "STANDARDENTRYCLASSCODE", item.Service ?? "PPD"),
                         ("6", "TRANSACTIONCODE", "23")
                     })
            {
                var field = after.LayoutVariants.Single(variant => variant.RecordCode == recordCode && variant.IsDefaultForRecord)
                    .Fields.Single(candidate => candidate.FieldCode == fieldCode);
                var record = new string(' ', 106).ToCharArray();
                record[0] = recordCode[0];
                value.CopyTo(0, record, field.StartPosition - 1, value.Length);
                predecessorReader.Read(new string(record), recordCode, fieldCode)
                    .Should().Be(successorReader.Read(new string(record), recordCode, fieldCode));
            }
        }

        foreach (var chamber in new[] { "ACH", "CENIT" })
        {
            var selected = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
            {
                ClearingHouseCode = chamber,
                FlowTypeCode = "ORIGINAL",
                DirectionCode = "ENTRADA",
                ProcessDateUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
                RequestedVersionMajor = chamber == "ACH" ? 35 : 1,
                RequestedVersionMinor = 0,
                RecordCodes = RequiredRecords
            });
            selected.Success.Should().BeTrue();
            selected.Profile!.VersionMinor.Should().Be(0);
            selected.TransactionCodeContract.Should().BeNull();
        }
    }

    private static string PhysicalSnapshot(NachaPublicationSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot.LayoutVariants.Select(variant => variant with
        {
            LayoutVariantId = null,
            Fields = variant.Fields.Select(field => field with { FieldDefinitionId = null }).ToArray()
        }).ToArray());

    private static string RecordSnapshot(NachaPublicationSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot.Records.Select(record => record.RecordCode == "6"
            ? record with { SemanticRuleSetId = null, SemanticRuleSet = null }
            : record).ToArray());

    [Fact]
    public async Task InboundTxCodeSuccessors_UpgradeAndPartialRerun_ShouldPreservePredecessorsAndSnapshots()
    {
        await using var context = await SeedAsync();
        var pairs = new[]
        {
            (AchColOfficialNachaLayout.InboundOriginalProfileCode, AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode),
            (AchColOfficialNachaLayout.InboundPrenotificationProfileCode, AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode),
            (CenitOrdinaryInbound2026Layout.OriginalProfileCode, CenitOrdinaryInbound2026Layout.TxCodeAwareOriginalProfileCode),
            (CenitOrdinaryInbound2026Layout.PrenotificationProfileCode, CenitOrdinaryInbound2026Layout.TxCodeAwarePrenotificationProfileCode),
            (CenitOrdinaryInbound2026Layout.CtxOriginalProfileCode, CenitOrdinaryInbound2026Layout.TxCodeAwareCtxOriginalProfileCode),
            (CenitOrdinaryInbound2026Layout.CtxPrenotificationProfileCode, CenitOrdinaryInbound2026Layout.TxCodeAwareCtxPrenotificationProfileCode)
        };
        var predecessorIds = await context.CfgProfiles.AsNoTracking()
            .Where(profile => pairs.Select(pair => pair.Item1).Contains(profile.ProfileCode))
            .ToDictionaryAsync(profile => profile.ProfileCode, profile => profile.Id);
        var predecessorJson = await context.HistConfigSnapshots.AsNoTracking()
            .Where(snapshot => predecessorIds.Values.Contains(snapshot.ProfileId) && snapshot.SnapshotType == "PUBLISH")
            .ToDictionaryAsync(snapshot => snapshot.ProfileId, snapshot => snapshot.SnapshotJson);
        var successorIds = await context.CfgProfiles.AsNoTracking()
            .Where(profile => pairs.Select(pair => pair.Item2).Contains(profile.ProfileCode))
            .Select(profile => profile.Id).ToArrayAsync();
        successorIds.Should().HaveCount(6);
        var cardinalityCodes = new[]
        {
            CenitOrdinaryInbound2026Layout.CardinalityOriginalProfileCode,
            CenitOrdinaryInbound2026Layout.CardinalityPrenotificationProfileCode,
            CenitOrdinaryInbound2026Layout.CardinalityCtxOriginalProfileCode,
            CenitOrdinaryInbound2026Layout.CardinalityCtxPrenotificationProfileCode
        };
        var cardinalityIds = await context.CfgProfiles.AsNoTracking()
            .Where(profile => cardinalityCodes.Contains(profile.ProfileCode))
            .Select(profile => profile.Id).ToArrayAsync();
        await context.HistConfigSnapshots.Where(snapshot => cardinalityIds.Contains(snapshot.ProfileId)).ExecuteDeleteAsync();
        await context.CfgProfiles.Where(profile => cardinalityIds.Contains(profile.Id)).ExecuteDeleteAsync();
        await context.HistConfigSnapshots.Where(snapshot => successorIds.Contains(snapshot.ProfileId)).ExecuteDeleteAsync();
        await context.CfgProfiles.Where(profile => successorIds.Contains(profile.Id)).ExecuteDeleteAsync();
        context.ChangeTracker.Clear();

        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        var published = await context.CfgProfiles.AsNoTracking()
            .Where(profile => pairs.Select(pair => pair.Item2).Contains(profile.ProfileCode))
            .ToDictionaryAsync(profile => profile.ProfileCode, profile => new { profile.Id, profile.SupersedesProfileId });
        published.Should().HaveCount(6);
        foreach (var (predecessorCode, successorCode) in pairs)
        {
            published[successorCode].SupersedesProfileId.Should().Be(predecessorIds[predecessorCode]);
        }
        var successorJson = await context.HistConfigSnapshots.AsNoTracking()
            .Where(snapshot => published.Values.Select(value => value.Id).Contains(snapshot.ProfileId)
                               && snapshot.SnapshotType == "PUBLISH")
            .ToDictionaryAsync(snapshot => snapshot.ProfileId, snapshot => snapshot.SnapshotJson);
        successorJson.Should().HaveCount(6);

        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        var partialId = published[CenitOrdinaryInbound2026Layout.TxCodeAwareCtxPrenotificationProfileCode].Id;
        var dependentId = await context.CfgProfiles.AsNoTracking()
            .Where(profile => profile.ProfileCode == CenitOrdinaryInbound2026Layout.CardinalityCtxPrenotificationProfileCode)
            .Select(profile => profile.Id).SingleAsync();
        await context.HistConfigSnapshots.Where(snapshot => snapshot.ProfileId == dependentId).ExecuteDeleteAsync();
        await context.CfgProfiles.Where(profile => profile.Id == dependentId).ExecuteDeleteAsync();
        await context.HistConfigSnapshots.Where(snapshot => snapshot.ProfileId == partialId).ExecuteDeleteAsync();
        await context.CfgProfiles.Where(profile => profile.Id == partialId).ExecuteDeleteAsync();
        context.ChangeTracker.Clear();
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        foreach (var (predecessorCode, successorCode) in pairs)
        {
            var predecessorId = predecessorIds[predecessorCode];
            var predecessor = await context.CfgProfiles.AsNoTracking().SingleAsync(profile => profile.Id == predecessorId);
            predecessor.VersionMinor.Should().Be(0);
            predecessor.SupersedesProfileId.Should().BeNull();
            (await context.HistConfigSnapshots.AsNoTracking().SingleAsync(snapshot =>
                snapshot.ProfileId == predecessorId && snapshot.SnapshotType == "PUBLISH"))
                .SnapshotJson.Should().Be(predecessorJson[predecessorId]);
            (await context.CfgProfiles.CountAsync(profile => profile.ProfileCode == successorCode)).Should().Be(1);
        }
        foreach (var (id, json) in successorJson)
        {
            if (id == partialId) continue;
            (await context.HistConfigSnapshots.AsNoTracking().SingleAsync(snapshot =>
                snapshot.ProfileId == id && snapshot.SnapshotType == "PUBLISH"))
                .SnapshotJson.Should().Be(json);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InboundTxCodeSuccessorConflict_ShouldFailClosed(bool corruptSnapshot)
    {
        await using var context = await SeedAsync();
        var successor = await context.CfgProfiles.Include(profile => profile.Tags).SingleAsync(profile =>
            profile.ProfileCode == CenitOrdinaryInbound2026Layout.TxCodeAwareCtxOriginalProfileCode);
        if (corruptSnapshot)
        {
            var snapshot = await context.HistConfigSnapshots.SingleAsync(item =>
                item.ProfileId == successor.Id && item.SnapshotType == "PUBLISH");
            snapshot.SnapshotJson += " ";
        }
        else
        {
            successor.Tags.Single(tag => tag.TagKey == "NormativeVersion").TagValue = "INCOMPATIBLE";
        }
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var act = () => new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(corruptSnapshot
                ? "*TXCODE_SUCCESSOR_PUBLISH_SNAPSHOT_CONFLICT*"
                : "*OFFICIAL_PUBLISHED_PROFILE_CONFLICT*");
    }

    [Fact]
    public async Task TxCodeAwareSeederRerun_ShouldNotRewritePredecessorOrSuccessorSnapshots()
    {
        await using var context = await SeedAsync();
        var predecessor = await context.CfgProfiles.AsNoTracking()
            .Where(profile => profile.ProfileCode == CenitOrdinaryOutbound2026Layout.OriginalProfileCode)
            .Select(profile => new { profile.Id, profile.VersionMajor, profile.VersionMinor, profile.EffectiveFrom, profile.ContextPriority, profile.SupersedesProfileId })
            .SingleAsync();
        var predecessorSnapshot = await context.HistConfigSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.ProfileId == predecessor.Id && snapshot.SnapshotType == "PUBLISH")
            .Select(snapshot => snapshot.SnapshotJson)
            .SingleAsync();
        var successor = await context.CfgProfiles.AsNoTracking().SingleAsync(profile =>
            profile.ProfileCode == CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode);
        var successorSnapshot = await context.HistConfigSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.ProfileId == successor.Id && snapshot.SnapshotType == "PUBLISH")
            .Select(snapshot => snapshot.SnapshotJson)
            .SingleAsync();

        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        var predecessorAfter = await context.CfgProfiles.AsNoTracking()
            .Where(profile => profile.Id == predecessor.Id)
            .Select(profile => new { profile.Id, profile.VersionMajor, profile.VersionMinor, profile.EffectiveFrom, profile.ContextPriority, profile.SupersedesProfileId })
            .SingleAsync();
        predecessorAfter.Should().BeEquivalentTo(predecessor);
        (await context.HistConfigSnapshots.AsNoTracking().SingleAsync(snapshot =>
            snapshot.ProfileId == predecessor.Id && snapshot.SnapshotType == "PUBLISH")).SnapshotJson.Should().Be(predecessorSnapshot);
        (await context.HistConfigSnapshots.AsNoTracking().SingleAsync(snapshot =>
            snapshot.ProfileId == successor.Id && snapshot.SnapshotType == "PUBLISH")).SnapshotJson.Should().Be(successorSnapshot);
        (await context.CfgProfiles.CountAsync(profile =>
            profile.ProfileCode == CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode)).Should().Be(1);
    }

    [Fact]
    public async Task HistoricalCenitOrdinaryOneZero_ShouldRemainUnchangedAndUpgradeDirectlyToFixedOneTwo()
    {
        await using var context = await SeedAsync();
        var predecessor = await context.CfgProfiles.SingleAsync(profile =>
            profile.ProfileCode == CenitOrdinaryOutbound2026Layout.OriginalProfileCode);
        var predecessorSnapshot = await context.HistConfigSnapshots.SingleAsync(snapshot =>
            snapshot.ProfileId == predecessor.Id && snapshot.SnapshotType == "PUBLISH");
        var read = NachaPublicationSnapshotSerializer.Read(predecessorSnapshot.SnapshotJson);
        predecessor.VersionMinor = 0;
        predecessorSnapshot.VersionMinor = 0;
        predecessorSnapshot.SnapshotJson = NachaPublicationSnapshotSerializer.Serialize(
            read.Snapshot! with { Profile = read.Snapshot.Profile with { VersionMinor = 0 } });
        var historicalSnapshot = predecessorSnapshot.SnapshotJson;

        var oldSuccessorId = await context.CfgProfiles.Where(profile =>
                profile.ProfileCode == CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode)
            .Select(profile => profile.Id)
            .SingleAsync();
        var cardinalitySuccessorId = await context.CfgProfiles.Where(profile =>
                profile.ProfileCode == CenitOrdinaryOutbound2026Layout.CardinalityOriginalProfileCode)
            .Select(profile => profile.Id)
            .SingleAsync();
        await context.HistConfigSnapshots.Where(snapshot => snapshot.ProfileId == cardinalitySuccessorId).ExecuteDeleteAsync();
        await context.CfgProfiles.Where(profile => profile.Id == cardinalitySuccessorId).ExecuteDeleteAsync();
        await context.HistConfigSnapshots.Where(snapshot => snapshot.ProfileId == oldSuccessorId).ExecuteDeleteAsync();
        await context.CfgProfiles.Where(profile => profile.Id == oldSuccessorId).ExecuteDeleteAsync();
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();

        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        var preserved = await context.CfgProfiles.AsNoTracking().SingleAsync(profile => profile.Id == predecessor.Id);
        preserved.VersionMinor.Should().Be(0);
        (await context.HistConfigSnapshots.AsNoTracking().SingleAsync(snapshot => snapshot.Id == predecessorSnapshot.Id))
            .SnapshotJson.Should().Be(historicalSnapshot);
        var successor = await context.CfgProfiles.AsNoTracking().SingleAsync(profile =>
            profile.ProfileCode == CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode);
        successor.VersionMinor.Should().Be(2);
        successor.SupersedesProfileId.Should().Be(predecessor.Id);
        (await context.CfgProfiles.CountAsync(profile =>
            profile.ClearingHouseId == predecessor.ClearingHouseId
            && profile.FlowTypeId == predecessor.FlowTypeId
            && profile.DirectionId == predecessor.DirectionId
            && profile.ServiceClassId == null
            && profile.VersionMajor == 1
            && profile.VersionMinor == 1)).Should().Be(0);
    }

    [Fact]
    public async Task ConflictingPublishedSuccessor_ShouldFailClosed()
    {
        await using var context = await SeedAsync();
        var successor = await context.CfgProfiles.Include(profile => profile.Tags).SingleAsync(profile =>
            profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        successor.Tags.Single(tag => tag.TagKey == "NormativeVersion").TagValue = "INCOMPATIBLE";
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var act = () => new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OFFICIAL_PUBLISHED_PROFILE_CONFLICT*");
    }

    [Fact]
    public async Task ConflictingPublishedSuccessorSnapshot_ShouldFailClosed()
    {
        await using var context = await SeedAsync();
        var successor = await context.CfgProfiles.SingleAsync(profile =>
            profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        var snapshot = await context.HistConfigSnapshots.SingleAsync(item =>
            item.ProfileId == successor.Id && item.SnapshotType == "PUBLISH");
        snapshot.SnapshotJson += " ";
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var act = () => new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TXCODE_SUCCESSOR_PUBLISH_SNAPSHOT_CONFLICT*");
    }

    [Theory]
    [InlineData("22", TransactionTypeEnum.Credit, false, null)]
    [InlineData("99", TransactionTypeEnum.Credit, false, "HISTORICAL_ORDINARY_TXCODE_UNKNOWN")]
    [InlineData("27", TransactionTypeEnum.Credit, false, "HISTORICAL_ORDINARY_TXCODE_DIRECTION_MISMATCH")]
    [InlineData("23", TransactionTypeEnum.Credit, false, "HISTORICAL_ORDINARY_TXCODE_PRENOTE_MISMATCH")]
    public async Task HistoricalOrdinaryTransactionAudit_ShouldFailClosed(
        string transactionCode,
        TransactionTypeEnum transactionType,
        bool isPrenotification,
        string? expectedError)
    {
        await using var context = await SeedAsync();
        var chamber = new ClearingHouse
        {
            Code = RegulatoryCycleScheduleCatalog.AchColombiaCode,
            Name = "ACH Colombia",
            OriginCode = "000101006"
        };
        context.ClearingHouses.Add(chamber);
        var cycle = new AchCycle
        {
            Id = $"TXCODE-AUDIT-{Guid.NewGuid():N}",
            CycleName = "TXCODE audit",
            ProcessingDate = new DateTime(2026, 8, 24),
            ClearingHouse = chamber
        };
        var batch = new AchBatch
        {
            AchCycle = cycle,
            CompanyName = "TEST",
            CompanyIdentification = "TEST",
            OriginOrOdfi = "00000000",
            EffectiveEntryDate = cycle.ProcessingDate
        };
        context.AchTransactions.Add(new AchTransaction
        {
            TransactionExternalId = Guid.NewGuid().ToString("N"),
            Reference = "TXCODE-AUDIT",
            Type = transactionType,
            TransactionCode = transactionCode,
            IsPrenotification = isPrenotification,
            Direction = AchTransactionDirection.Outgoing,
            State = AchTransferStateEnum.Pending,
            SourceAccountNumber = "TEST-SOURCE",
            DestinationAccountNumber = "TEST-DESTINATION",
            AchCycle = cycle,
            AchBatch = batch,
            EffectiveEntryDate = cycle.ProcessingDate
        });
        await context.SaveChangesAsync();

        var act = () => new NachaConfigOfficialProfilesSeeder(context).SeedAsync();

        if (expectedError is null)
        {
            await act.Should().NotThrowAsync();
        }
        else
        {
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{expectedError}*");
        }
    }

    [Fact]
    public async Task PublicationValidation_ShouldRejectHistoricalCodeReassignmentWithinChamber()
    {
        await using var context = await SeedAsync();
        var successor = await LoadProfileAsync(context, AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        var ruleSet = successor!.Records.Single(record => record.RecordCode.Code == "6").SemanticRuleSet!;
        ruleSet.Rules.Single(rule => rule.RuleCode == "TRANSACTION_CODE_22").RuleConfigJson =
            "{\"transactionCode\":\"22\",\"direction\":\"CREDIT\",\"accountType\":\"Savings\",\"isPrenotification\":false}";
        ruleSet.Rules.Single(rule => rule.RuleCode == "TRANSACTION_CODE_32").RuleConfigJson =
            "{\"transactionCode\":\"32\",\"direction\":\"CREDIT\",\"accountType\":\"Checking\",\"isPrenotification\":false}";
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var result = await new NachaConfigValidationService(context).ValidateBeforePublishAsync(successor.Id);

        result.Issues.Should().Contain(issue => issue.Codigo == "HISTORICAL_TRANSACTION_CODE_REASSIGNMENT");
    }

    [Fact]
    public async Task PublicationValidation_ShouldAllowSameMeaningAndKeepChamberHistoryIndependent()
    {
        await using var context = await SeedAsync();
        var achProfileId = await context.CfgProfiles.Where(profile =>
                profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode)
            .Select(profile => profile.Id)
            .SingleAsync();
        var achSnapshot = await context.HistConfigSnapshots.SingleAsync(snapshot =>
            snapshot.ProfileId == achProfileId && snapshot.SnapshotType == "PUBLISH");
        var read = NachaPublicationSnapshotSerializer.Read(achSnapshot.SnapshotJson).Snapshot!;
        var changedRecords = read.Records.Select(record =>
        {
            if (record.RecordCode != "6")
            {
                return record;
            }

            var changedRules = record.SemanticRuleSet!.Rules.Select(rule => rule.RuleCode switch
            {
                "TRANSACTION_CODE_22" => rule with
                {
                    RuleConfiguration = JsonDocument.Parse("{\"transactionCode\":\"22\",\"direction\":\"CREDIT\",\"accountType\":\"Savings\",\"isPrenotification\":false}").RootElement.Clone()
                },
                "TRANSACTION_CODE_32" => rule with
                {
                    RuleConfiguration = JsonDocument.Parse("{\"transactionCode\":\"32\",\"direction\":\"CREDIT\",\"accountType\":\"Checking\",\"isPrenotification\":false}").RootElement.Clone()
                },
                _ => rule
            }).ToArray();
            return record with { SemanticRuleSet = record.SemanticRuleSet with { Rules = changedRules } };
        }).ToArray();
        achSnapshot.SnapshotJson = NachaPublicationSnapshotSerializer.Serialize(read with { Records = changedRecords });
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var cenit = await context.CfgProfiles.SingleAsync(profile =>
            profile.ProfileCode == CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode);
        var result = await new NachaConfigValidationService(context).ValidateBeforePublishAsync(cenit.Id);

        result.Issues.Should().NotContain(issue => issue.Codigo == "HISTORICAL_TRANSACTION_CODE_REASSIGNMENT");
    }

    [Fact]
    public async Task PublishedInboundPreselection_ShouldSelectAllAcceptedSuccessorsAndIgnoreLiveMutation()
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);
        var cases = new[]
        {
            ("ACH", "PPD", "22", AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode),
            ("ACH", "PPD", "27", AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode),
            ("ACH", "PPD", "23", AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode),
            ("ACH", "PPD", "28", AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode),
            ("ACH", "PPD", "32", AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode),
            ("ACH", "PPD", "37", AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode),
            ("ACH", "PPD", "33", AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode),
            ("ACH", "PPD", "38", AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode),
            ("ACH", "PPD", "52", AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode),
            ("ACH", "PPD", "55", AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode),
            ("ACH", "PPD", "53", AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode),
            ("ACH", "PPD", "57", AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode),
            ("CENIT", "PPD", "22", CenitOrdinaryInbound2026Layout.CardinalityOriginalProfileCode),
            ("CENIT", "CCD", "27", CenitOrdinaryInbound2026Layout.CardinalityOriginalProfileCode),
            ("CENIT", "CCD", "23", CenitOrdinaryInbound2026Layout.CardinalityPrenotificationProfileCode),
            ("CENIT", "PPD", "28", CenitOrdinaryInbound2026Layout.CardinalityPrenotificationProfileCode),
            ("CENIT", "CCD", "32", CenitOrdinaryInbound2026Layout.CardinalityOriginalProfileCode),
            ("CENIT", "PPD", "37", CenitOrdinaryInbound2026Layout.CardinalityOriginalProfileCode),
            ("CENIT", "PPD", "33", CenitOrdinaryInbound2026Layout.CardinalityPrenotificationProfileCode),
            ("CENIT", "CCD", "38", CenitOrdinaryInbound2026Layout.CardinalityPrenotificationProfileCode),
            ("CENIT", "PPD", "52", CenitOrdinaryInbound2026Layout.CardinalityOriginalProfileCode),
            ("CENIT", "CCD", "55", CenitOrdinaryInbound2026Layout.CardinalityOriginalProfileCode),
            ("CENIT", "CCD", "53", CenitOrdinaryInbound2026Layout.CardinalityPrenotificationProfileCode),
            ("CENIT", "PPD", "57", CenitOrdinaryInbound2026Layout.CardinalityPrenotificationProfileCode),
            ("CENIT", "CTX", "22", CenitOrdinaryInbound2026Layout.CardinalityCtxOriginalProfileCode),
            ("CENIT", "CTX", "27", CenitOrdinaryInbound2026Layout.CardinalityCtxOriginalProfileCode),
            ("CENIT", "CTX", "23", CenitOrdinaryInbound2026Layout.CardinalityCtxPrenotificationProfileCode),
            ("CENIT", "CTX", "28", CenitOrdinaryInbound2026Layout.CardinalityCtxPrenotificationProfileCode),
            ("CENIT", "CTX", "32", CenitOrdinaryInbound2026Layout.CardinalityCtxOriginalProfileCode),
            ("CENIT", "CTX", "37", CenitOrdinaryInbound2026Layout.CardinalityCtxOriginalProfileCode),
            ("CENIT", "CTX", "33", CenitOrdinaryInbound2026Layout.CardinalityCtxPrenotificationProfileCode),
            ("CENIT", "CTX", "38", CenitOrdinaryInbound2026Layout.CardinalityCtxPrenotificationProfileCode),
            ("CENIT", "CTX", "52", CenitOrdinaryInbound2026Layout.CardinalityCtxOriginalProfileCode),
            ("CENIT", "CTX", "55", CenitOrdinaryInbound2026Layout.CardinalityCtxOriginalProfileCode),
            ("CENIT", "CTX", "53", CenitOrdinaryInbound2026Layout.CardinalityCtxPrenotificationProfileCode),
            ("CENIT", "CTX", "57", CenitOrdinaryInbound2026Layout.CardinalityCtxPrenotificationProfileCode)
        };
        foreach (var (chamber, service, code, expected) in cases)
        {
            var result = await resolver.ResolvePublishedInboundAsync(
                InboundRequest(chamber), InboundEvidence(service, code));
            result.Success.Should().BeTrue(string.Join("; ", result.Warnings));
            result.Profile!.ProfileCode.Should().Be(expected);
            result.TransactionCodeContract!.TryGetRule(code, out var rule).Should().BeTrue();
            rule.IsPrenotification.Should().Be(code is "23" or "28" or "33" or "38" or "53" or "57");
        }

        var liveRule = await context.CfgRuleSetRules.SingleAsync(rule =>
            rule.RuleSet.RuleSetCode == "NACHA_ACH_TRANSACTION_CODE_V1"
            && rule.RuleCode == "TRANSACTION_CODE_22");
        liveRule.RuleConfigJson = "{\"transactionCode\":\"99\",\"direction\":\"CREDIT\",\"accountType\":\"Checking\",\"isPrenotification\":false}";
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();
        var after = await resolver.ResolvePublishedInboundAsync(InboundRequest("ACH"), InboundEvidence("PPD", "22"));
        after.Success.Should().BeTrue(string.Join("; ", after.Warnings));
        after.Profile!.ProfileCode.Should().Be(AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode);
        after.TransactionCodeContract!.TryGetRule("22", out var unchanged).Should().BeTrue();
        unchanged.AccountType.Should().Be(AccountTypeEnum.Checking);

        var liveField = await context.CfgLayoutFields.SingleAsync(field =>
            field.LayoutVariant.ProfileId == after.Profile.Id
            && field.LayoutVariant.RecordCode.Code == "6"
            && field.FieldCode == "TRANSACTIONCODE");
        liveField.StartPosition = 3;
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();
        var publishedReader = await NachaProfileRecordReader.LoadPublishedAsync(context, after.Profile.Id, default);
        publishedReader.Read(InboundEvidence("PPD", "22")[1], "6", "TRANSACTIONCODE").Should().Be("22");
    }

    [Fact]
    public async Task PublishedInboundPreselection_ShouldFollowPublicationAcrossMajorAndMinorVersions()
    {
        await using var context = await SeedAsync();
        var resolver = new NachaConfigResolver(context);
        var evidence = InboundEvidence("PPD", "22");
        var before = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
        var effective = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        NachaConfigResolutionRequest Request(DateTime date, int? major = null, int? minor = null) => new()
        {
            ClearingHouseCode = "ACH",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = date,
            RequestedVersionMajor = major,
            RequestedVersionMinor = minor,
            RecordCodes = ["5", "6"]
        };

        var current = await resolver.ResolvePublishedInboundAsync(Request(before), evidence);
        current.Success.Should().BeTrue();
        current.Profile!.ProfileCode.Should().Be(AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode);

        var major = await AddSyntheticInboundSuccessorAsync(context, "TEST_INBOUND_MAJOR_SUCCESSOR", 36, 0,
            effective, "BORRADOR", false);
        (await resolver.ResolvePublishedInboundAsync(Request(effective), evidence)).Profile!.Id.Should().Be(current.Profile.Id);

        major.StatusId = await context.CatConfigStatuses.Where(status => status.Code == "PUBLICADO")
            .Select(status => status.Id).SingleAsync();
        await context.SaveChangesAsync();
        await AddSyntheticInboundSnapshotAsync(context, current.Profile.Id, major);
        (await resolver.ResolvePublishedInboundAsync(Request(before), evidence)).Profile!.Id.Should().Be(current.Profile.Id);
        (await resolver.ResolvePublishedInboundAsync(Request(effective), evidence)).Profile!.Id.Should().Be(major.Id);

        major.ContextPriority = current.Profile.ContextPriority + 1;
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        (await resolver.ResolvePublishedInboundAsync(Request(effective), evidence)).Profile!.Id.Should().Be(current.Profile.Id);
        major = await context.CfgProfiles.SingleAsync(profile => profile.Id == major.Id);
        major.ContextPriority = current.Profile.ContextPriority;
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var minor = await AddSyntheticInboundSuccessorAsync(context, "TEST_INBOUND_MINOR_SUCCESSOR", 36, 1,
            effective, "PUBLICADO", true, current.Profile.Id);
        (await resolver.ResolvePublishedInboundAsync(Request(effective), evidence)).Profile!.Id.Should().Be(minor.Id);
        (await resolver.ResolvePublishedInboundAsync(Request(effective, 36), evidence)).Profile!.Id.Should().Be(minor.Id);
        (await resolver.ResolvePublishedInboundAsync(Request(effective, 36, 0), evidence)).Profile!.Id.Should().Be(major.Id);
        (await resolver.ResolvePublishedInboundAsync(Request(effective, 35, 1), evidence)).Profile!.Id.Should().Be(current.Profile.Id);
        (await resolver.ResolvePublishedInboundAsync(Request(effective, 37, 0), evidence)).Success.Should().BeFalse();
        (await resolver.ResolvePublishedInboundAsync(Request(effective, null, 1), evidence))
            .SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileVersionUnsupported);

        minor.EffectiveTo = effective;
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        (await resolver.ResolvePublishedInboundAsync(Request(effective.AddDays(1)), evidence)).Profile!.Id.Should().Be(major.Id);
    }

    [Theory]
    [InlineData("missing-snapshot", NachaProfileSelectionStatus.SemanticContractMissing, "INBOUND_PUBLISH_SNAPSHOT_MISSING")]
    [InlineData("missing-metadata", NachaProfileSelectionStatus.SemanticContractMissing, "INBOUND_TXCODE_METADATA_INVALID")]
    [InlineData("malformed-snapshot", NachaProfileSelectionStatus.SemanticContractInvalid, "INBOUND_PUBLISH_SNAPSHOT_INVALID")]
    [InlineData("unsupported-code", NachaProfileSelectionStatus.ProfileNotFound, "INBOUND_TXCODE_UNSUPPORTED")]
    [InlineData("ambiguous", NachaProfileSelectionStatus.ProfileAmbiguous, "INBOUND_PUBLICATION_AMBIGUOUS")]
    [InlineData("live-only", NachaProfileSelectionStatus.ProfileNotFound, "INBOUND_PUBLICATION_NO_COMPATIBLE_AUTHORITY")]
    [InlineData("conflicting", NachaProfileSelectionStatus.ProfileAmbiguous, "INBOUND_TXCODE_AUTHORITY_CONFLICT")]
    public async Task PublishedInboundPreselection_ShouldFailClosed(
        string mode,
        NachaProfileSelectionStatus expectedStatus,
        string expectedCode)
    {
        await using var context = await SeedAsync();
        var profile = (await LoadProfileAsync(context, AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode))!;
        var publication = await context.HistConfigSnapshots.SingleAsync(row =>
            row.ProfileId == profile.Id && row.SnapshotType == "PUBLISH");
        switch (mode)
        {
            case "missing-snapshot":
                context.HistConfigSnapshots.Remove(publication);
                break;
            case "missing-metadata":
                var snapshot = NachaPublicationSnapshotSerializer.Read(publication.SnapshotJson).Snapshot!;
                publication.SnapshotJson = NachaPublicationSnapshotSerializer.Serialize(snapshot with
                {
                    Records = snapshot.Records.Select(record => record.RecordCode == "6"
                        ? record with { SemanticRuleSetId = null, SemanticRuleSet = null }
                        : record).ToArray()
                });
                break;
            case "malformed-snapshot":
                publication.SnapshotJson = "{";
                break;
            case "ambiguous":
                context.CfgProfiles.Add(new CfgProfile
                {
                    ProfileCode = "TEST_DUPLICATE_INBOUND_PUBLICATION",
                    NameEs = "Ambiguous inbound test authority",
                    ClearingHouseId = profile.ClearingHouseId,
                    FlowTypeId = profile.FlowTypeId,
                    DirectionId = profile.DirectionId,
                    ServiceClassId = profile.ServiceClassId,
                    ContextPriority = profile.ContextPriority,
                    EffectiveFrom = profile.EffectiveFrom,
                    StatusId = profile.StatusId,
                    VersionMajor = profile.VersionMajor,
                    VersionMinor = profile.VersionMinor
                });
                break;
            case "live-only":
                var draftId = await context.CatConfigStatuses.Where(status => status.Code == "BORRADOR")
                    .Select(status => status.Id).SingleAsync();
                var originals = await context.CfgProfiles.Where(candidate =>
                    candidate.ClearingHouse.Code == "ACH"
                    && candidate.Direction.Code == "ENTRADA"
                    && candidate.FlowType.Code == "ORIGINAL").ToListAsync();
                foreach (var original in originals)
                {
                    original.StatusId = draftId;
                }
                break;
            case "conflicting":
                var ctx = (await LoadProfileAsync(context, CenitOrdinaryInbound2026Layout.CardinalityCtxOriginalProfileCode))!;
                var ctxPublication = await context.HistConfigSnapshots.SingleAsync(row =>
                    row.ProfileId == ctx.Id && row.SnapshotType == "PUBLISH");
                var ctxSnapshot = NachaPublicationSnapshotSerializer.Read(ctxPublication.SnapshotJson).Snapshot!;
                ctxPublication.SnapshotJson = NachaPublicationSnapshotSerializer.Serialize(ctxSnapshot with
                {
                    Records = ctxSnapshot.Records.Select(record => record.RecordCode == "6"
                        ? record with
                        {
                            SemanticRuleSet = record.SemanticRuleSet! with
                            {
                                Rules = record.SemanticRuleSet.Rules.Select(rule => rule.RuleCode switch
                                {
                                    "TRANSACTION_CODE_22" => rule with
                                    {
                                        RuleConfiguration = JsonDocument.Parse("{\"transactionCode\":\"22\",\"direction\":\"Credit\",\"accountType\":\"Savings\",\"isPrenotification\":false}").RootElement.Clone()
                                    },
                                    "TRANSACTION_CODE_32" => rule with
                                    {
                                        RuleConfiguration = JsonDocument.Parse("{\"transactionCode\":\"32\",\"direction\":\"Credit\",\"accountType\":\"Checking\",\"isPrenotification\":false}").RootElement.Clone()
                                    },
                                    _ => rule
                                }).ToArray()
                            }
                        }
                        : record).ToArray()
                });
                break;
        }
        if (mode is "ambiguous" or "unsupported-code")
            await context.SaveChangesAsync();
        else
            await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);
        context.ChangeTracker.Clear();

        var chamber = mode == "conflicting" ? "CENIT" : "ACH";
        var result = await new NachaConfigResolver(context).ResolvePublishedInboundAsync(
            InboundRequest(chamber), InboundEvidence("PPD", mode == "unsupported-code" ? "99" : "22"));

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(expectedStatus);
        result.Warnings.Should().ContainSingle();
        result.Warnings[0].Should().StartWith(expectedCode);
    }

    private static NachaConfigResolutionRequest InboundRequest(string chamber)
        => new()
        {
            ClearingHouseCode = chamber,
            DirectionCode = "ENTRADA",
            ProcessDateUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = ["5", "6"]
        };

    private static async Task<CfgProfile> AddSyntheticInboundSuccessorAsync(
        AchDbContext context, string code, int major, int minor, DateTime effective,
        string status, bool withSnapshot, int? sourceProfileId = null)
    {
        var source = await context.CfgProfiles.AsNoTracking().SingleAsync(profile =>
            profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode);
        var profile = new CfgProfile
        {
            Id = await context.CfgProfiles.MaxAsync(profile => profile.Id) + 1,
            ProfileCode = code,
            NameEs = code,
            ClearingHouseId = source.ClearingHouseId,
            FlowTypeId = source.FlowTypeId,
            DirectionId = source.DirectionId,
            ServiceClassId = source.ServiceClassId,
            ContextPriority = source.ContextPriority,
            EffectiveFrom = effective,
            StatusId = await context.CatConfigStatuses.Where(item => item.Code == status)
                .Select(item => item.Id).SingleAsync(),
            VersionMajor = major,
            VersionMinor = minor,
            RowVersion = [1]
        };
        context.CfgProfiles.Add(profile);
        await context.SaveChangesAsync();
        if (withSnapshot)
        {
            await AddSyntheticInboundSnapshotAsync(context, sourceProfileId ?? source.Id, profile);
        }
        return profile;
    }

    private static async Task AddSyntheticInboundSnapshotAsync(AchDbContext context, int sourceProfileId, CfgProfile target)
    {
        var json = await context.HistConfigSnapshots.AsNoTracking()
            .Where(item => item.ProfileId == sourceProfileId && item.SnapshotType == "PUBLISH")
            .Select(item => item.SnapshotJson).SingleAsync();
        var snapshot = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(json).Snapshot!;
        var copy = snapshot with
        {
            Profile = snapshot.Profile with
            {
                ProfileId = target.Id,
                ProfileCode = target.ProfileCode,
                VersionMajor = target.VersionMajor,
                VersionMinor = target.VersionMinor,
                ContextPriority = target.ContextPriority,
                EffectiveFrom = target.EffectiveFrom
            }
        };
        context.HistConfigSnapshots.Add(new HistConfigSnapshot
        {
            Id = await context.HistConfigSnapshots.MaxAsync(item => item.Id) + 1,
            ProfileId = target.Id,
            VersionMajor = target.VersionMajor,
            VersionMinor = target.VersionMinor,
            SnapshotType = "PUBLISH",
            SnapshotJson = NachaPublicationSnapshotSerializer.Serialize(copy),
            CreatedAtUtc = target.EffectiveFrom,
            CreatedBy = "test"
        });
        await context.SaveChangesAsync();
    }

    private static IReadOnlyList<string> InboundEvidence(string service, string transactionCode)
    {
        var batch = new string(' ', 106).ToCharArray();
        batch[0] = '5';
        service.CopyTo(0, batch, 50, service.Length);
        var entry = new string(' ', 106).ToCharArray();
        entry[0] = '6';
        transactionCode.CopyTo(0, entry, 1, transactionCode.Length);
        return [new string(batch), new string(entry)];
    }

    private Task<AchDbContext> SeedAsync() => _fixture.CreateSeededContextAsync();

    private sealed class AlwaysValidNachaConfigValidationService : INachaConfigValidationService
    {
        public Task<NachaConfigValidationResultDto> ValidateBeforePublishAsync(int profileId, CancellationToken ct = default)
            => Task.FromResult(new NachaConfigValidationResultDto
            {
                ProfileId = profileId,
                IsValid = true,
                Resumen = "OK"
            });
    }

    private static Task<NachaConfigResolutionResult> ResolveCtxProfileAsync(AchDbContext context)
        => new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "CTX",
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords
        });

    private static Task<NachaConfigResolutionResult> ResolveCenitInboundAsync(
        AchDbContext context,
        string flowType,
        string? serviceClass)
        => new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = flowType,
            DirectionCode = "ENTRADA",
            ServiceClassCode = serviceClass,
            RequestedVersionMajor = 1,
            RequestedVersionMinor = 0,
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = RequiredRecords
        });

    private static async Task<List<CfgProfile>> LoadOfficialProfilesAsync(AchDbContext context)
    {
        return await context.CfgProfiles
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .Include(x => x.Status)
            .Include(x => x.Tags)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.Records)
                .ThenInclude(x => x.SemanticRuleSet)
                    .ThenInclude(x => x!.Rules)
                        .ThenInclude(x => x.RuleType)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(x => x.SourceDefinition)
                        .ThenInclude(x => x.DataSourceType)
            .Where(x => x.ProfileCode.StartsWith("OFFICIAL_"))
            .ToListAsync();
    }

    private static async Task<CfgProfile?> LoadProfileAsync(AchDbContext context, string profileCode)
    {
        return await context.CfgProfiles
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .Include(x => x.Status)
            .Include(x => x.Tags)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.Records)
                .ThenInclude(x => x.SemanticRuleSet)
                    .ThenInclude(x => x!.Rules)
                        .ThenInclude(x => x.RuleType)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(x => x.SourceDefinition)
                        .ThenInclude(x => x.DataSourceType)
            .FirstOrDefaultAsync(x => x.ProfileCode == profileCode);
    }

    private static async Task<(int Profiles, int Records, int Variants, int Fields)> SnapshotProfileCardinalityAsync(
        AchDbContext context,
        string profileCode)
    {
        var profileId = await context.CfgProfiles
            .Where(profile => profile.ProfileCode == profileCode)
            .Select(profile => profile.Id)
            .SingleAsync();
        return (
            await context.CfgProfiles.CountAsync(profile => profile.Id == profileId),
            await context.CfgProfileRecords.CountAsync(record => record.ProfileId == profileId),
            await context.CfgLayoutVariants.CountAsync(variant => variant.ProfileId == profileId),
            await context.CfgLayoutFields.CountAsync(field => field.LayoutVariant.ProfileId == profileId));
    }

    private static void AssertFieldsForAllRecords(CfgProfile profile)
    {
        profile.LayoutVariants.Select(x => x.RecordCode.Code).Distinct().Should().BeEquivalentTo(RequiredRecords);
        profile.LayoutVariants.Should().OnlyContain(x => x.StatusId == profile.StatusId);
        foreach (var recordCode in RequiredRecords)
        {
            var variants = profile.LayoutVariants.Where(x => x.RecordCode.Code == recordCode).ToList();
            variants.Should().NotBeEmpty($"record {recordCode} must have at least one variant");
            variants.Should().OnlyContain(variant => variant.Fields.Count > 0);
            variants.SelectMany(variant => variant.Fields).Should().OnlyContain(field => field.IsEnabled);
        }

        if (profile.ClearingHouse.Code == "ACH")
        {
            var expectedType7Variants = profile.FlowType.Code == "ORIGINAL"
                ? new[]
                {
                    AchColOfficialNachaLayout.Type7CreditMonetaryVariant,
                    AchColOfficialNachaLayout.Type7CreditPrenotificationVariant,
                    AchColOfficialNachaLayout.Type7DebitVariant
                }
                :
                [
                    AchColOfficialNachaLayout.Type7CreditPrenotificationVariant,
                    AchColOfficialNachaLayout.Type7DebitVariant
                ];
            profile.LayoutVariants.Where(variant => variant.RecordCode.Code == "7").Select(variant => variant.VariantCode)
                .Should().BeEquivalentTo(expectedType7Variants);
        }
    }

    private static string? Constant(CfgProfile profile, string recordCode, string fieldCode)
        => profile.LayoutVariants.Single(variant => variant.RecordCode.Code == recordCode && variant.IsDefaultForRecord)
            .Fields.Single(field => field.FieldCode == fieldCode)
            .SourceDefinition.ConstantValue;

    private static void AssertField(CfgProfile profile, string recordCode, string fieldCode, int start, int length)
    {
        var field = profile.LayoutVariants
            .Single(variant => variant.RecordCode.Code == recordCode && variant.IsDefaultForRecord)
            .Fields.Single(candidate => candidate.FieldCode == fieldCode);
        field.StartPosition.Should().Be(start);
        field.Length.Should().Be(length);
    }
}
