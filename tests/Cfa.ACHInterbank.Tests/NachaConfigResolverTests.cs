using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        context.CfgProfileRecords.AddRange(
            new CfgProfileRecord { Id = 100, ProfileId = 10, RecordCodeId = 1, Sequence = 10, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { Id = 101, ProfileId = 10, RecordCodeId = 2, Sequence = 20, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" });

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
}
