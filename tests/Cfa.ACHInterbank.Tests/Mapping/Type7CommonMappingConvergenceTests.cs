using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Mapping;

public class Type7CommonMappingConvergenceTests
{
    [Fact]
    public async Task BuildNachaFileAsync_ShouldUseCommonMappingEngine_ForType7_WhenEnabled()
    {
        var sut = CreateSut(out var loader, out var configResolver, out var renderer, out var mappingEngine, out var planCompiler, out var strategy, out var semanticValidator, out var validationService);
        SetupScenario(loader, configResolver, renderer, mappingEngine, planCompiler, strategy, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        var content = await sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        mappingEngine.Verify(x => x.MapRecordAsync(It.Is<RecordMappingRequest>(r => r.RecordCode == "7"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldFallbackLegacyType7_WhenCommonMappingFails()
    {
        var sut = CreateSut(out var loader, out var configResolver, out var renderer, out var mappingEngine, out var planCompiler, out var strategy, out var semanticValidator, out var validationService);
        SetupScenario(loader, configResolver, renderer, mappingEngine, planCompiler, strategy, mappingSuccess: false);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        var content = await sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        mappingEngine.Verify(x => x.MapRecordAsync(It.Is<RecordMappingRequest>(r => r.RecordCode == "7"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        content.Should().Contain("7");
    }

    [Fact]
    public void CanonicalMapper_ShouldResolveType7LegacyAliases()
    {
        var sut = new NachaCanonicalMapper();

        sut.ResolveCanonicalKey("7", "TipoAddenda").Should().Be("AddendaType");
        sut.ResolveCanonicalKey("7", "NumeroTraceOriginal").Should().Be("OriginalTraceNumber");
    }

    private static NachaFileBuilder CreateSut(
        out Mock<INachaDataLoader> loader,
        out Mock<INachaConfigResolver> configResolver,
        out Mock<INachaFixedWidthRecordRenderer> renderer,
        out Mock<INachaRecordMappingEngine> mappingEngine,
        out Mock<IFieldMappingPlanCompiler> planCompiler,
        out Mock<INachaType7GenerationStrategy> strategy,
        out Mock<INachaSemanticValidator> semanticValidator,
        out Mock<INachaTransactionValidationService> validationService)
    {
        loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        configResolver = new Mock<INachaConfigResolver>(MockBehavior.Strict);
        renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        mappingEngine = new Mock<INachaRecordMappingEngine>(MockBehavior.Strict);
        planCompiler = new Mock<IFieldMappingPlanCompiler>(MockBehavior.Strict);
        strategy = new Mock<INachaType7GenerationStrategy>(MockBehavior.Strict);
        semanticValidator = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        validationService = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);

        var recordProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Loose);
        var holiday = new Mock<IBankHoliday>(MockBehavior.Loose);

        var options = Options.Create(new NachaGenerationOptions
        {
            Mode = "SHADOW_COMPARE",
            ExecutionScope = "DEVELOPMENT",
            EnableType7TableDriven = true,
            EnableType7CommonMappingEngine = true,
            Type7EnableTableDrivenForClearingHouses = ["ACH"]
        });

        var dbOptions = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(CreateOpenConnection())
            .Options;
        var db = new AchDbContext(dbOptions);
        db.Database.EnsureCreated();

        return new NachaFileBuilder(
            db,
            holiday.Object,
            loader.Object,
            validationService.Object,
            renderer.Object,
            recordProvider.Object,
            semanticValidator.Object,
            configResolver.Object,
            null,
            strategy.Object,
            null,
            null,
            mappingEngine.Object,
            planCompiler.Object,
            options);
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static void SetupScenario(
        Mock<INachaDataLoader> loader,
        Mock<INachaConfigResolver> configResolver,
        Mock<INachaFixedWidthRecordRenderer> renderer,
        Mock<INachaRecordMappingEngine> mappingEngine,
        Mock<IFieldMappingPlanCompiler> planCompiler,
        Mock<INachaType7GenerationStrategy> strategy,
        bool mappingSuccess)
    {
        var cycle = new AchCycle
        {
            Id = "cycle-t7",
            CycleName = "C40",
            ProcessingDate = DateTime.UtcNow,
            ClearingHouse = new ClearingHouse { Name = "ACH Colombia", OriginCode = "12345678" }
        };

        var addenda = new AchTransactionAddenda { AddendaType = "05", Purpose = "PAGOS", SequenceNumber = 1 };
        var tx = new AchTransaction
        {
            Id = 10,
            Type = TransactionTypeEnum.Credit,
            Amount = 100m,
            AchBatchId = 100,
            AchCycleId = cycle.Id,
            TransactionCode = "22",
            ReceivingDFI = "12345678",
            TraceNumber = "000000000000001",
            EffectiveEntryDate = DateTime.UtcNow,
            DestinationAccountNumber = "123456789",
            RecipientIdNumber = "RCV001",
            CompanyIdentification = "1234567890",
            Addendas = [addenda]
        };

        var batch = new AchBatch
        {
            Id = 100,
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            CompanyEntryDescription = "PAGOS",
            CompanyIdentification = "1234567890",
            CompanyName = "COMPANY",
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = DateTime.UtcNow,
            Transactions = [tx]
        };

        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync([batch]);
        loader.Setup(x => x.LoadHeaderAsync(cycle.Id, It.IsAny<CancellationToken>())).ReturnsAsync((NachaHeader?)null);
        loader.Setup(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, NachaRecordLayout>
        {
            ["1"] = new NachaRecordLayout { RecordCode = "1", TotalLength = 106, Fields = [] },
            ["7"] = new NachaRecordLayout { RecordCode = "7", TotalLength = 106, Fields = [new NachaRecordField { FieldName = "AddendaType", DbColumn = "AddendaType", StartPosition = 2, Length = 2, Justification = 'L', PadChar = ' ' }] },
            ["9"] = new NachaRecordLayout { RecordCode = "9", TotalLength = 106, Fields = [] }
        });
        loader.Setup(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new NachaRecordDefinition { RecordCode = "1", Sequence = 10, IsEnabled = true, SourceType = NachaRecordSourceType.Custom },
            new NachaRecordDefinition { RecordCode = "7", Sequence = 20, IsEnabled = true, SourceType = NachaRecordSourceType.Custom },
            new NachaRecordDefinition { RecordCode = "9", Sequence = 30, IsEnabled = true, SourceType = NachaRecordSourceType.Custom }
        ]);
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<(string Term, string StandardEntryClassCode)> { ("PAGOS", "PPD") });

        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string recordType, object _, NachaRecordLayout _) => new string(recordType[0], 106));
        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string recordType, IReadOnlyDictionary<string, object?> _, NachaRecordLayout _) => new string(recordType[0], 106));

        var layoutVariant = new CfgLayoutVariant
        {
            Id = 700,
            VariantCode = "R7_COMMON",
            TotalLength = 106,
            RecordCode = new CatRecordCode { Code = "7", NameEs = "Addenda" },
            Fields =
            [
                new CfgLayoutField
                {
                    Id = 701,
                    FieldCode = "R7_ADDENDA_TYPE",
                    FieldNameEs = "Tipo",
                    StartPosition = 2,
                    Length = 2,
                    Justification = 'L',
                    PadChar = ' ',
                    IsEnabled = true,
                    SourceDefinition = new CfgFieldSourceDefinition
                    {
                        DataSourceType = new CatDataSourceType { Code = "ENTIDAD", NameEs = "Entidad" },
                        PropertyPath = "AddendaType"
                    },
                    Rules = []
                }
            ]
        };

        configResolver.Setup(x => x.ResolveAsync(It.IsAny<NachaConfigResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaConfigResolutionResult
            {
                Success = true,
                Profile = new CfgProfile { Id = 1, ProfileCode = "P1" },
                LayoutsByRecordCode = new Dictionary<string, CfgLayoutVariant>(StringComparer.OrdinalIgnoreCase)
                {
                    ["7"] = layoutVariant
                },
                Trace = [],
                Warnings = []
            });

        strategy.Setup(x => x.BuildCandidates(It.IsAny<IReadOnlyList<AchBatch>>())).Returns(
        [
            new NachaType7RecordCandidate
            {
                Batch = batch,
                Transaction = tx,
                Addenda = addenda,
                FieldValues = new Dictionary<string, object?>
                {
                    ["AddendaType"] = "05",
                    ["Purpose"] = "PAGO"
                }
            }
        ]);

        planCompiler.Setup(x => x.CompileRecordPlan(layoutVariant, It.IsAny<List<string>>())).Returns(new RecordRuntimePlan
        {
            LayoutVariantId = 700,
            RecordCode = "7",
            TotalLength = 106,
            Fields =
            [
                new FieldRuntimePlan
                {
                    LayoutFieldId = 1,
                    RecordCode = "7",
                    FieldCode = "R7_ADDENDA_TYPE",
                    FieldNameEs = "Tipo",
                    StartPosition = 2,
                    Length = 2,
                    Justification = 'L',
                    PadChar = ' ',
                    SourceTypeCode = "ENTIDAD",
                    PropertyPath = "AddendaType",
                    Rules = []
                }
            ]
        });

        mappingEngine.Setup(x => x.MapRecordAsync(It.IsAny<RecordMappingRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new RecordMappingResult
        {
            Success = mappingSuccess,
            ValuesByFieldCode = mappingSuccess ? new Dictionary<string, object?> { ["R7_ADDENDA_TYPE"] = "05" } : new Dictionary<string, object?>(),
            FieldTraces =
            [
                new FieldTrace { FieldCode = "R7_ADDENDA_TYPE", SourceUsed = "DICT:AddendaType", CanonicalKey = "AddendaType", RawValue = "05", TransformedValue = "05", FinalValue = "05", FallbackStrategy = mappingSuccess ? "NONE" : "DEFAULT" }
            ],
            Warnings = mappingSuccess ? [] : ["FAIL"]
        });
    }
}
