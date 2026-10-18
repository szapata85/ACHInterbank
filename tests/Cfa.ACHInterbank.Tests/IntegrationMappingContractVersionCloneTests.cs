using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationMappingContractVersionCloneTests
{
    [Fact]
    public async Task CloneCompatible_ShouldCopyByParameterPath_AndPreserveSource()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p4.3-compatible");

        var sourceContractBefore = await fixture.Context.IntegrationContractVersions.AsNoTracking().SingleAsync(x => x.Id == setup.SourceContract.Id);
        var sourceMappingBefore = await fixture.Context.IntegrationMappingSets.AsNoTracking().SingleAsync(x => x.Id == setup.SourceMapping.Id);
        var sourceRuleCountBefore = await fixture.Context.IntegrationMappingRules.CountAsync(x => x.MappingSetId == setup.SourceMapping.Id);

        var result = await fixture.MappingService.CloneToContractVersionAsync(
            setup.SourceMapping.Id,
            new CloneIntegrationMappingToContractVersionRequest(
                target.Contract.Id,
                "mapping-v2",
                "p4.3-test",
                setup.SourceMapping.Revision,
                target.Contract.Revision));

        Assert.Equal(IntegrationMappingSetStatusEnum.Draft, result.TargetMappingSet.Status);
        Assert.Equal(target.Contract.Id, result.TargetMappingSet.ContractVersionId);
        Assert.DoesNotContain(result.Classifications, x => x.Classification is IntegrationMappingContractCloneClassificationEnum.New
            or IntegrationMappingContractCloneClassificationEnum.Removed
            or IntegrationMappingContractCloneClassificationEnum.Incompatible);
        Assert.True(result.Validation.IsComplete);

        var sourceContractAfter = await fixture.Context.IntegrationContractVersions.AsNoTracking().SingleAsync(x => x.Id == setup.SourceContract.Id);
        var sourceMappingAfter = await fixture.Context.IntegrationMappingSets.AsNoTracking().SingleAsync(x => x.Id == setup.SourceMapping.Id);
        var sourceRuleCountAfter = await fixture.Context.IntegrationMappingRules.CountAsync(x => x.MappingSetId == setup.SourceMapping.Id);

        Assert.Equal(sourceContractBefore.Status, sourceContractAfter.Status);
        Assert.Equal(sourceContractBefore.Revision, sourceContractAfter.Revision);
        Assert.Equal(sourceMappingBefore.Status, sourceMappingAfter.Status);
        Assert.Equal(sourceMappingBefore.Revision, sourceMappingAfter.Revision);
        Assert.Equal(sourceRuleCountBefore, sourceRuleCountAfter);
    }

    [Fact]
    public async Task Clone_NewOptional_ShouldClassifyNewWithoutMakingDraftIncomplete()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p4.3-new-optional");
        await fixture.AddTargetParameterAsync(target.Contract.Id, required: false, "P4_3_OPTIONAL");

        var result = await fixture.CloneMappingAsync(setup, target, "mapping-optional");

        Assert.Contains(result.Classifications, x => x.TargetParameterPath == "P4_3_OPTIONAL"
            && x.Classification == IntegrationMappingContractCloneClassificationEnum.New);
        Assert.True(result.Validation.IsComplete);
    }

    [Fact]
    public async Task Clone_NewRequired_ShouldLeaveDraftIncompleteUntilMapped()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p4.3-new-required");
        var newParameter = await fixture.AddTargetParameterAsync(target.Contract.Id, required: true, "P4_3_REQUIRED");

        var result = await fixture.CloneMappingAsync(setup, target, "mapping-required");

        Assert.Contains(result.Classifications, x => x.TargetParameterPath == newParameter.ParameterPath
            && x.Classification == IntegrationMappingContractCloneClassificationEnum.New);
        Assert.False(result.Validation.IsComplete);
        Assert.Contains(result.Validation.MappingValidation.Issues, x => x.Code == "REQUIRED_PARAMETER_MISSING"
            && x.Path == newParameter.ParameterPath);
    }

    [Fact]
    public async Task ContractDriven_NewRequiredScalar_ShouldSurfaceOneNewPending_ThenBecomePublishableAfterGuidedMapping()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p6.4-new-required-scalar");
        var newParameter = await fixture.AddTargetParameterAsync(target.Contract.Id, required: true, "P6_4_REQUIRED_SCALAR");

        var clone = await fixture.CloneMappingAsync(setup, target, "mapping-p6.4-new-field");
        var before = await fixture.MappingService.GetWorkspaceAsync(clone.TargetMappingSet.Id);

        Assert.NotNull(before);
        Assert.Equal(1, before!.Publication.PendingRequired);
        Assert.Equal(1, before.Publication.NewRequiredPending);
        Assert.False(before.Publication.CanPublish);
        Assert.Contains(before.Parameters, parameter => parameter.ParameterId == newParameter.Id
            && parameter.Required
            && parameter.Status == "Pending"
            && parameter.CurrentRule is null);

        var compatibleSource = before.SourceCompatibility
            .First(item => item.ParameterId == newParameter.Id && item.AllowsNoTransformation);

        var updated = await fixture.MappingService.UpdateParameterMappingAsync(
            clone.TargetMappingSet.Id,
            newParameter.Id,
            new UpdateIntegrationParameterMappingRequest(
                clone.TargetMappingSet.Revision,
                compatibleSource.SourceCatalogFieldId,
                null,
                null,
                "p6.4-test"));

        var after = await fixture.MappingService.GetWorkspaceAsync(updated.Id);
        Assert.NotNull(after);
        Assert.Equal(0, after!.Publication.PendingRequired);
        Assert.Equal(0, after.Publication.NewRequiredPending);
        Assert.True(after.Publication.CanPublish);
    }

    [Fact]
    public async Task Clone_RemovedParameter_ShouldRemainHistoricalEvidenceOnly()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p4.3-removed");
        var sourcePath = await fixture.GetMappedSourcePathAsync(setup.SourceMapping.Id);
        var targetParameter = await fixture.Context.IntegrationMethodParameters
            .SingleAsync(x => x.ContractVersionId == target.Contract.Id && x.ParameterPath == sourcePath);
        fixture.Context.IntegrationMethodParameters.Remove(targetParameter);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.CloneMappingAsync(setup, target, "mapping-removed");

        Assert.Contains(result.Classifications, x => x.SourceParameterPath == sourcePath
            && x.TargetParameterPath is null
            && x.Classification == IntegrationMappingContractCloneClassificationEnum.Removed);
        Assert.DoesNotContain(result.TargetMappingSet.Rules, x => x.ParameterId == targetParameter.Id);

        var history = await fixture.Context.IntegrationMappingSetHistory.AsNoTracking()
            .SingleAsync(x => x.MappingSetId == result.TargetMappingSet.Id && x.Action == "ClonedAcrossContractVersions");
        Assert.Contains("Removed", history.SnapshotJson, StringComparison.Ordinal);
        Assert.Contains(sourcePath, history.SnapshotJson, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Clone_TypeOrCardinalityChange_ShouldClassifyIncompatible(bool changeType)
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, changeType ? "p4.3-type" : "p4.3-cardinality");
        var sourcePath = await fixture.GetMappedSourcePathAsync(setup.SourceMapping.Id);
        var targetParameter = await fixture.Context.IntegrationMethodParameters
            .SingleAsync(x => x.ContractVersionId == target.Contract.Id && x.ParameterPath == sourcePath);

        if (changeType)
        {
            targetParameter.DataType = "long";
            targetParameter.XsdType = "xsd:long";
        }
        else
        {
            targetParameter.Cardinality = IntegrationParameterCardinalityEnum.Collection;
            targetParameter.MaxOccurs = null;
            targetParameter.SupportStatus = "RequiresDevelopment";
        }

        await fixture.Context.SaveChangesAsync();
        var result = await fixture.CloneMappingAsync(setup, target, changeType ? "mapping-type" : "mapping-cardinality");

        Assert.Contains(result.Classifications, x => x.TargetParameterPath == sourcePath
            && x.Classification == IntegrationMappingContractCloneClassificationEnum.Incompatible);
        Assert.DoesNotContain(result.TargetMappingSet.Rules, rule => rule.ParameterId == targetParameter.Id);
        Assert.False(result.Validation.IsComplete);
    }

    [Fact]
    public async Task Clone_ChangedParameterPath_ShouldFailClosedAsIncompatible()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p4.3-path");
        var sourcePath = await fixture.GetMappedSourcePathAsync(setup.SourceMapping.Id);
        var targetParameter = await fixture.Context.IntegrationMethodParameters
            .SingleAsync(x => x.ContractVersionId == target.Contract.Id && x.ParameterPath == sourcePath);
        targetParameter.ParameterPath = sourcePath + "_V2";
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.CloneMappingAsync(setup, target, "mapping-path");

        Assert.Contains(result.Classifications, x => x.SourceParameterPath == sourcePath
            && x.TargetParameterPath == sourcePath + "_V2"
            && x.Classification == IntegrationMappingContractCloneClassificationEnum.Incompatible);
        Assert.False(result.Validation.IsComplete);
    }

    [Fact]
    public async Task ContractMappingValidation_ShouldBecomeCompleteAfterRequiredNewFieldIsMapped()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p4.3-validation");
        var newParameter = await fixture.AddTargetParameterAsync(target.Contract.Id, required: true, "P4_3_REQUIRED_MAP");
        var clone = await fixture.CloneMappingAsync(setup, target, "mapping-validation");
        Assert.False(clone.Validation.IsComplete);

        var mapped = await fixture.MappingService.UpsertRulesAsync(
            clone.TargetMappingSet.Id,
            new UpsertIntegrationMappingRulesRequest("p4.3-test",
            [
                new UpsertIntegrationMappingRuleRequest(
                    null,
                    setup.Method.Id,
                    newParameter.Id,
                    IntegrationSourceKindEnum.Constant,
                    null,
                    null,
                    "VALUE",
                    null,
                    null,
                    null,
                    1,
                    null,
                    true,
                    null)
            ]));

        var validation = await fixture.MappingService.ValidateContractMappingAsync(
            mapped.Id,
            new ValidateIntegrationContractMappingRequest(
                mapped.Revision,
                target.Contract.Revision,
                true));

        Assert.True(validation.IsComplete);
        Assert.DoesNotContain(validation.MappingValidation.Issues, x => x.Code == "REQUIRED_PARAMETER_MISSING");
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private Fixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            var validation = new IntegrationMappingValidationService(context);
            MappingService = new IntegrationMappingSetService(context, validation, new IntegrationMappingPreviewService(context));
            ContractService = new IntegrationContractVersionService(context);
        }

        public AchDbContext Context { get; }
        public IntegrationMappingSetService MappingService { get; }
        public IntegrationContractVersionService ContractService { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new Fixture(connection, context);
        }

        public async Task<Setup> PrepareAsync()
        {
            await new IntegrationMappingBootstrapper(Context).EnsureAsync();
            var method = await Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas");
            var sourceContract = await Context.IntegrationContractVersions.AsNoTracking()
                .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
            var sourceMapping = await Context.IntegrationMappingSets.AsNoTracking()
                .Where(x => x.MethodId == method.Id
                    && x.ContractVersionId == sourceContract.Id
                    && x.Status == IntegrationMappingSetStatusEnum.Published)
                .OrderByDescending(x => x.Version)
                .FirstAsync();
            return new Setup(method, sourceContract, sourceMapping);
        }

        public Task<IntegrationContractVersionDetailDto> CloneContractAsync(Setup setup, string label)
            => ContractService.CloneAsync(
                setup.Method.Id,
                new CloneIntegrationContractVersionRequest(
                    setup.SourceContract.Id,
                    label,
                    "P4.3 test",
                    "p4.3-test"));

        public async Task<IntegrationMethodParameter> AddTargetParameterAsync(Guid contractVersionId, bool required, string path)
        {
            var contract = await Context.IntegrationContractVersions.AsNoTracking().SingleAsync(x => x.Id == contractVersionId);
            var parameter = new IntegrationMethodParameter
            {
                MethodId = contract.MethodId,
                ContractVersionId = contractVersionId,
                ParameterPath = path,
                XmlElementName = path,
                DisplayName = path,
                DescriptionEs = string.Empty,
                Category = "P4.3",
                ExampleValue = string.Empty,
                UiHelpText = string.Empty,
                DataType = "string",
                XsdType = "xsd:string",
                Nillable = !required,
                MinOccurs = required ? 1 : 0,
                MaxOccurs = 1,
                SupportStatus = "Supported",
                Direction = IntegrationParameterDirectionEnum.Input,
                Cardinality = IntegrationParameterCardinalityEnum.Scalar,
                Required = required,
                SortOrder = 900,
                IsActive = true
            };
            Context.IntegrationMethodParameters.Add(parameter);
            await Context.SaveChangesAsync();
            return parameter;
        }

        public async Task<string> GetMappedSourcePathAsync(Guid mappingSetId)
            => await Context.IntegrationMappingRules
                .AsNoTracking()
                .Include(x => x.Parameter)
                .Where(x => x.MappingSetId == mappingSetId && x.Enabled)
                .OrderBy(x => x.Priority)
                .Select(x => x.Parameter.ParameterPath)
                .FirstAsync();

        public Task<IntegrationMappingContractCloneResultDto> CloneMappingAsync(
            Setup setup,
            IntegrationContractVersionDetailDto target,
            string name)
            => MappingService.CloneToContractVersionAsync(
                setup.SourceMapping.Id,
                new CloneIntegrationMappingToContractVersionRequest(
                    target.Contract.Id,
                    name,
                    "p4.3-test",
                    setup.SourceMapping.Revision,
                    target.Contract.Revision));

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed record Setup(
        IntegrationMethod Method,
        IntegrationContractVersion SourceContract,
        IntegrationMappingSet SourceMapping);
}
