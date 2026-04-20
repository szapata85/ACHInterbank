using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
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

public class NachaFileBuilderBatchNumberHardeningTests
{
    [Fact]
    public async Task BuildNachaFileAsync_R5AndR8_ShouldUseSameBatchNumberPerBatch()
    {
        var setup = CreateBaseSut(mode: "HYBRID");
        object? record5 = null;
        object? record8 = null;

        setup.Renderer.Setup(x => x.RenderRecordAsync("5", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()))
            .Callback<string, object, NachaRecordLayout>((_, entity, _) => record5 = entity)
            .ReturnsAsync(new string('5', 106));

        setup.Renderer.Setup(x => x.RenderRecordAsync("8", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()))
            .Callback<string, object, NachaRecordLayout>((_, entity, _) => record8 = entity)
            .ReturnsAsync(new string('8', 106));

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        record5.Should().NotBeNull();
        record8.Should().NotBeNull();
        ReadIntProperty(record5!, "BatchNumber").Should().Be(77);
        ReadIntProperty(record8!, "BatchNumber").Should().Be(77);
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShadowCompare_ShouldRequestBatchNumberOnce()
    {
        var setup = CreateBaseSut(mode: "SHADOW_COMPARE");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        setup.BatchGenerator.Verify(x => x.AssignBatchNumbersAsync(
            It.IsAny<IReadOnlyList<AchBatch>>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static int ReadIntProperty(object instance, string propertyName)
    {
        var prop = instance.GetType().GetProperty(propertyName);
        prop.Should().NotBeNull();
        return (int)(prop!.GetValue(instance) ?? 0);
    }

    private static SutSetup CreateBaseSut(string mode)
    {
        var loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        var renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        var validation = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);
        var semantic = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        var resolver = new Mock<INachaConfigResolver>(MockBehavior.Loose);
        var recordProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Loose);
        var holiday = new Mock<IBankHoliday>(MockBehavior.Loose);
        var batchGenerator = new Mock<IBatchNumberGenerator>(MockBehavior.Strict);

        var cycle = new AchCycle { Id = "c1", CycleName = "C40", ProcessingDate = DateTime.UtcNow, ClearingHouse = new ClearingHouse { Name = "ACH Colombia" } };
        var tx = new AchTransaction
        {
            Id = 1,
            Type = TransactionTypeEnum.Credit,
            Amount = 100m,
            AchBatchId = 100,
            AchCycleId = cycle.Id,
            CompanyIdentification = "1234567890",
            ReceivingDFI = "12345678",
            DestinationAccountNumber = "1234567890",
            TraceNumber = "123456780000001",
            Addendas = []
        };
        var batch = new AchBatch { Id = 100, AchCycle = cycle, AchCycleId = cycle.Id, CompanyIdentification = "1234567890", CompanyName = "CO", OriginOrOdfi = "12345678", ServiceClassCode = "220", EffectiveEntryDate = DateTime.UtcNow, Transactions = [tx], CompanyEntryDescription = "PAGOS" };

        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync([batch]);
        loader.Setup(x => x.LoadHeaderAsync(cycle.Id, It.IsAny<CancellationToken>())).ReturnsAsync((NachaHeader?)null);
        loader.Setup(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, NachaRecordLayout>
        {
            ["1"] = new NachaRecordLayout { RecordCode = "1", TotalLength = 106, Fields = [] },
            ["5"] = new NachaRecordLayout { RecordCode = "5", TotalLength = 106, Fields = [] },
            ["6"] = new NachaRecordLayout { RecordCode = "6", TotalLength = 106, Fields = [] },
            ["8"] = new NachaRecordLayout { RecordCode = "8", TotalLength = 106, Fields = [] },
            ["9"] = new NachaRecordLayout { RecordCode = "9", TotalLength = 106, Fields = [] }
        });
        loader.Setup(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([
            new NachaRecordDefinition { RecordCode = "1", IsEnabled = true, Sequence = 1 },
            new NachaRecordDefinition { RecordCode = "5", IsEnabled = true, Sequence = 2 },
            new NachaRecordDefinition { RecordCode = "6", IsEnabled = true, Sequence = 3 },
            new NachaRecordDefinition { RecordCode = "8", IsEnabled = true, Sequence = 4 },
            new NachaRecordDefinition { RecordCode = "9", IsEnabled = true, Sequence = 5 }
        ]);
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([(Term: "PAGOS", StandardEntryClassCode: "PPD")]);
        validation.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semantic.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));
        resolver.Setup(x => x.ResolveAsync(It.IsAny<NachaConfigResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaConfigResolutionResult { Success = false, Trace = [], Warnings = [] });

        renderer.Setup(x => x.RenderRecordAsync("1", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('1', 106));
        renderer.Setup(x => x.RenderRecordAsync("5", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('5', 106));
        renderer.Setup(x => x.RenderRecordAsync("6", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('6', 106));
        renderer.Setup(x => x.RenderRecordAsync("8", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('8', 106));
        renderer.Setup(x => x.RenderRecordAsync("9", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('9', 106));
        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync(new string('S', 106));

        batchGenerator.Setup(x => x.AssignBatchNumbersAsync(It.IsAny<IReadOnlyList<AchBatch>>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AchBatch> batches, string _, DateTime _, CancellationToken _) =>
                new BatchNumberAssignmentResult(
                    batches.ToDictionary(x => x.Id, x => 77),
                    "DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI",
                    1,
                    [new BatchNumberScopeTrace("DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI", "ACH|12345678|2026-04-19|DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI", 0, 77, true, batches.Count)]));

        var options = Options.Create(new NachaGenerationOptions
        {
            Mode = mode,
            EnableRecord5MappingEngine = true,
            EnableRecord8MappingEngine = true
        });

        var dbOptions = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(CreateOpenConnection()).Options;
        var db = new AchDbContext(dbOptions);
        db.Database.EnsureCreated();

        var sut = new NachaFileBuilder(db, holiday.Object, loader.Object, validation.Object, renderer.Object, recordProvider.Object, semantic.Object,
            resolver.Object, null, null, null, null, null, null, options, null, batchGenerator.Object);

        return new SutSetup(sut, renderer, batchGenerator);
    }

    private sealed record SutSetup(NachaFileBuilder Sut, Mock<INachaFixedWidthRecordRenderer> Renderer, Mock<IBatchNumberGenerator> BatchGenerator);

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }
}
