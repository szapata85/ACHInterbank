using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractActivationRollbackTests
{
    [Fact]
    public async Task Activation_ShouldPublishMappingAndRetireCurrentPairAtomically()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var candidate = await fixture.CreateCompatibleCandidateAsync(setup, "p4.4-activate");

        var result = await fixture.ActivateAsync(candidate, "p4.4-test");

        Assert.Equal("Activation", result.Transition);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, result.Contract.Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, result.MappingSet.Status);
        Assert.Equal(candidate.Contract.Id, result.MappingSet.ContractVersionId);
        Assert.True(result.Validation.IsValid);
        Assert.Equal(setup.SourceContract.Id, result.PreviousContractVersionId);
        Assert.Equal(setup.SourceMapping.Id, result.PreviousMappingSetId);

        var sourceContract = await fixture.Contract(setup.SourceContract.Id);
        var sourceMapping = await fixture.Mapping(setup.SourceMapping.Id);
        Assert.Equal(IntegrationContractVersionStatusEnum.Retired, sourceContract.Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Archived, sourceMapping.Status);
        Assert.False(sourceMapping.IsActive);
        Assert.Equal(1, await fixture.Context.IntegrationContractVersions.CountAsync(x =>
            x.MethodId == setup.Method.Id && x.Status == IntegrationContractVersionStatusEnum.Active));
        Assert.Contains(await fixture.Context.IntegrationMappingSetHistory.AsNoTracking().ToListAsync(),
            x => x.MappingSetId == candidate.Mapping.Id && x.Action == "ActivatedWithContract");
        var auditEntities = await fixture.Context.AuditLogs.AsNoTracking()
            .Select(x => x.EntityName)
            .ToListAsync();
        Assert.Contains(nameof(IntegrationContractVersion), auditEntities);
        Assert.Contains(nameof(IntegrationMappingSet), auditEntities);
    }

    [Fact]
    public async Task Activation_IncompleteMapping_ShouldFailWithoutPartialState()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p4.4-incomplete");
        await fixture.AddRequiredParameterAsync(target.Contract.Id, "P4_4_REQUIRED");
        var mapping = await fixture.CloneMappingAsync(setup, target.Contract.Id, target.Contract.Revision, "mapping-incomplete");
        var validated = await fixture.ContractService.MarkValidatedAsync(target.Contract.Id, target.Contract.Revision);
        var mappingBefore = await fixture.Mapping(mapping.TargetMappingSet.Id);
        var sourceContractBefore = await fixture.Contract(setup.SourceContract.Id);
        var sourceMappingBefore = await fixture.Mapping(setup.SourceMapping.Id);
        var historyCountBefore = await fixture.Context.IntegrationMappingSetHistory.CountAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ContractService.ActivateAsync(
            target.Contract.Id,
            new ActivateIntegrationContractVersionRequest(
                mapping.TargetMappingSet.Id,
                validated.Revision,
                mappingBefore.Revision,
                "p4.4-test")));

        var sourceContractAfter = await fixture.Contract(setup.SourceContract.Id);
        var sourceMappingAfter = await fixture.Mapping(setup.SourceMapping.Id);
        var targetAfter = await fixture.Contract(target.Contract.Id);
        var mappingAfter = await fixture.Mapping(mapping.TargetMappingSet.Id);
        Assert.Equal(sourceContractBefore.Status, sourceContractAfter.Status);
        Assert.Equal(sourceContractBefore.Revision, sourceContractAfter.Revision);
        Assert.Equal(sourceMappingBefore.Status, sourceMappingAfter.Status);
        Assert.Equal(sourceMappingBefore.Revision, sourceMappingAfter.Revision);
        Assert.Equal(IntegrationContractVersionStatusEnum.Validated, targetAfter.Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Draft, mappingAfter.Status);
        Assert.Equal(mappingBefore.Revision, mappingAfter.Revision);
        Assert.Equal(historyCountBefore, await fixture.Context.IntegrationMappingSetHistory.CountAsync());
    }

    [Fact]
    public async Task Activation_MappingFromAnotherContractVersion_ShouldFailClosed()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var first = await fixture.CreateCompatibleCandidateAsync(setup, "p4.4-first");
        var second = await fixture.CreateCompatibleCandidateAsync(setup, "p4.4-second");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ContractService.ActivateAsync(
            first.Contract.Id,
            new ActivateIntegrationContractVersionRequest(
                second.Mapping.Id,
                first.Contract.Revision,
                second.Mapping.Revision,
                "p4.4-test")));

        Assert.Contains("no pertenece exactamente", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, (await fixture.Contract(setup.SourceContract.Id)).Status);
        Assert.Equal(IntegrationContractVersionStatusEnum.Validated, (await fixture.Contract(first.Contract.Id)).Status);
    }

    [Fact]
    public async Task MappingUpdate_ParameterFromAnotherContractVersion_ShouldFailClosed()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p8.3-cross-version-parameter");
        var mapping = await fixture.CloneMappingAsync(
            setup,
            target.Contract.Id,
            target.Contract.Revision,
            "mapping-p8.3-cross-version-parameter");
        var sourceParameter = await fixture.Context.IntegrationMethodParameters
            .AsNoTracking()
            .FirstAsync(parameter =>
                parameter.ContractVersionId == setup.SourceContract.Id
                && parameter.Direction == IntegrationParameterDirectionEnum.Input
                && parameter.IsActive);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.MappingService.UpsertRulesAsync(
                mapping.TargetMappingSet.Id,
                new UpsertIntegrationMappingRulesRequest("p8.3-test",
                [
                    new UpsertIntegrationMappingRuleRequest(
                        null,
                        setup.Method.Id,
                        sourceParameter.Id,
                        IntegrationSourceKindEnum.Constant,
                        null,
                        null,
                        "P8.3",
                        null,
                        null,
                        null,
                        1,
                        null,
                        true,
                        null)
                ])));

        Assert.Contains("ParameterId no son válidos", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Activation_RevisionConflict_ShouldFailBeforeStateTransition()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var candidate = await fixture.CreateCompatibleCandidateAsync(setup, "p4.4-revision");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => fixture.ContractService.ActivateAsync(
            candidate.Contract.Id,
            new ActivateIntegrationContractVersionRequest(
                candidate.Mapping.Id,
                candidate.Contract.Revision,
                candidate.Mapping.Revision - 1,
                "p4.4-test")));

        Assert.Equal(IntegrationContractVersionStatusEnum.Active, (await fixture.Contract(setup.SourceContract.Id)).Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, (await fixture.Mapping(setup.SourceMapping.Id)).Status);
        Assert.Equal(IntegrationContractVersionStatusEnum.Validated, (await fixture.Contract(candidate.Contract.Id)).Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Draft, (await fixture.Mapping(candidate.Mapping.Id)).Status);
    }

    [Fact]
    public async Task Activation_RequiresDevelopmentDiff_ShouldFailClosed()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var target = await fixture.CloneContractAsync(setup, "p4.4-incompatible");
        var targetEntity = await fixture.Context.IntegrationContractVersions.SingleAsync(x => x.Id == target.Contract.Id);
        targetEntity.TargetNamespace += "/incompatible";
        await fixture.Context.SaveChangesAsync();
        var targetRevision = targetEntity.Revision;
        var mapping = await fixture.CloneMappingAsync(setup, targetEntity.Id, targetRevision, "mapping-incompatible");
        var validated = await fixture.ContractService.MarkValidatedAsync(targetEntity.Id, targetRevision);
        var latestMapping = await fixture.Mapping(mapping.TargetMappingSet.Id);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ContractService.ActivateAsync(
            targetEntity.Id,
            new ActivateIntegrationContractVersionRequest(
                latestMapping.Id,
                validated.Revision,
                latestMapping.Revision,
                "p4.4-test")));

        Assert.Contains("RequiresDevelopment", error.Message, StringComparison.Ordinal);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, (await fixture.Contract(setup.SourceContract.Id)).Status);
        Assert.Equal(IntegrationContractVersionStatusEnum.Validated, (await fixture.Contract(targetEntity.Id)).Status);
    }

    [Fact]
    public async Task Rollback_ShouldReactivateHistoricalPairWithoutCloningOrChangingVersionNumber()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var candidate = await fixture.CreateCompatibleCandidateAsync(setup, "p4.4-rollback");
        var activated = await fixture.ActivateAsync(candidate, "p4.4-activate");
        var sourceRetired = await fixture.Contract(setup.SourceContract.Id);
        var sourceArchived = await fixture.Mapping(setup.SourceMapping.Id);
        var currentActive = await fixture.Contract(candidate.Contract.Id);
        var currentPublished = await fixture.Mapping(candidate.Mapping.Id);
        var targetVersionBefore = sourceArchived.Version;
        var mappingCountBefore = await fixture.Context.IntegrationMappingSets.CountAsync();
        var contractCountBefore = await fixture.Context.IntegrationContractVersions.CountAsync();

        var rollback = await fixture.ContractService.RollbackAsync(
            setup.SourceContract.Id,
            new RollbackIntegrationContractVersionRequest(
                setup.SourceMapping.Id,
                currentActive.Id,
                currentPublished.Id,
                sourceRetired.Revision,
                sourceArchived.Revision,
                currentActive.Revision,
                currentPublished.Revision,
                "p4.4-rollback"));

        Assert.Equal("Rollback", rollback.Transition);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, rollback.Contract.Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, rollback.MappingSet.Status);
        Assert.Equal(targetVersionBefore, rollback.MappingSet.Version);
        Assert.Equal(mappingCountBefore, await fixture.Context.IntegrationMappingSets.CountAsync());
        Assert.Equal(contractCountBefore, await fixture.Context.IntegrationContractVersions.CountAsync());
        Assert.Equal(1, await fixture.Context.IntegrationContractVersions.CountAsync(x =>
            x.MethodId == setup.Method.Id && x.Status == IntegrationContractVersionStatusEnum.Active));
        Assert.Equal(IntegrationContractVersionStatusEnum.Retired, (await fixture.Contract(currentActive.Id)).Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Archived, (await fixture.Mapping(currentPublished.Id)).Status);
        Assert.Contains(await fixture.Context.IntegrationMappingSetHistory.AsNoTracking().ToListAsync(),
            x => x.MappingSetId == setup.SourceMapping.Id && x.Action == "ReactivatedByRollback");
    }

    [Fact]
    public async Task Rollback_WhenHistoricalMappingBecameIncompatible_ShouldFailWithoutPartialState()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var candidate = await fixture.CreateCompatibleCandidateAsync(setup, "p4.4-rollback-invalid");
        await fixture.ActivateAsync(candidate, "p4.4-activate");

        var rule = await fixture.Context.IntegrationMappingRules.AsNoTracking()
            .FirstAsync(x => x.MappingSetId == setup.SourceMapping.Id && x.SourceCatalogFieldId.HasValue);
        var sourceField = await fixture.Context.IntegrationSourceCatalogFields
            .SingleAsync(x => x.Id == rule.SourceCatalogFieldId!.Value);
        sourceField.IsActive = false;
        await fixture.Context.SaveChangesAsync();

        var sourceRetired = await fixture.Contract(setup.SourceContract.Id);
        var sourceArchived = await fixture.Mapping(setup.SourceMapping.Id);
        var currentActive = await fixture.Contract(candidate.Contract.Id);
        var currentPublished = await fixture.Mapping(candidate.Mapping.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ContractService.RollbackAsync(
            setup.SourceContract.Id,
            new RollbackIntegrationContractVersionRequest(
                setup.SourceMapping.Id,
                currentActive.Id,
                currentPublished.Id,
                sourceRetired.Revision,
                sourceArchived.Revision,
                currentActive.Revision,
                currentPublished.Revision,
                "p4.4-rollback")));

        Assert.Equal(IntegrationContractVersionStatusEnum.Retired, (await fixture.Contract(setup.SourceContract.Id)).Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Archived, (await fixture.Mapping(setup.SourceMapping.Id)).Status);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, (await fixture.Contract(candidate.Contract.Id)).Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, (await fixture.Mapping(candidate.Mapping.Id)).Status);
    }

    [Fact]
    public async Task Rollback_RevisionConflict_ShouldKeepCurrentPairActive()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var candidate = await fixture.CreateCompatibleCandidateAsync(setup, "p4.4-rollback-revision");
        await fixture.ActivateAsync(candidate, "p4.4-activate");
        var sourceRetired = await fixture.Contract(setup.SourceContract.Id);
        var sourceArchived = await fixture.Mapping(setup.SourceMapping.Id);
        var currentActive = await fixture.Contract(candidate.Contract.Id);
        var currentPublished = await fixture.Mapping(candidate.Mapping.Id);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => fixture.ContractService.RollbackAsync(
            setup.SourceContract.Id,
            new RollbackIntegrationContractVersionRequest(
                setup.SourceMapping.Id,
                currentActive.Id,
                currentPublished.Id,
                sourceRetired.Revision,
                sourceArchived.Revision,
                currentActive.Revision,
                currentPublished.Revision - 1,
                "p4.4-rollback")));

        Assert.Equal(IntegrationContractVersionStatusEnum.Active, (await fixture.Contract(candidate.Contract.Id)).Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, (await fixture.Mapping(candidate.Mapping.Id)).Status);
    }

    [Fact]
    public async Task Rollback_ShouldAppendAuditHistoryWithoutRewritingExistingHistoryRows()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var candidate = await fixture.CreateCompatibleCandidateAsync(setup, "p4.4-history");
        await fixture.ActivateAsync(candidate, "p4.4-activate");

        var before = await fixture.Context.IntegrationMappingSetHistory.AsNoTracking()
            .Where(x => x.MappingSetId == setup.SourceMapping.Id || x.MappingSetId == candidate.Mapping.Id)
            .ToDictionaryAsync(x => x.Id, x => new { x.Action, x.SnapshotHash, x.SnapshotJson, x.PerformedAtUtc });

        var sourceRetired = await fixture.Contract(setup.SourceContract.Id);
        var sourceArchived = await fixture.Mapping(setup.SourceMapping.Id);
        var currentActive = await fixture.Contract(candidate.Contract.Id);
        var currentPublished = await fixture.Mapping(candidate.Mapping.Id);
        await fixture.ContractService.RollbackAsync(
            setup.SourceContract.Id,
            new RollbackIntegrationContractVersionRequest(
                setup.SourceMapping.Id,
                currentActive.Id,
                currentPublished.Id,
                sourceRetired.Revision,
                sourceArchived.Revision,
                currentActive.Revision,
                currentPublished.Revision,
                "p4.4-rollback"));

        var after = await fixture.Context.IntegrationMappingSetHistory.AsNoTracking()
            .Where(x => x.MappingSetId == setup.SourceMapping.Id || x.MappingSetId == candidate.Mapping.Id)
            .ToListAsync();
        foreach (var item in before)
        {
            var persisted = Assert.Single(after, x => x.Id == item.Key);
            Assert.Equal(item.Value.Action, persisted.Action);
            Assert.Equal(item.Value.SnapshotHash, persisted.SnapshotHash);
            Assert.Equal(item.Value.SnapshotJson, persisted.SnapshotJson);
            Assert.Equal(item.Value.PerformedAtUtc, persisted.PerformedAtUtc);
        }

        Assert.True(after.Count > before.Count);
    }

    [Fact]
    public async Task MappingHistory_ShouldRejectMutationOrDeletion()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();
        var history = await fixture.Context.IntegrationMappingSetHistory
            .FirstAsync(x => x.MappingSetId == setup.SourceMapping.Id);
        history.Action = "rewrite-not-allowed";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Context.SaveChangesAsync());
        Assert.Contains("historial de MappingSet es inmutable", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private Fixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            Validation = new IntegrationMappingValidationService(context);
            ContractService = new IntegrationContractVersionService(context, Validation, new IntegrationMappingSnapshotBuilder(context));
            MappingService = new IntegrationMappingSetService(context, Validation, new IntegrationMappingPreviewService(context));
        }

        public AchDbContext Context { get; }
        public IntegrationMappingValidationService Validation { get; }
        public IntegrationContractVersionService ContractService { get; }
        public IntegrationMappingSetService MappingService { get; }

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
                .SingleAsync(x => x.MethodId == method.Id
                    && x.ContractVersionId == sourceContract.Id
                    && x.Status == IntegrationMappingSetStatusEnum.Published);
            return new Setup(method, sourceContract, sourceMapping);
        }

        public Task<IntegrationContractVersionDetailDto> CloneContractAsync(Setup setup, string label)
            => ContractService.CloneAsync(
                setup.Method.Id,
                new CloneIntegrationContractVersionRequest(
                    setup.SourceContract.Id,
                    label,
                    "P4.4 test",
                    "p4.4-test"));

        public Task<IntegrationMappingContractCloneResultDto> CloneMappingAsync(
            Setup setup,
            Guid targetContractId,
            long targetContractRevision,
            string name)
            => MappingService.CloneToContractVersionAsync(
                setup.SourceMapping.Id,
                new CloneIntegrationMappingToContractVersionRequest(
                    targetContractId,
                    name,
                    "p4.4-test",
                    setup.SourceMapping.Revision,
                    targetContractRevision));

        public async Task<Candidate> CreateCompatibleCandidateAsync(Setup setup, string label)
        {
            var target = await CloneContractAsync(setup, label);
            var clone = await CloneMappingAsync(setup, target.Contract.Id, target.Contract.Revision, $"mapping-{label}");
            var validated = await ContractService.MarkValidatedAsync(target.Contract.Id, target.Contract.Revision);
            var mapping = await Mapping(clone.TargetMappingSet.Id);
            return new Candidate(validated, mapping);
        }

        public Task<IntegrationContractMappingTransitionResultDto> ActivateAsync(Candidate candidate, string actor)
            => ContractService.ActivateAsync(
                candidate.Contract.Id,
                new ActivateIntegrationContractVersionRequest(
                    candidate.Mapping.Id,
                    candidate.Contract.Revision,
                    candidate.Mapping.Revision,
                    actor));

        public async Task AddRequiredParameterAsync(Guid contractVersionId, string path)
        {
            var contract = await Contract(contractVersionId);
            Context.IntegrationMethodParameters.Add(new IntegrationMethodParameter
            {
                MethodId = contract.MethodId,
                ContractVersionId = contractVersionId,
                ParameterPath = path,
                XmlElementName = path,
                DisplayName = path,
                DescriptionEs = string.Empty,
                Category = "P4.4",
                ExampleValue = string.Empty,
                UiHelpText = string.Empty,
                DataType = "string",
                XsdType = "xsd:string",
                Nillable = false,
                MinOccurs = 1,
                MaxOccurs = 1,
                SupportStatus = "Supported",
                Direction = IntegrationParameterDirectionEnum.Input,
                Cardinality = IntegrationParameterCardinalityEnum.Scalar,
                Required = true,
                SortOrder = 999,
                IsActive = true
            });
            await Context.SaveChangesAsync();
        }

        public Task<IntegrationContractVersion> Contract(Guid id)
            => Context.IntegrationContractVersions.AsNoTracking().SingleAsync(x => x.Id == id);

        public Task<IntegrationMappingSet> Mapping(Guid id)
            => Context.IntegrationMappingSets.AsNoTracking().SingleAsync(x => x.Id == id);

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

    private sealed record Candidate(
        IntegrationContractVersionDto Contract,
        IntegrationMappingSet Mapping);
}
