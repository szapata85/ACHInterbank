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

public class NachaConfigOfficialProfilesSeederTests
{
    private static readonly string[] RequiredRecords = ["1", "5", "6", "7", "8", "9"];

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
        profiles.Single(x => x.ClearingHouse.Code == "ACH").Tags.Should().Contain(t => t.TagValue == "MAN-004 V32");
        profiles.Single(x => x.ClearingHouse.Code == "CENIT").Tags.Should().Contain(t => t.TagValue.Contains("CENIT/DSP-152"));
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

    private static async Task<AchDbContext> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        return context;
    }

    private static async Task<List<CfgProfile>> LoadOfficialProfilesAsync(AchDbContext context)
    {
        return await context.CfgProfiles
            .Include(x => x.ClearingHouse)
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
