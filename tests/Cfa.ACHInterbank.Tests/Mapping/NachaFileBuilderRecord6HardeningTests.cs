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

public class NachaFileBuilderRecord6HardeningTests
{
    [Fact]
    public async Task BuildNachaFileAsync_ShouldUseRecord6MappingEngine_WhenFlagEnabled()
    {
        var sut = CreateSut(enableRecord6: true, shadowMode: false,
            out var loader,
            out var configResolver,
            out var renderer,
            out var recordMappingEngine,
            out var planCompiler,
            out var semanticValidator,
            out var validationService);

        SetupScenario(loader, renderer, configResolver, planCompiler, recordMappingEngine, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        var content = await sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        recordMappingEngine.Verify(x => x.MapRecordAsync(It.Is<RecordMappingRequest>(r => r.RecordCode == "6"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldFallbackToLegacy_ForRecord6_WhenMappingEngineFails()
    {
        var sut = CreateSut(enableRecord6: true, shadowMode: false,
            out var loader,
            out var configResolver,
            out var renderer,
            out var recordMappingEngine,
            out var planCompiler,
            out var semanticValidator,
            out var validationService);

        SetupScenario(loader, renderer, configResolver, planCompiler, recordMappingEngine, mappingSuccess: false);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        var content = await sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        renderer.Verify(x => x.RenderRecordAsync("6", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldRunShadowCompare_ForRecord6_WhenModeShadowCompare()
    {
        var sut = CreateSut(enableRecord6: true, shadowMode: true,
            out var loader,
            out var configResolver,
            out var renderer,
            out var recordMappingEngine,
            out var planCompiler,
            out var semanticValidator,
            out var validationService);

        SetupScenario(loader, renderer, configResolver, planCompiler, recordMappingEngine, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        await sut.BuildNachaFileAsync([100], CancellationToken.None);

        renderer.Verify(x => x.RenderRecordAsync("6", It.IsAny<Dictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
        renderer.Verify(x => x.RenderRecordAsync("6", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
    }

    private static NachaFileBuilder CreateSut(
        bool enableRecord6,
        bool shadowMode,
        out Mock<INachaDataLoader> loader,
        out Mock<INachaConfigResolver> configResolver,
        out Mock<INachaFixedWidthRecordRenderer> renderer,
        out Mock<INachaRecordMappingEngine> recordMappingEngine,
        out Mock<IFieldMappingPlanCompiler> planCompiler,
        out Mock<INachaSemanticValidator> semanticValidator,
        out Mock<INachaTransactionValidationService> validationService)
    {
        loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        configResolver = new Mock<INachaConfigResolver>(MockBehavior.Strict);
        renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        recordMappingEngine = new Mock<INachaRecordMappingEngine>(MockBehavior.Strict);
        planCompiler = new Mock<IFieldMappingPlanCompiler>(MockBehavior.Strict);
        semanticValidator = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        validationService = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);
        var recordProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Loose);
        var holiday = new Mock<IBankHoliday>(MockBehavior.Loose);

        var options = Options.Create(new NachaGenerationOptions
        {
            Mode = shadowMode ? "SHADOW_COMPARE" : "HYBRID",
            ExecutionScope = "DEVELOPMENT",
            EnableRecord6MappingEngine = enableRecord6,
            Record6MappingDiagnostics = true
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
            null,
            null,
            null,
            recordMappingEngine.Object,
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
        Mock<INachaFixedWidthRecordRenderer> renderer,
        Mock<INachaConfigResolver> configResolver,
        Mock<IFieldMappingPlanCompiler> planCompiler,
        Mock<INachaRecordMappingEngine> recordMappingEngine,
        bool mappingSuccess)
    {
        var cycle = new AchCycle
        {
            Id = "cycle-1",
            CycleName = "C40",
            ProcessingDate = DateTime.UtcNow,
            ClearingHouse = new ClearingHouse { Name = "ACH Colombia", OriginCode = "12345678" }
        };

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
            Addendas = []
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
        loader.Setup(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(BuildLayouts());
        loader.Setup(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(BuildDefinitions());
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<(string Term, string StandardEntryClassCode)> { ("PAGOS", "PPD") });

        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string recordType, object _, NachaRecordLayout _) => new string(recordType[0], 106));
        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string recordType, IReadOnlyDictionary<string, object?> _, NachaRecordLayout _) => new string(recordType[0], 106));

        var layoutVariant = BuildLayoutVariant();
        configResolver.Setup(x => x.ResolveAsync(It.IsAny<NachaConfigResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaConfigResolutionResult
            {
                Success = true,
                Profile = new CfgProfile { Id = 1, ProfileCode = "P1" },
                LayoutsByRecordCode = new Dictionary<string, CfgLayoutVariant>(StringComparer.OrdinalIgnoreCase)
                {
                    ["6"] = layoutVariant
                },
                Trace = [],
                Warnings = []
            });

        planCompiler.Setup(x => x.CompileRecordPlan(layoutVariant, It.IsAny<List<string>>()))
            .Returns(new RecordRuntimePlan
            {
                LayoutVariantId = layoutVariant.Id,
                RecordCode = "6",
                TotalLength = layoutVariant.TotalLength,
                Fields =
                [
                    new FieldRuntimePlan
                    {
                        LayoutFieldId = 1,
                        RecordCode = "6",
                        FieldCode = "R6_TRACE",
                        FieldNameEs = "Trace",
                        StartPosition = 80,
                        Length = 15,
                        Justification = 'R',
                        PadChar = '0',
                        SourceTypeCode = "ENTIDAD",
                        PropertyPath = "TraceNumber",
                        Rules = []
                    }
                ]
            });

        recordMappingEngine.Setup(x => x.MapRecordAsync(It.IsAny<RecordMappingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordMappingResult
            {
                Success = mappingSuccess,
                ValuesByFieldCode = mappingSuccess
                    ? new Dictionary<string, object?> { ["R6_TRACE"] = "000000000000001" }
                    : new Dictionary<string, object?>(),
                FieldTraces =
                [
                    new FieldTrace
                    {
                        FieldCode = "R6_TRACE",
                        SourceUsed = "ENTITY:TraceNumber",
                        CanonicalKey = "TraceNumber",
                        RawValue = "000000000000001",
                        TransformedValue = "000000000000001",
                        FinalValue = "000000000000001",
                        FallbackStrategy = mappingSuccess ? "NONE" : "DEFAULT"
                    }
                ],
                Warnings = mappingSuccess ? [] : ["FAIL"]
            });
    }

    private static Dictionary<string, NachaRecordLayout> BuildLayouts()
    {
        return new Dictionary<string, NachaRecordLayout>
        {
            ["1"] = new NachaRecordLayout { RecordCode = "1", TotalLength = 106, Fields = [] },
            ["6"] = new NachaRecordLayout { RecordCode = "6", TotalLength = 106, Fields = [new NachaRecordField { FieldName = "TraceNumber", DbColumn = "TraceNumber", StartPosition = 80, Length = 15, Justification = 'R', PadChar = '0' }] },
            ["9"] = new NachaRecordLayout { RecordCode = "9", TotalLength = 106, Fields = [] }
        };
    }

    private static List<NachaRecordDefinition> BuildDefinitions()
    {
        return
        [
            new NachaRecordDefinition { RecordCode = "1", Sequence = 10, IsEnabled = true, SourceType = NachaRecordSourceType.Custom },
            new NachaRecordDefinition { RecordCode = "6", Sequence = 20, IsEnabled = true, SourceType = NachaRecordSourceType.Custom },
            new NachaRecordDefinition { RecordCode = "9", Sequence = 30, IsEnabled = true, SourceType = NachaRecordSourceType.Custom }
        ];
    }

    private static CfgLayoutVariant BuildLayoutVariant()
    {
        return new CfgLayoutVariant
        {
            Id = 900,
            VariantCode = "R6_PHASE1",
            TotalLength = 106,
            RecordCode = new CatRecordCode { Code = "6", NameEs = "Detalle" },
            Fields =
            [
                new CfgLayoutField
                {
                    Id = 901,
                    FieldCode = "R6_TRACE",
                    FieldNameEs = "Trace",
                    StartPosition = 80,
                    Length = 15,
                    Justification = 'R',
                    PadChar = '0',
                    IsEnabled = true,
                    SourceDefinition = new CfgFieldSourceDefinition
                    {
                        DataSourceType = new CatDataSourceType { Code = "ENTIDAD", NameEs = "Entidad" },
                        PropertyPath = "TraceNumber"
                    },
                    Rules = []
                }
            ]
        };
    }
}
