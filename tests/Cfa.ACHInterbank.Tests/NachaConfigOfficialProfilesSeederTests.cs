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
        var profile = await LoadProfileAsync(context, AchColOfficialNachaLayout.OutboundOriginalProfileCode);

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
        profile.EffectiveFrom.Should().Be(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc));
        profile.EffectiveTo.Should().BeNull();
        profile.Tags.Should().Contain(tag => tag.TagKey == "NormativeVersion" && tag.TagValue == "2026-05-07")
            .And.Contain(tag => tag.TagKey == "IsPlaceholder" && tag.TagValue == "false")
            .And.Contain(tag => tag.TagKey == "IsHomologated" && tag.TagValue == "false");
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
        activeOrdinary.Should().HaveCount(4)
            .And.OnlyContain(profile => profile.VersionMajor == 35
                                        && profile.VersionMinor == 0
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
    public async Task PublishedProfiles_ShouldHaveEffectiveDates()
    {
        await using var context = await SeedAsync();
        var profiles = await LoadOfficialProfilesAsync(context);

        profiles.Should().OnlyContain(x => x.PublishedAt.HasValue);
        profiles.Where(profile => CenitOrdinaryOutbound2026Layout.IsProfile(profile.ProfileCode)
                                  || CenitCtxOutbound2026Layout.IsProfile(profile.ProfileCode))
            .Should().OnlyContain(profile => profile.EffectiveFrom == new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc));
        profiles.Where(profile => !CenitOrdinaryOutbound2026Layout.IsProfile(profile.ProfileCode)
                                  && !CenitCtxOutbound2026Layout.IsProfile(profile.ProfileCode)
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
            .Should().HaveCount(2)
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
        result.Profile!.ProfileCode.Should().Be(AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        result.Profile.VersionMajor.Should().Be(AchColOfficialNachaLayout.ProfileVersionMajor);
        result.Profile.VersionMinor.Should().Be(AchColOfficialNachaLayout.ProfileVersionMinor);
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
        result.Profile!.ProfileCode.Should().Be(CenitCtxOutbound2026Layout.OriginalProfileCode);
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
        await context.SaveChangesAsync();

        var result = await ResolveCenitInboundAsync(context, "ORIGINAL", "CTX");

        result.Success.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileAmbiguous);
    }

    [Fact]
    public async Task CenitCtxProfile_ShouldFailClosedWhenMissing()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles.SingleAsync(item =>
            item.ProfileCode == CenitCtxOutbound2026Layout.OriginalProfileCode);
        profile.StatusId = await context.CatConfigStatuses
            .Where(status => status.Code == "INACTIVO")
            .Select(status => status.Id)
            .SingleAsync();
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
            variant.Profile.ProfileCode == CenitCtxOutbound2026Layout.OriginalProfileCode
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
        await context.SaveChangesAsync();

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
    [InlineData("ACH", AchColOfficialNachaLayout.OutboundPrenotificationProfileCode)]
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
