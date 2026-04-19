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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Mapping;

public class NachaFileBuilderHeaderMappingEngineTests
{
    [Fact]
    public async Task BuildNachaFileAsync_ShouldUseRecord1MappingEngine_WhenFlagEnabled()
    {
        var sut = CreateSut(enableRecord1: true, enableRecord5: false, shadowMode: false,
            out var loader, out var resolver, out var renderer, out var recordMappingEngine, out var planCompiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, planCompiler, recordMappingEngine, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        var content = await sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        content.Length.Should().BeGreaterThan(0);
        content.Length.Should().BeMultipleOf(106);
        recordMappingEngine.Verify(x => x.MapRecordAsync(It.Is<RecordMappingRequest>(r => r.RecordCode == "1"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldUseRecord5MappingEngine_WhenFlagEnabled()
    {
        var sut = CreateSut(enableRecord1: false, enableRecord5: true, shadowMode: false,
            out var loader, out var resolver, out var renderer, out var recordMappingEngine, out var planCompiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, planCompiler, recordMappingEngine, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        await sut.BuildNachaFileAsync([100], CancellationToken.None);

        recordMappingEngine.Verify(x => x.MapRecordAsync(It.Is<RecordMappingRequest>(r => r.RecordCode == "5"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldFallbackToLegacy_ForRecord1_WhenMappingFails()
    {
        var sut = CreateSut(enableRecord1: true, enableRecord5: false, shadowMode: false,
            out var loader, out var resolver, out var renderer, out var recordMappingEngine, out var planCompiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, planCompiler, recordMappingEngine, mappingSuccess: false);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        await sut.BuildNachaFileAsync([100], CancellationToken.None);

        renderer.Verify(x => x.RenderRecordAsync("1", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldFallbackToLegacy_ForRecord5_WhenMappingFails()
    {
        var sut = CreateSut(enableRecord1: false, enableRecord5: true, shadowMode: false,
            out var loader, out var resolver, out var renderer, out var recordMappingEngine, out var planCompiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, planCompiler, recordMappingEngine, mappingSuccess: false);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        await sut.BuildNachaFileAsync([100], CancellationToken.None);

        renderer.Verify(x => x.RenderRecordAsync("5", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldRunShadowCompare_ForRecord1_WhenModeShadowCompare()
    {
        var sut = CreateSut(enableRecord1: true, enableRecord5: false, shadowMode: true,
            out var loader, out var resolver, out var renderer, out var recordMappingEngine, out var planCompiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, planCompiler, recordMappingEngine, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        await sut.BuildNachaFileAsync([100], CancellationToken.None);

        renderer.Verify(x => x.RenderRecordAsync("1", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
        renderer.Verify(x => x.RenderRecordAsync("1", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldRunShadowCompare_ForRecord5_WhenModeShadowCompare()
    {
        var sut = CreateSut(enableRecord1: false, enableRecord5: true, shadowMode: true,
            out var loader, out var resolver, out var renderer, out var recordMappingEngine, out var planCompiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, planCompiler, recordMappingEngine, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        await sut.BuildNachaFileAsync([100], CancellationToken.None);

        renderer.Verify(x => x.RenderRecordAsync("5", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
        renderer.Verify(x => x.RenderRecordAsync("5", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
    }

    private static NachaFileBuilder CreateSut(
        bool enableRecord1,
        bool enableRecord5,
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
            EnableRecord1MappingEngine = enableRecord1,
            EnableRecord5MappingEngine = enableRecord5,
            Record6MappingDiagnostics = true
        });

        var dbOptions = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new Mock<AchDbContext>(dbOptions).Object;

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

    private static void SetupScenario(
        Mock<INachaDataLoader> loader,
        Mock<INachaConfigResolver> configResolver,
        Mock<INachaFixedWidthRecordRenderer> renderer,
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

        var layout1 = BuildLayoutVariant("1", "R1_FIELD", "ImmediateDestination");
        var layout5 = BuildLayoutVariant("5", "R5_FIELD", "CompanyName");

        configResolver.Setup(x => x.ResolveAsync(It.IsAny<NachaConfigResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaConfigResolutionResult
            {
                Success = true,
                Profile = new CfgProfile { Id = 1, ProfileCode = "P1" },
                LayoutsByRecordCode = new Dictionary<string, CfgLayoutVariant>(StringComparer.OrdinalIgnoreCase)
                {
                    ["1"] = layout1,
                    ["5"] = layout5
                },
                Trace = [],
                Warnings = []
            });

        planCompiler.Setup(x => x.CompileRecordPlan(It.IsAny<CfgLayoutVariant>(), It.IsAny<List<string>>()))
            .Returns((CfgLayoutVariant variant, List<string> _) => new RecordRuntimePlan
            {
                LayoutVariantId = variant.Id,
                RecordCode = variant.RecordCode?.Code ?? "1",
                TotalLength = variant.TotalLength,
                Fields =
                [
                    new FieldRuntimePlan
                    {
                        LayoutFieldId = 1,
                        RecordCode = variant.RecordCode?.Code ?? "1",
                        FieldCode = variant.Fields.First().FieldCode,
                        FieldNameEs = variant.Fields.First().FieldNameEs,
                        StartPosition = variant.Fields.First().StartPosition,
                        Length = variant.Fields.First().Length,
                        Justification = variant.Fields.First().Justification,
                        PadChar = variant.Fields.First().PadChar,
                        SourceTypeCode = "ENTIDAD",
                        PropertyPath = variant.Fields.First().SourceDefinition?.PropertyPath,
                        Rules = []
                    }
                ]
            });

        recordMappingEngine.Setup(x => x.MapRecordAsync(It.IsAny<RecordMappingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecordMappingRequest req, CancellationToken _) =>
            {
                var fieldCode = req.RecordCode == "5" ? "R5_FIELD" : "R1_FIELD";
                return new RecordMappingResult
                {
                    Success = mappingSuccess,
                    ValuesByFieldCode = mappingSuccess ? new Dictionary<string, object?> { [fieldCode] = "VALUE" } : new Dictionary<string, object?>(),
                    FieldTraces =
                    [
                        new FieldTrace
                        {
                            FieldCode = fieldCode,
                            SourceUsed = "ENTITY",
                            CanonicalKey = "Canonical",
                            FinalValue = "VALUE"
                        }
                    ]
                };
            });
    }

    private static Dictionary<string, NachaRecordLayout> BuildLayouts()
    {
        return new Dictionary<string, NachaRecordLayout>
        {
            ["1"] = new NachaRecordLayout { RecordCode = "1", TotalLength = 106, Fields = [new NachaRecordField { FieldName = "ImmediateDestination", DbColumn = "ImmediateDestination", StartPosition = 4, Length = 10, Justification = 'R', PadChar = ' ' }] },
            ["5"] = new NachaRecordLayout { RecordCode = "5", TotalLength = 106, Fields = [new NachaRecordField { FieldName = "CompanyName", DbColumn = "CompanyName", StartPosition = 5, Length = 16, Justification = 'L', PadChar = ' ' }] },
            ["9"] = new NachaRecordLayout { RecordCode = "9", TotalLength = 106, Fields = [] }
        };
    }

    private static List<NachaRecordDefinition> BuildDefinitions()
    {
        return
        [
            new NachaRecordDefinition { RecordCode = "1", Sequence = 10, IsEnabled = true, SourceType = NachaRecordSourceType.Custom },
            new NachaRecordDefinition { RecordCode = "5", Sequence = 20, IsEnabled = true, SourceType = NachaRecordSourceType.Custom },
            new NachaRecordDefinition { RecordCode = "9", Sequence = 30, IsEnabled = true, SourceType = NachaRecordSourceType.Custom }
        ];
    }

    private static CfgLayoutVariant BuildLayoutVariant(string recordCode, string fieldCode, string propertyPath)
    {
        return new CfgLayoutVariant
        {
            Id = recordCode == "1" ? 910 : 920,
            VariantCode = $"R{recordCode}_PHASE1",
            TotalLength = 106,
            RecordCode = new CatRecordCode { Code = recordCode, NameEs = $"Record {recordCode}" },
            Fields =
            [
                new CfgLayoutField
                {
                    Id = recordCode == "1" ? 911 : 921,
                    FieldCode = fieldCode,
                    FieldNameEs = fieldCode,
                    StartPosition = 2,
                    Length = 12,
                    Justification = 'L',
                    PadChar = ' ',
                    IsEnabled = true,
                    SourceDefinition = new CfgFieldSourceDefinition
                    {
                        DataSourceType = new CatDataSourceType { Code = "ENTIDAD", NameEs = "Entidad" },
                        PropertyPath = propertyPath
                    },
                    Rules = []
                }
            ]
        };
    }
}
