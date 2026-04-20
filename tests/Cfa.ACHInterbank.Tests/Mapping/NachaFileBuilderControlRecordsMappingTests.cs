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

public class NachaFileBuilderControlRecordsMappingTests
{
    [Fact]
    public async Task BuildNachaFileAsync_ShouldUseMappingEngine_ForRecord8And9_WhenFlagsEnabled()
    {
        var sut = CreateSut(enableRecord8: true, enableRecord9: true, shadowMode: false,
            out var loader, out var resolver, out var renderer, out var mappingEngine, out var compiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, compiler, mappingEngine, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        var content = await sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        mappingEngine.Verify(x => x.MapRecordAsync(It.Is<RecordMappingRequest>(r => r.RecordCode == "8"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mappingEngine.Verify(x => x.MapRecordAsync(It.Is<RecordMappingRequest>(r => r.RecordCode == "9"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldFallbackLegacy_ForRecord8And9_WhenMappingFails()
    {
        var sut = CreateSut(enableRecord8: true, enableRecord9: true, shadowMode: false,
            out var loader, out var resolver, out var renderer, out var mappingEngine, out var compiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, compiler, mappingEngine, mappingSuccess: false);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        await sut.BuildNachaFileAsync([100], CancellationToken.None);

        renderer.Verify(x => x.RenderRecordAsync("8", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
        renderer.Verify(x => x.RenderRecordAsync("9", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldRunShadowCompare_ForRecord8And9()
    {
        var sut = CreateSut(enableRecord8: true, enableRecord9: true, shadowMode: true,
            out var loader, out var resolver, out var renderer, out var mappingEngine, out var compiler, out var semanticValidator, out var validationService);
        SetupScenario(loader, resolver, renderer, compiler, mappingEngine, mappingSuccess: true);
        validationService.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        await sut.BuildNachaFileAsync([100], CancellationToken.None);

        renderer.Verify(x => x.RenderRecordAsync("8", It.IsAny<Dictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
        renderer.Verify(x => x.RenderRecordAsync("9", It.IsAny<Dictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()), Times.AtLeastOnce);
    }

    private static NachaFileBuilder CreateSut(
        bool enableRecord8,
        bool enableRecord9,
        bool shadowMode,
        out Mock<INachaDataLoader> loader,
        out Mock<INachaConfigResolver> resolver,
        out Mock<INachaFixedWidthRecordRenderer> renderer,
        out Mock<INachaRecordMappingEngine> mappingEngine,
        out Mock<IFieldMappingPlanCompiler> compiler,
        out Mock<INachaSemanticValidator> semanticValidator,
        out Mock<INachaTransactionValidationService> validationService)
    {
        loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        resolver = new Mock<INachaConfigResolver>(MockBehavior.Strict);
        renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        mappingEngine = new Mock<INachaRecordMappingEngine>(MockBehavior.Strict);
        compiler = new Mock<IFieldMappingPlanCompiler>(MockBehavior.Strict);
        semanticValidator = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        validationService = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);
        var recordProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Loose);
        var holiday = new Mock<IBankHoliday>(MockBehavior.Loose);
        var batchNumberGenerator = new Mock<IBatchNumberGenerator>(MockBehavior.Strict);
        batchNumberGenerator.Setup(x => x.AssignBatchNumbersAsync(It.IsAny<IReadOnlyList<AchBatch>>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AchBatch> batches, string _, DateTime _, CancellationToken _) => new BatchNumberAssignmentResult(
                batches.ToDictionary(b => b.Id, b => 1),
                "DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI",
                1,
                []));

        var options = Options.Create(new NachaGenerationOptions
        {
            Mode = shadowMode ? "SHADOW_COMPARE" : "HYBRID",
            EnableRecord8MappingEngine = enableRecord8,
            EnableRecord9MappingEngine = enableRecord9
        });

        var dbOptions = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(CreateOpenConnection()).Options;
        var db = new AchDbContext(dbOptions);
        db.Database.EnsureCreated();

        return new NachaFileBuilder(
            db, holiday.Object, loader.Object, validationService.Object, renderer.Object, recordProvider.Object, semanticValidator.Object,
            resolver.Object, null, null, null, null, mappingEngine.Object, compiler.Object, options, null, batchNumberGenerator.Object);
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static void SetupScenario(
        Mock<INachaDataLoader> loader,
        Mock<INachaConfigResolver> resolver,
        Mock<INachaFixedWidthRecordRenderer> renderer,
        Mock<IFieldMappingPlanCompiler> compiler,
        Mock<INachaRecordMappingEngine> mappingEngine,
        bool mappingSuccess)
    {
        var cycle = new AchCycle { Id = "c1", CycleName = "C40", ProcessingDate = DateTime.UtcNow, ClearingHouse = new ClearingHouse { Name = "ACH Colombia" } };
        var tx = new AchTransaction { Id = 1, Type = TransactionTypeEnum.Credit, Amount = 100m, AchBatchId = 100, AchCycleId = cycle.Id, CompanyIdentification = "1234567890", Addendas = [] };
        var batch = new AchBatch { Id = 100, AchCycle = cycle, AchCycleId = cycle.Id, CompanyIdentification = "1234567890", CompanyName = "CO", OriginOrOdfi = "12345678", ServiceClassCode = "220", EffectiveEntryDate = DateTime.UtcNow, Transactions = [tx], CompanyEntryDescription = "PAGOS" };

        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync([batch]);
        loader.Setup(x => x.LoadHeaderAsync(cycle.Id, It.IsAny<CancellationToken>())).ReturnsAsync((NachaHeader?)null);
        loader.Setup(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, NachaRecordLayout>
        {
            ["8"] = new NachaRecordLayout { RecordCode = "8", TotalLength = 106, Fields = [] },
            ["9"] = new NachaRecordLayout { RecordCode = "9", TotalLength = 106, Fields = [] }
        });
        loader.Setup(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new NachaRecordDefinition { RecordCode = "8", Sequence = 10, IsEnabled = true, SourceType = NachaRecordSourceType.Custom },
            new NachaRecordDefinition { RecordCode = "9", Sequence = 20, IsEnabled = true, SourceType = NachaRecordSourceType.Custom }
        ]);
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<(string, string)> { ("PAGOS", "PPD") });

        var variant8 = BuildVariant("8", "R8_F1");
        var variant9 = BuildVariant("9", "R9_F1");
        resolver.Setup(x => x.ResolveAsync(It.IsAny<NachaConfigResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaConfigResolutionResult { Success = true, Profile = new CfgProfile { Id = 1, ProfileCode = "P1" }, LayoutsByRecordCode = new Dictionary<string, CfgLayoutVariant> { ["8"] = variant8, ["9"] = variant9 } });

        compiler.Setup(x => x.CompileRecordPlan(It.IsAny<CfgLayoutVariant>(), It.IsAny<List<string>>()))
            .Returns((CfgLayoutVariant v, List<string> _) => new RecordRuntimePlan
            {
                LayoutVariantId = v.Id,
                RecordCode = v.RecordCode?.Code ?? "8",
                TotalLength = 106,
                Fields = [new FieldRuntimePlan { LayoutFieldId = 1, RecordCode = v.RecordCode?.Code ?? "8", FieldCode = v.Fields.First().FieldCode, FieldNameEs = "f", StartPosition = 1, Length = 10, SourceTypeCode = "CONSTANTE", ConstantValue = "1", Rules = [] }]
            });

        mappingEngine.Setup(x => x.MapRecordAsync(It.IsAny<RecordMappingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecordMappingRequest req, CancellationToken _) => new RecordMappingResult
            {
                Success = mappingSuccess,
                ValuesByFieldCode = mappingSuccess ? new Dictionary<string, object?> { [req.RecordCode == "8" ? "R8_F1" : "R9_F1"] = "1" } : new Dictionary<string, object?>()
            });

        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string t, object _, NachaRecordLayout _) => new string(t[0], 106));
        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string t, IReadOnlyDictionary<string, object?> _, NachaRecordLayout _) => new string(t[0], 106));
    }

    private static CfgLayoutVariant BuildVariant(string recordCode, string fieldCode)
    {
        return new CfgLayoutVariant
        {
            Id = recordCode == "8" ? 180 : 190,
            VariantCode = $"R{recordCode}",
            TotalLength = 106,
            RecordCode = new CatRecordCode { Code = recordCode, NameEs = $"R{recordCode}" },
            Fields = [new CfgLayoutField { Id = 1, FieldCode = fieldCode, FieldNameEs = fieldCode, StartPosition = 1, Length = 10, IsEnabled = true, SourceDefinition = new CfgFieldSourceDefinition { DataSourceType = new CatDataSourceType { Code = "CONSTANTE" }, ConstantValue = "1" }, Rules = [] }]
        };
    }
}
