using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
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
        var profile = await LoadProfileAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0");

        profile.Should().NotBeNull();
        profile!.ClearingHouse.Code.Should().Be("ACH");
        profile.Status.Code.Should().Be("PUBLICADO");
        profile.EffectiveFrom.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        profile.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public async Task NachaConfigSeeds_ShouldCreatePublishedCenitProfile()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");

        profile.Should().NotBeNull();
        profile!.ClearingHouse.Code.Should().Be("CENIT");
        profile.Status.Code.Should().Be("PUBLICADO");
        profile.EffectiveFrom.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        profile.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public async Task AchColombiaAndCenitProfiles_ShouldBeIndependent()
    {
        await using var context = await SeedAsync();
        var ach = await LoadProfileAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0");
        var cenit = await LoadProfileAsync(context, "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");

        ach!.Id.Should().NotBe(cenit!.Id);
        ach.ClearingHouseId.Should().NotBe(cenit.ClearingHouseId);
        ach.LayoutVariants.Select(x => x.Id).Should().NotIntersectWith(cenit.LayoutVariants.Select(x => x.Id));
        ach.LayoutVariants.SelectMany(x => x.Fields).Select(x => x.Id)
            .Should().NotIntersectWith(cenit.LayoutVariants.SelectMany(x => x.Fields).Select(x => x.Id));
    }

    [Fact]
    public async Task AchColombiaProfile_ShouldContainRecords_1_5_6_7_8_9()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0");

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
        var profile = await LoadProfileAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0");

        AssertFieldsForAllRecords(profile!);
    }

    [Fact]
    public async Task CenitProfile_ShouldContainLayoutFieldsForAllRecords()
    {
        await using var context = await SeedAsync();
        var profile = await LoadProfileAsync(context, "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");

        AssertFieldsForAllRecords(profile!);
    }

    [Fact]
    public async Task PublishedProfiles_ShouldHaveEffectiveDates()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        profiles.Should().OnlyContain(x => x.PublishedAt.HasValue);
        profiles.Should().OnlyContain(x => x.EffectiveFrom == new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        profiles.Should().OnlyContain(x => x.EffectiveTo == null);
    }

    [Fact]
    public async Task PublishedProfiles_ShouldHaveNormativeSource()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        profiles.Should().OnlyContain(x => x.Tags.Any(t => t.TagKey == "NormativeSource" && !string.IsNullOrWhiteSpace(t.TagValue)));
        profiles.Where(x => x.ClearingHouse.Code == "ACH"
                            && !x.Tags.Any(t => t.TagKey == "NormativeVersion" && t.TagValue == "V35"))
            .Should().OnlyContain(x => x.Tags.Any(t => t.TagValue == "MAN-004 V32"));
        profiles.Single(x => x.ProfileCode == "OFFICIAL_ACH_SALIDA_DEVOLUCION_V35_1_0").Tags
            .Should().Contain(t => t.TagKey == "NormativeVersion" && t.TagValue == "V35")
            .And.Contain(t => t.TagKey == "NormativeSource" && t.TagValue.Contains("sección 6.6"));
        profiles.Where(x => x.ClearingHouse.Code == "CENIT"
                            && x.ProfileCode != CenitReturnIn2026Layout.ProfileCode
                            && x.ProfileCode != CenitReturnOut2026Layout.ProfileCode)
            .Should().OnlyContain(x => x.Tags.Any(t => t.TagValue.Contains("CENIT/DSP-152")));
        profiles.Single(x => x.ProfileCode == CenitReturnIn2026Layout.ProfileCode).Tags
            .Should().Contain(t => t.TagKey == "NormativeVersion" && t.TagValue == "2026-05-07")
            .And.Contain(t => t.TagKey == "NormativeSource" && t.TagValue.Contains("Formato NACHA-M CENIT"));
        profiles.Single(x => x.ProfileCode == CenitReturnOut2026Layout.ProfileCode).Tags
            .Should().Contain(t => t.TagKey == "NormativeVersion" && t.TagValue == CenitReturnOut2026Layout.NormativeVersion)
            .And.Contain(t => t.TagKey == "NormativeSource" && t.TagValue.Contains("Formato NACHA-M CENIT"));
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
        result.Profile!.ProfileCode.Should().Be("OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0");
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
        result.Profile!.ProfileCode.Should().Be("OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");
        result.LayoutsByRecordCode.Keys.Should().BeEquivalentTo(RequiredRecords);
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
    public async Task NonReturnDimensions_ShouldNotSelectIncomingReturnProfile()
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
            RequireHomologated = true
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileNotFound);
    }

    [Theory]
    [InlineData("ACH", "OFFICIAL_ACH_SALIDA_PRENOTIFICACION_V1_0")]
    [InlineData("CENIT", "OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_0")]
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

    private Task<AchDbContext> SeedAsync() => _fixture.CreateSeededContextAsync();

    private static async Task<List<CfgProfile>> LoadOfficialProfilesAsync(AchDbContext context)
    {
        return await context.CfgProfiles
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.Status)
            .Include(x => x.Tags)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
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
            .Include(x => x.Status)
            .Include(x => x.Tags)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
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
            profile.LayoutVariants.Where(variant => variant.RecordCode.Code == "7").Select(variant => variant.VariantCode)
                .Should().BeEquivalentTo("ACH_R7_CREDIT_V2", "ACH_R7_DEBIT_V2");
        }
    }
}
