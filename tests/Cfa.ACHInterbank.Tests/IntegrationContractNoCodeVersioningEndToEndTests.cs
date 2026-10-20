using System.Xml.Linq;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Integrations.Registries;
using Cfa.ACHInterbank.Application.Integrations.Services;
using Cfa.ACHInterbank.Application.Integrations.Transformations;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Providers;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractNoCodeVersioningEndToEndTests
{
    private const string TestParameterPath = "TIPOCLIENTE_TEST";
    private const string ExistingSourcePath = "transaction.reference";

    [Fact]
    public async Task TipoClienteTest_ShouldBeIntroducedByContractAndMappingOnly_ThenRollbackNonDestructively()
    {
        await using var fixture = await Fixture.CreateAsync();
        var setup = await fixture.PrepareAsync();

        var existingProvider = fixture.SourceRegistry.GetRequired(
            IntegrationSourceKindEnum.Transaction,
            ExistingSourcePath,
            setup.Method.Code);
        Assert.True(string.Equals(ExistingSourcePath, existingProvider.Descriptor.SourceKey, StringComparison.OrdinalIgnoreCase));

        var v2 = await fixture.ContractService.CloneAsync(
            setup.Method.Id,
            new CloneIntegrationContractVersionRequest(
                setup.SourceContract.Id,
                "p4.5-tipocliente-test-v2",
                "P4.5 no-code test",
                "p4.5-test"));

        var newParameter = await fixture.AddRequiredScalarAsync(v2.Contract.Id, TestParameterPath);

        var cloned = await fixture.MappingService.CloneToContractVersionAsync(
            setup.SourceMapping.Id,
            new CloneIntegrationMappingToContractVersionRequest(
                v2.Contract.Id,
                "p4.5-tipocliente-test-mapping",
                "p4.5-test",
                setup.SourceMapping.Revision,
                v2.Contract.Revision));

        Assert.Contains(cloned.Classifications, item =>
            item.TargetParameterPath == TestParameterPath
            && item.Classification == IntegrationMappingContractCloneClassificationEnum.New);
        Assert.False(cloned.Validation.IsComplete);
        Assert.Contains(cloned.Validation.MappingValidation.Issues, issue =>
            issue.Code == "REQUIRED_PARAMETER_MISSING"
            && issue.Path == TestParameterPath);

        var mapped = await fixture.MappingService.UpsertRulesAsync(
            cloned.TargetMappingSet.Id,
            new UpsertIntegrationMappingRulesRequest(
                "p4.5-test",
                [
                    new UpsertIntegrationMappingRuleRequest(
                        null,
                        setup.Method.Id,
                        newParameter.Id,
                        IntegrationSourceKindEnum.Transaction,
                        null,
                        ExistingSourcePath,
                        null,
                        null,
                        null,
                        null,
                        1,
                        null,
                        true,
                        null)
                ]));

        var persistedRule = await fixture.Context.IntegrationMappingRules
            .AsNoTracking()
            .SingleAsync(rule => rule.MappingSetId == mapped.Id && rule.ParameterId == newParameter.Id);
        Assert.Equal(IntegrationSourceKindEnum.Transaction, persistedRule.SourceKind);
        Assert.Equal(ExistingSourcePath, persistedRule.SourceFieldPath);
        Assert.Null(persistedRule.TransformationCode);
        Assert.Null(persistedRule.FixedValue);

        var contractMappingValidation = await fixture.MappingService.ValidateContractMappingAsync(
            mapped.Id,
            new ValidateIntegrationContractMappingRequest(
                mapped.Revision,
                v2.Contract.Revision,
                true));
        Assert.True(contractMappingValidation.IsComplete);
        Assert.True(contractMappingValidation.MappingValidation.IsValid);

        var validatedV2 = await fixture.ContractService.MarkValidatedAsync(v2.Contract.Id, v2.Contract.Revision);
        var candidateMapping = await fixture.MappingService.GetByIdAsync(mapped.Id)
            ?? throw new InvalidOperationException("No se encontró el MappingSet candidato después de validar contract+mapping.");

        var activation = await fixture.ContractService.ActivateAsync(
            validatedV2.Id,
            new ActivateIntegrationContractVersionRequest(
                candidateMapping.Id,
                validatedV2.Revision,
                candidateMapping.Revision,
                "p4.5-test"));

        Assert.Equal(IntegrationContractVersionStatusEnum.Active, activation.Contract.Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, activation.MappingSet.Status);

        var resolved = await fixture.RequestResolver.ResolveAsync(
            new IntegrationExecutionContext(
                setup.Method.Code,
                "p4.5-correlation",
                DateTime.UtcNow,
                transactionId: fixture.Transaction.Id,
                cycleId: fixture.Cycle.Id));

        var xml = fixture.Writer.Write(resolved);
        var operation = ExtractOperation(xml);
        var testElement = Assert.Single(operation.Elements().Where(element => element.Name.LocalName == TestParameterPath));
        Assert.Equal(fixture.Transaction.Reference, testElement.Value);

        var mappedParameterIds = await fixture.Context.IntegrationMappingRules
            .AsNoTracking()
            .Where(rule => rule.MappingSetId == activation.MappingSet.Id && rule.Enabled)
            .Select(rule => rule.ParameterId)
            .Distinct()
            .ToListAsync();
        var expectedOrder = await fixture.Context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(parameter => parameter.ContractVersionId == activation.Contract.Id
                && parameter.IsActive
                && parameter.Direction == IntegrationParameterDirectionEnum.Input
                && mappedParameterIds.Contains(parameter.Id))
            .OrderBy(parameter => parameter.SortOrder)
            .ThenBy(parameter => parameter.ParameterPath)
            .Select(parameter => parameter.XmlElementName)
            .ToListAsync();
        var actualOrder = operation.Elements().Select(element => element.Name.LocalName).ToList();
        Assert.Equal(expectedOrder, actualOrder);

        var retiredV1 = await fixture.ContractService.GetByIdAsync(setup.SourceContract.Id)
            ?? throw new InvalidOperationException("No se encontró v1 después de activar v2.");
        var archivedV1Mapping = await fixture.MappingService.GetByIdAsync(setup.SourceMapping.Id)
            ?? throw new InvalidOperationException("No se encontró el mapping v1 después de activar v2.");

        var rollback = await fixture.ContractService.RollbackAsync(
            retiredV1.Id,
            new RollbackIntegrationContractVersionRequest(
                archivedV1Mapping.Id,
                activation.Contract.Id,
                activation.MappingSet.Id,
                retiredV1.Revision,
                archivedV1Mapping.Revision,
                activation.Contract.Revision,
                activation.MappingSet.Revision,
                "p4.5-test"));

        Assert.Equal("Rollback", rollback.Transition);
        Assert.Equal(setup.SourceContract.Id, rollback.Contract.Id);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, rollback.Contract.Status);
        Assert.Equal(setup.SourceMapping.Id, rollback.MappingSet.Id);
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, rollback.MappingSet.Status);

        var v2AfterRollback = await fixture.ContractService.GetByIdAsync(activation.Contract.Id)
            ?? throw new InvalidOperationException("No se encontró v2 después del rollback.");
        var v2MappingAfterRollback = await fixture.MappingService.GetByIdAsync(activation.MappingSet.Id)
            ?? throw new InvalidOperationException("No se encontró mapping v2 después del rollback.");
        Assert.Equal(IntegrationContractVersionStatusEnum.Retired, v2AfterRollback.Status);
        Assert.Equal(IntegrationMappingSetStatusEnum.Archived, v2MappingAfterRollback.Status);

        var sourceRuleCountAfterRollback = await fixture.Context.IntegrationMappingRules
            .AsNoTracking()
            .CountAsync(rule => rule.MappingSetId == setup.SourceMapping.Id);
        Assert.Equal(setup.SourceRuleCount, sourceRuleCountAfterRollback);
        Assert.True(await fixture.Context.IntegrationContractVersions.AsNoTracking().AnyAsync(contract => contract.Id == activation.Contract.Id));
        Assert.Contains(await fixture.MappingService.GetHistoryAsync(setup.SourceMapping.Id), history => history.Action == "ReactivatedByRollback");
        Assert.Contains(await fixture.MappingService.GetHistoryAsync(activation.MappingSet.Id), history => history.Action == "ArchivedByRollback");
    }

    private static XElement ExtractOperation(string xml)
    {
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var envelope = Assert.IsType<XElement>(document.Root);
        var body = Assert.Single(envelope.Elements().Where(element => element.Name.LocalName == "Body"));
        return Assert.Single(body.Elements());
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
            ContractService = new IntegrationContractVersionService(context, validation, new IntegrationMappingSnapshotBuilder(context));
            SourceRegistry = new IntegrationSourceProviderRegistry(new CurrentIntegrationSourceProviderFactory(context).Create());
            var transformationRegistry = new IntegrationTransformationRegistry(StandardIntegrationTransformationCatalog.Create());
            RequestResolver = new IntegrationRequestResolver(
                new IntegrationExecutionContractResolver(context),
                new IntegrationParameterValueResolver(SourceRegistry, transformationRegistry));
            Writer = new ContractDrivenSoapBodyWriter();
        }

        public AchDbContext Context { get; }
        public IntegrationMappingSetService MappingService { get; }
        public IntegrationContractVersionService ContractService { get; }
        public IntegrationSourceProviderRegistry SourceRegistry { get; }
        public IntegrationRequestResolver RequestResolver { get; }
        public ContractDrivenSoapBodyWriter Writer { get; }
        public AchCycle Cycle { get; private set; } = null!;
        public AchTransaction Transaction { get; private set; } = null!;

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var fixture = new Fixture(connection, context);
            await fixture.SeedRuntimeDataAsync();
            return fixture;
        }

        public async Task<Setup> PrepareAsync()
        {
            await new IntegrationMappingBootstrapper(Context).EnsureAsync();
            var method = await Context.IntegrationMethods.SingleAsync(method => method.Code == CurrentIntegrationSourceProviderFactory.ProcContrapartidasMethod);
            var sourceContract = await Context.IntegrationContractVersions.AsNoTracking()
                .SingleAsync(contract => contract.MethodId == method.Id && contract.Status == IntegrationContractVersionStatusEnum.Active);
            var sourceMapping = await Context.IntegrationMappingSets.AsNoTracking()
                .SingleAsync(mapping => mapping.MethodId == method.Id
                    && mapping.ContractVersionId == sourceContract.Id
                    && mapping.Status == IntegrationMappingSetStatusEnum.Published);
            var sourceRuleCount = await Context.IntegrationMappingRules.AsNoTracking()
                .CountAsync(rule => rule.MappingSetId == sourceMapping.Id);
            return new Setup(method, sourceContract, sourceMapping, sourceRuleCount);
        }

        public async Task<IntegrationMethodParameter> AddRequiredScalarAsync(Guid contractVersionId, string parameterPath)
        {
            var contract = await Context.IntegrationContractVersions.AsNoTracking()
                .SingleAsync(item => item.Id == contractVersionId);
            var maxSortOrder = await Context.IntegrationMethodParameters
                .Where(parameter => parameter.ContractVersionId == contractVersionId)
                .MaxAsync(parameter => parameter.SortOrder);

            var parameter = new IntegrationMethodParameter
            {
                MethodId = contract.MethodId,
                ContractVersionId = contractVersionId,
                ParameterPath = parameterPath,
                XmlElementName = parameterPath,
                DisplayName = "Tipo cliente de prueba",
                DescriptionEs = "Campo scalar required controlado para demostrar versionado no-code.",
                UiHelpText = "Solo fixture P4.5.",
                Category = "P4.5 Test",
                ExampleValue = "TX-REF-001",
                DataType = "string",
                XsdType = "xsd:string",
                Direction = IntegrationParameterDirectionEnum.Input,
                Cardinality = IntegrationParameterCardinalityEnum.Scalar,
                Required = true,
                Nillable = false,
                MinOccurs = 1,
                MaxOccurs = 1,
                SupportStatus = "Supported",
                SortOrder = checked(maxSortOrder + 10),
                IsActive = true
            };

            Context.IntegrationMethodParameters.Add(parameter);
            await Context.SaveChangesAsync();
            return parameter;
        }

        private async Task SeedRuntimeDataAsync()
        {
            Context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 1,
                HolidayStrategy = "Colombian"
            });

            Context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 10,
                Code = "ACH",
                Name = "ACH",
                OriginCode = "ORG",
                ClearingHouseId = 1
            });

            var source = new FinancialInstitution
            {
                Id = 1,
                Name = "Origen",
                RoutingNumber = "12345",
                TransitCode = "678",
                IsDefaultSource = true,
                Status = FinancialInstitutionStatus.Active
            };
            source.CalculateCheckDigit();

            var destination = new FinancialInstitution
            {
                Id = 2,
                Name = "Destino",
                RoutingNumber = "76543",
                TransitCode = "210",
                Status = FinancialInstitutionStatus.Active
            };
            destination.CalculateCheckDigit();
            Context.FinancialInstitutions.AddRange(source, destination);
            await Context.SaveChangesAsync();

            var clearingHouse = await Context.ClearingHouses.FirstAsync(item => item.Id == 10);
            Cycle = new AchCycle
            {
                Id = "CYCLE-P4-5-001",
                CycleName = "Cycle P4.5",
                ProcessingDate = DateTime.UtcNow.Date,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(17),
                CutoffTime = TimeSpan.FromHours(16),
                ClearingHouseId = 10,
                ClearingHouse = clearingHouse
            };

            var batch = new AchBatch
            {
                Id = 4500,
                AchCycleId = Cycle.Id,
                EffectiveEntryDate = Cycle.ProcessingDate,
                BatchSequenceNumber = 1,
                CompanyEntryDescriptionId = 1
            };

            Transaction = new AchTransaction
            {
                Id = 4501,
                TransactionExternalId = "TX-P4-5-001",
                AchCycleId = Cycle.Id,
                AchCycle = Cycle,
                AchBatch = batch,
                AchBatchId = batch.Id,
                Amount = 75.5m,
                Reference = "TX-REF-001",
                Type = TransactionTypeEnum.Debit,
                TransactionCode = "27",
                TraceNumber = "TRACE001",
                CompanyIdentification = "900",
                OriginatingDFI = "001",
                ReceivingDFI = "002",
                EffectiveEntryDate = Cycle.ProcessingDate,
                SourceInstitutionId = source.Id,
                DestinationInstitutionId = destination.Id,
                SourceAccountNumber = "123",
                DestinationAccountNumber = "456"
            };

            Context.AchCycles.Add(Cycle);
            Context.AchBatches.Add(batch);
            Context.AchTransactions.Add(Transaction);
            await Context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed record Setup(
        IntegrationMethod Method,
        IntegrationContractVersion SourceContract,
        IntegrationMappingSet SourceMapping,
        int SourceRuleCount);
}
