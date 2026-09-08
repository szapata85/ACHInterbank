using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaConfigResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldSelectPublishedProfileAndLayouts()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);

        var resolver = new NachaConfigResolver(context);
        var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RecordCodes = ["1", "5"]
        });

        result.Success.Should().BeTrue();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileSelected);
        result.Profile.Should().NotBeNull();
        result.SettlementPolicy.Should().Be(NachaSettlementPolicy.SettlementDate);
        result.SemanticContract.Should().NotBeNull();
        result.SemanticContract!.Rules.Should().HaveCount(3);
        result.SemanticContract.TryGetRule("220", out var serviceClass220).Should().BeTrue();
        serviceClass220.Should().Match<NachaServiceClassSemanticRule>(rule => rule.AllowsCredit && !rule.AllowsDebit);
        result.LayoutsByRecordCode.Should().ContainKey("1");
        result.LayoutsByRecordCode.Should().ContainKey("5");
    }

    [Fact]
    public async Task ResolveAsync_ShouldReportAmbiguity_WhenMultipleLayoutsSamePriority()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context, includeAmbiguousLayout: true);

        var resolver = new NachaConfigResolver(context);
        var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RecordCodes = ["1"]
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileAmbiguous);
        result.Profile.Should().NotBeNull();
        result.LayoutsByRecordCode.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnProfileNotFound_WhenDimensionsDoNotExist()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);

        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "RETORNO",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = DateTime.UtcNow
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileNotFound);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnProfileInactive_WhenProfileIsOutsideEffectivePeriod()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        var profile = await context.CfgProfiles.SingleAsync();
        profile.EffectiveFrom = DateTime.UtcNow.AddDays(2);
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequest());

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileInactive);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnProfileVersionUnsupported_WhenRequestedVersionDoesNotExist()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);

        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RequestedVersionMajor = 32,
            RequestedVersionMinor = 0
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileVersionUnsupported);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnClearingHouseUndetermined_WhenClearingHouseIsBlank()
    {
        await using var context = CreateContext();

        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = " ",
            FlowTypeCode = "RETORNO",
            DirectionCode = "ENTRADA",
            ProcessDateUtc = DateTime.UtcNow
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ClearingHouseUndetermined);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnProfileAmbiguous_WhenProfilesHaveSamePriorityAndVersion()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        context.CfgProfiles.Add(new CfgProfile
        {
            Id = 11,
            ProfileCode = "P2",
            NameEs = "Perfil duplicado",
            ClearingHouseId = 1,
            FlowTypeId = 1,
            DirectionId = 1,
            ServiceClassId = 1,
            ContextPriority = 100,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            StatusId = 1,
            VersionMajor = 1,
            VersionMinor = 0,
            RowVersion = [2]
        });
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequest());

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileAmbiguous);
        result.Profile.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnProfileInactive_WhenHomologationIsRequiredButMissing()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequest(requireHomologated: true));

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.ProfileInactive);
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenRequiredOutboundPolicyIsMissing()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequest(requireOutboundPolicy: true));

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.OutboundPolicyMissing);
        result.OutboundPolicy.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenOutboundPolicyIsInternallyInconsistent()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        context.CfgProfileTags.AddRange(
            new CfgProfileTag { Id = 1, ProfileId = 10, TagKey = NachaOutboundPolicyMetadata.FileOrderKey, TagValue = "10" },
            new CfgProfileTag { Id = 2, ProfileId = 10, TagKey = NachaOutboundPolicyMetadata.FileAllocationKey, TagValue = "CombineServicePartitionsByIndex" },
            new CfgProfileTag
            {
                Id = 3,
                ProfileId = 10,
                TagKey = NachaOutboundPolicyMetadata.ServiceKeyPrefix + "PPD",
                TagValue = "Order=10;Strategy=EntriesPerFile;MaxEntriesPerFile=0"
            });
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequest(requireOutboundPolicy: true));

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.OutboundPolicyInvalid);
        result.OutboundPolicy.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenPublishedBatchPolicyIsIncomplete()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        context.CfgProfileTags.Add(new CfgProfileTag
        {
            Id = 1,
            ProfileId = 10,
            TagKey = NachaOutboundPolicyMetadata.BatchNumberStrategyKey,
            TagValue = "FileLocalOrdinal"
        });
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequest(requireOutboundPolicy: true));

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.OutboundPolicyInvalid);
        result.OutboundPolicy.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenRecord5SettlementPolicyIsMissing()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        context.CfgProfileTags.RemoveRange(context.CfgProfileTags);
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RecordCodes = ["5"]
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.SettlementPolicyMissing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("UNSUPPORTED")]
    public async Task ResolveAsync_ShouldFailClosed_WhenSettlementPolicyIsEmptyOrUnsupported(string value)
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        var tag = await context.CfgProfileTags.SingleAsync();
        tag.TagValue = value;
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RecordCodes = ["5"]
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.SettlementPolicyInvalid);
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenSettlementPolicyTagIsAmbiguous()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        context.CfgProfileTags.Add(new CfgProfileTag
        {
            Id = 9001,
            ProfileId = 10,
            TagKey = NachaSettlementPolicyMetadata.TagKey,
            TagValue = NachaSettlementPolicy.JulianSettlementDate.ToString()
        });
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RecordCodes = ["5"]
        });

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.SettlementPolicyInvalid);
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenRequiredSemanticContractIsMissing()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        var record5 = await context.CfgProfileRecords.SingleAsync(record => record.RecordCodeId == 2);
        record5.SemanticRuleSetId = null;
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequestForRecord5());

        result.Success.Should().BeFalse();
        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.SemanticContractMissing);
        result.Warnings.Should().ContainSingle(message => message.Contains("MISSING_SEMANTIC_CONTRACT"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenSemanticRuleIsUnsupported()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        var rule = await context.CfgRuleSetRules.FirstAsync();
        rule.RuleCode = "UNSUPPORTED_RULE";
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequestForRecord5());

        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.SemanticContractInvalid);
        result.Warnings.Should().ContainSingle(message => message.Contains("UNSUPPORTED_SEMANTIC_RULE"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenSemanticDeclarationIsDuplicated()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        context.CfgRuleSetRules.Add(SemanticRule(510, "service_class_allowed_directions_200", "200", "CREDIT", "DEBIT"));
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequestForRecord5());

        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.SemanticContractInvalid);
        result.Warnings.Should().ContainSingle(message => message.Contains("DUPLICATE_SEMANTIC_DECLARATION"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldFailClosed_WhenSemanticDeclarationsContradict()
    {
        await using var context = CreateContext();
        await SeedBaseCatalogAsync(context);
        context.CfgRuleSetRules.Add(SemanticRule(510, "service_class_allowed_directions_220", "220", "DEBIT"));
        await context.SaveChangesAsync();

        var result = await new NachaConfigResolver(context).ResolveAsync(BaseRequestForRecord5());

        result.SelectionStatus.Should().Be(NachaProfileSelectionStatus.SemanticContractInvalid);
        result.Warnings.Should().ContainSingle(message => message.Contains("CONTRADICTORY_SEMANTIC_DECLARATION"));
    }

    private static AchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AchDbContext(options);
    }

    private static async Task SeedBaseCatalogAsync(AchDbContext context, bool includeAmbiguousLayout = false)
    {
        context.CatClearingHouses.Add(new CatClearingHouse { Id = 1, Code = "ACH", Name = "ACH Colombia", IsActive = true });
        context.CatFlowTypes.Add(new CatFlowType { Id = 1, Code = "ORIGINAL", NameEs = "Original", IsActive = true });
        context.CatDirections.Add(new CatDirection { Id = 1, Code = "SALIDA", NameEs = "Salida", IsActive = true });
        context.CatServiceClasses.Add(new CatServiceClass { Id = 1, Code = "PPD", NameEs = "PPD", IsActive = true });
        context.CatConfigStatuses.Add(new CatConfigStatus { Id = 1, Code = "PUBLICADO", IsEditable = false, IsPublishable = false });
        context.CatRecordCodes.AddRange(
            new CatRecordCode { Id = 1, Code = "1", NameEs = "Header", IsMandatoryBase = true },
            new CatRecordCode { Id = 2, Code = "5", NameEs = "Batch", IsMandatoryBase = true });
        context.CatDataSourceTypes.Add(new CatDataSourceType { Id = 1, Code = "CONSTANTE", NameEs = "Constante" });
        context.CatRuleTypes.Add(new CatRuleType { Id = 1, Code = NachaSemanticContractMetadata.RequiredRuleType, NameEs = "Condicional" });

        var profile = new CfgProfile
        {
            Id = 10,
            ProfileCode = "P1",
            NameEs = "Perfil",
            ClearingHouseId = 1,
            FlowTypeId = 1,
            DirectionId = 1,
            ServiceClassId = 1,
            ContextPriority = 100,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            StatusId = 1,
            VersionMajor = 1,
            VersionMinor = 0,
            RowVersion = [1]
        };

        context.CfgProfiles.Add(profile);
        context.CfgProfileTags.Add(new CfgProfileTag
        {
            Id = 9000,
            ProfileId = 10,
            TagKey = NachaSettlementPolicyMetadata.TagKey,
            TagValue = NachaSettlementPolicy.SettlementDate.ToString()
        });
        context.CfgProfileRecords.AddRange(
            new CfgProfileRecord { Id = 100, ProfileId = 10, RecordCodeId = 1, Sequence = 10, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { Id = 101, ProfileId = 10, RecordCodeId = 2, Sequence = 20, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN", SemanticRuleSetId = 500 });

        context.CfgRuleSets.Add(new CfgRuleSet
        {
            Id = 500,
            RuleSetCode = "TEST_SERVICE_CLASS_DIRECTION",
            NameEs = "Contrato semántico",
            Scope = NachaSemanticContractMetadata.RequiredScope
        });
        context.CfgRuleSetRules.AddRange(
            SemanticRule(501, NachaSemanticContractMetadata.RuleCodePrefix + "200", "200", "CREDIT", "DEBIT"),
            SemanticRule(502, NachaSemanticContractMetadata.RuleCodePrefix + "220", "220", "CREDIT"),
            SemanticRule(503, NachaSemanticContractMetadata.RuleCodePrefix + "225", "225", "DEBIT"));

        var source = new CfgFieldSourceDefinition { Id = 1000, DataSourceTypeId = 1, ConstantValue = "1" };
        context.CfgFieldSourceDefinitions.Add(source);

        context.CfgLayoutVariants.Add(new CfgLayoutVariant
        {
            Id = 200,
            ProfileId = 10,
            RecordCodeId = 1,
            VariantCode = "R1_BASE",
            NameEs = "R1",
            Priority = 100,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            StatusId = 1,
            TotalLength = 106,
            IsDefaultForRecord = true
        });

        context.CfgLayoutVariants.Add(new CfgLayoutVariant
        {
            Id = 201,
            ProfileId = 10,
            RecordCodeId = 2,
            VariantCode = "R5_BASE",
            NameEs = "R5",
            Priority = 100,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            StatusId = 1,
            TotalLength = 106,
            IsDefaultForRecord = true
        });

        if (includeAmbiguousLayout)
        {
            context.CfgLayoutVariants.Add(new CfgLayoutVariant
            {
                Id = 202,
                ProfileId = 10,
                RecordCodeId = 1,
                VariantCode = "R1_ALT",
                NameEs = "R1 ALT",
                Priority = 100,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                StatusId = 1,
                TotalLength = 106,
                IsDefaultForRecord = true
            });
        }

        context.CfgLayoutFields.AddRange(
            new CfgLayoutField { Id = 300, LayoutVariantId = 200, FieldCode = "F1", FieldNameEs = "F1", StartPosition = 1, Length = 1, SourceDefinitionId = 1000, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { Id = 301, LayoutVariantId = 201, FieldCode = "F2", FieldNameEs = "F2", StartPosition = 1, Length = 1, SourceDefinitionId = 1000, SortOrder = 1, IsEnabled = true });

        await context.SaveChangesAsync();
    }

    private static NachaConfigResolutionRequest BaseRequest(
        bool requireHomologated = false,
        bool requireOutboundPolicy = false)
        => new()
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RecordCodes = ["1"],
            RequireHomologated = requireHomologated,
            RequireOutboundPolicy = requireOutboundPolicy
        };

    private static NachaConfigResolutionRequest BaseRequestForRecord5()
        => new()
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RecordCodes = ["5"]
        };

    private static CfgRuleSetRule SemanticRule(int id, string ruleCode, string serviceClassCode, params string[] directions)
        => new()
        {
            Id = id,
            RuleSetId = 500,
            RuleTypeId = 1,
            RuleCode = ruleCode,
            RuleConfigJson = JsonSerializer.Serialize(new { serviceClassCode, allowedDirections = directions }),
            ErrorCode = "NACHA_SERVICE_CLASS_DIRECTION_NOT_ALLOWED",
            ErrorMessageEs = "Dirección no permitida.",
            Order = id
        };
}
