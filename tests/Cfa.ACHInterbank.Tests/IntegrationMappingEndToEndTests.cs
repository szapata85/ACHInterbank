using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IntegrationMappingEndToEndTests
{
    [Fact]
    public async Task Catalog_Methods_Available()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var methods = await fixture.Catalog.GetMethodsAsync();
        Assert.Contains(methods, x => x.Code == "WSCFAACH.Proc_Contrapartidas");
    }

    [Fact]
    public async Task Catalog_Parameters_Available_ByMethod()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var parameters = await fixture.Catalog.GetMethodParametersAsync(fixture.MethodId);
        Assert.Contains(parameters, x => x.ParameterPath == "CycleId");
        Assert.Contains(parameters, x => x.ParameterPath == "Transactions[].Reference");
    }

    [Fact]
    public async Task Catalog_SourceFields_Available_ByMethod()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var source = await fixture.Catalog.GetSourceCatalogAsync(fixture.MethodId);
        Assert.Contains(source, x => x.FieldPath == "transaction.reference");
        Assert.Contains(source, x => x.FieldPath == "addenda.information");
        Assert.Contains(source, x => x.FieldPath == "cycle.id");
    }

    [Fact]
    public async Task MappingSet_CanCreateAndEditDraft()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var created = await fixture.MappingSetService.CreateDraftAsync(new CreateIntegrationMappingSetRequest(fixture.MethodId, "Draft A", "notes", "tester"));
        var updated = await fixture.MappingSetService.UpdateDraftAsync(created.Id, new UpdateIntegrationMappingSetRequest("Draft B", "notes2", true, "tester"));

        Assert.Equal("Draft B", updated.Name);
    }

    [Fact]
    public async Task MappingSet_BulkUpsertRules_Works()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var created = await fixture.CreateDraftWithRulesAsync("BulkUpsert");
        Assert.NotEmpty(created.Rules);
    }

    [Fact]
    public async Task MappingSet_Validation_DetectsErrors_AndCoverage()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.MappingSetService.CreateDraftAsync(new CreateIntegrationMappingSetRequest(fixture.MethodId, "Invalid", "", "tester"));

        var required = fixture.Parameters.First(p => p.Required);
        await fixture.MappingSetService.UpsertRulesAsync(draft.Id, new UpsertIntegrationMappingRulesRequest("tester", [
            new UpsertIntegrationMappingRuleRequest(null, fixture.MethodId, required.Id, IntegrationSourceKindEnum.Transaction, null, "", null, null, "NotAllowed", null, 1, null, false, null)
        ]));

        var validation = await fixture.MappingSetService.ValidateAsync(draft.Id, new ValidateIntegrationMappingSetRequest(true));

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, i => i.Code is "TRANSFORMATION_INVALID" or "SOURCE_NOT_DEFINED");
        Assert.True(validation.Coverage.IncompleteParameters >= 1 || validation.Coverage.InvalidParameters >= 1);
    }

    [Fact]
    public async Task MappingSet_Preview_ReturnsItemsAndPayload()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.CreateDraftWithRulesAsync("Preview");

        var preview = await fixture.MappingSetService.PreviewAsync(draft.Id, new PreviewIntegrationMappingSetRequest(UseControlledSample: true, MaxItems: 20));
        Assert.NotEmpty(preview.Items);
        Assert.Contains("{", preview.PayloadPreviewJson);
    }

    [Fact]
    public async Task MappingSet_Publish_RequiresValidConfiguration()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.CreateDraftWithRulesAsync("Publish");

        var published = await fixture.MappingSetService.PublishAsync(draft.Id, new PublishIntegrationMappingSetRequest("tester", "ok"));
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, published.Status);
    }

    [Fact]
    public async Task MappingSet_Clone_Works()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.CreateDraftWithRulesAsync("CloneSource");
        var clone = await fixture.MappingSetService.CloneAsync(draft.Id, new CloneIntegrationMappingSetRequest("CloneResult", "tester"));
        Assert.Equal("CloneResult", clone.Name);
        Assert.NotEmpty(clone.Rules);
    }

    [Fact]
    public async Task Resolver_UsesPublishedDynamicMapping()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var published = await fixture.CreateAndPublishResolverMappingAsync();

        var contract = await fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow);

        Assert.NotNull(contract);
        Assert.NotNull(contract!.Contract);
        Assert.Equal(fixture.Transaction.TransactionExternalId, contract.Contract.OFIDTX);
        Assert.False(string.IsNullOrWhiteSpace(contract.Contract.OFFECHEFEC));
    }

    [Fact]
    public async Task Resolver_ReturnsNull_WhenNoPublishedMapping_AllowingHybridFallback()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var result = await fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow);
        Assert.Null(result);
    }

    [Fact]
    public async Task PublishedMappingMetadata_HasIdVersionAndSnapshotHash()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var published = await fixture.CreateAndPublishResolverMappingAsync();

        var history = await fixture.MappingSetService.GetHistoryAsync(published.Id);
        Assert.Contains(history, x => x.SnapshotHash.Length > 10);
        Assert.True(published.Version > 0);
        Assert.NotEqual(Guid.Empty, published.Id);
    }

    [Fact]
    public async Task MappingSet_Compare_ReturnsRuleDiffsAndMetadata()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var left = await fixture.CreateDraftWithRulesAsync("Compare Left");
        var right = await fixture.CreateDraftWithRulesAsync("Compare Right");

        var firstRule = right.Rules.First();
        await fixture.MappingSetService.UpsertRulesAsync(
            right.Id,
            new UpsertIntegrationMappingRulesRequest("tester", [
                new UpsertIntegrationMappingRuleRequest(firstRule.Id, fixture.MethodId, firstRule.ParameterId, firstRule.SourceKind, firstRule.SourceCatalogFieldId, firstRule.SourceFieldPath, firstRule.FixedValue, "NEW-DEFAULT", firstRule.TransformationCode, firstRule.FormatMask, firstRule.Priority + 1, firstRule.RequiredOverride, firstRule.Enabled, firstRule.ConditionExpression)
            ]));

        var compare = await fixture.MappingSetService.CompareAsync(new CompareIntegrationMappingSetsRequest(left.Id, right.Id));

        Assert.Equal(left.Id, compare.Left.MappingSetId);
        Assert.Equal(right.Id, compare.Right.MappingSetId);
        Assert.Contains(compare.Rules, x => x.ChangeType is "Modified" or "Equal");
    }

    private sealed class IntegrationFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AchDbContext Context { get; }
        public IntegrationCatalogService Catalog { get; }
        public IntegrationMappingSetService MappingSetService { get; }
        public ProcContrapartidasFunctionalMappingResolver Resolver { get; }
        public int MethodId { get; private set; }
        public List<IntegrationMethodParameter> Parameters { get; private set; } = [];
        public AchCycle Cycle { get; private set; } = null!;
        public AchTransaction Transaction { get; private set; } = null!;

        private IntegrationFixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            Catalog = new IntegrationCatalogService(context);
            var validation = new IntegrationMappingValidationService(context);
            var preview = new IntegrationMappingPreviewService(context);
            MappingSetService = new IntegrationMappingSetService(context, validation, preview);
            Resolver = new ProcContrapartidasFunctionalMappingResolver(context);
        }

        public static async Task<IntegrationFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var fixture = new IntegrationFixture(connection, context);
            await fixture.SeedAsync();
            return fixture;
        }

        public async Task SeedAsync()
        {
            await new IntegrationMappingScenarioSeeder(Context, new TestingHostEnvironment()).SeedAsync();

            await Catalog.GetMethodsAsync();
            var methods = await Catalog.GetMethodsAsync();
            MethodId = methods.First(x => x.Code == "WSCFAACH.Proc_Contrapartidas").Id;
            Parameters = await Context.IntegrationMethodParameters.Where(x => x.MethodId == MethodId).ToListAsync();

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

            var clearingHouse = await Context.ClearingHouses.FirstAsync(x => x.Id == 10);

            Cycle = new AchCycle
            {
                Id = "CYCLE-T-001",
                CycleName = "Cycle T",
                ProcessingDate = DateTime.UtcNow.Date,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(17),
                CutoffTime = TimeSpan.FromHours(16),
                ClearingHouseId = 10,
                ClearingHouse = clearingHouse
            };

            var batch = new AchBatch { Id = 999, AchCycleId = Cycle.Id, EffectiveEntryDate = Cycle.ProcessingDate, BatchSequenceNumber = 1, CompanyEntryDescriptionId = 1 };
            Transaction = new AchTransaction
            {
                Id = 1001,
                AchCycleId = Cycle.Id,
                AchCycle = Cycle,
                AchBatch = batch,
                AchBatchId = batch.Id,
                Amount = 75.5m,
                Reference = "TX-REF-001",
                Type = TransactionTypeEnum.Credit,
                TransactionCode = "22",
                TraceNumber = "TRACE001",
                CompanyIdentification = "900",
                OriginatingDFI = "001",
                ReceivingDFI = "002",
                EffectiveEntryDate = DateTime.UtcNow.Date,
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2,
                SourceAccountNumber = "123",
                DestinationAccountNumber = "456",
                Addendas = [new AchTransactionAddenda { AddendaType = "05", Information = "INFO", SequenceNumber = 1, BusinessType = AchAddendaBusinessType.Credit }]
            };

            Context.AchCycles.Add(Cycle);
            Context.AchBatches.Add(batch);
            Context.AchTransactions.Add(Transaction);
            await Context.SaveChangesAsync();
        }

        public async Task<IntegrationMappingSetDto> CreateDraftWithRulesAsync(string name)
        {
            var draft = await MappingSetService.CreateDraftAsync(new CreateIntegrationMappingSetRequest(MethodId, name, "", "tester"));
            var rules = Parameters
                .Where(x => x.Required)
                .Select(p => new UpsertIntegrationMappingRuleRequest(null, MethodId, p.Id, IntegrationSourceKindEnum.Constant, null, null, DefaultFor(p), null, null, null, 1, null, true, null))
                .ToArray();

            return await MappingSetService.UpsertRulesAsync(draft.Id, new UpsertIntegrationMappingRulesRequest("tester", rules));
        }

        public async Task<IntegrationMappingSetDto> CreateAndPublishResolverMappingAsync()
        {
            var draft = await MappingSetService.CreateDraftAsync(new CreateIntegrationMappingSetRequest(MethodId, "Resolver", "", "tester"));

            var rules = new List<UpsertIntegrationMappingRuleRequest>();
            foreach (var parameter in Parameters.Where(x => x.Required))
            {
                var (kind, path, fixedVal) = parameter.ParameterPath switch
                {
                    "CycleId" => (IntegrationSourceKindEnum.Cycle, "cycle.id", (string?)null),
                    "CycleName" => (IntegrationSourceKindEnum.Cycle, "cycle.cycleName", (string?)null),
                    "ClearingHouseId" => (IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.id", (string?)null),
                    "ClearingHouseCode" => (IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.code", (string?)null),
                    "Transactions[].Reference" => (IntegrationSourceKindEnum.Transaction, "transaction.reference", (string?)null),
                    "Transactions[].TransactionId" => (IntegrationSourceKindEnum.Transaction, "transaction.id", (string?)null),
                    "Transactions[].AchBatchId" => (IntegrationSourceKindEnum.Batch, "batch.id", (string?)null),
                    "Transactions[].Amount" => (IntegrationSourceKindEnum.Transaction, "transaction.amount", (string?)null),
                    "Transactions[].Addendas[].AddendaType" => (IntegrationSourceKindEnum.Addenda, "addenda.addendaType", (string?)null),
                    _ => (IntegrationSourceKindEnum.Constant, string.Empty, DefaultFor(parameter))
                };

                rules.Add(new UpsertIntegrationMappingRuleRequest(null, MethodId, parameter.Id, kind, null, path, fixedVal, DefaultFor(parameter), null, null, 1, null, true, null));
            }

            await MappingSetService.UpsertRulesAsync(draft.Id, new UpsertIntegrationMappingRulesRequest("tester", rules));
            return await MappingSetService.PublishAsync(draft.Id, new PublishIntegrationMappingSetRequest("tester"));
        }

        private static string DefaultFor(IntegrationMethodParameter parameter)
            => parameter.DataType.ToLowerInvariant() switch
            {
                "int" or "long" => "1",
                "decimal" or "double" or "float" => "1.00",
                "datetime" => DateTime.UtcNow.ToString("O"),
                "timespan" => "08:00:00",
                _ => "TEST"
            };

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestingHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Cfa.ACHInterbank.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
