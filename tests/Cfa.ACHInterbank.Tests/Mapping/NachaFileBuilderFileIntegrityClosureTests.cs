using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
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

public class NachaFileBuilderFileIntegrityClosureTests
{
    [Theory]
    [InlineData("ACH Colombia")]
    [InlineData("CENIT")]
    public async Task BuildNachaFileAsync_ShouldGenerateCompleteFile_WithExpectedIntegrity(string chamberName)
    {
        var loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        var renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        var validation = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);
        var semantic = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        var recordProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Loose);
        var holiday = new Mock<IBankHoliday>(MockBehavior.Loose);
        var batchGenerator = new Mock<IBatchNumberGenerator>(MockBehavior.Strict);

        object? record8 = null;
        object? record9 = null;

        var cycle = new AchCycle { Id = "c1", CycleName = "C40", ProcessingDate = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), ClearingHouse = new ClearingHouse { Name = chamberName } };
        var tx = new AchTransaction
        {
            Id = 1,
            Type = TransactionTypeEnum.Credit,
            Amount = 123.45m,
            AchBatchId = 100,
            AchCycleId = cycle.Id,
            CompanyIdentification = "1234567890",
            ReceivingDFI = "11112222",
            DestinationAccountNumber = "000123456789",
            RecipientIdNumber = "900000001",
                        TransactionCode = "22",
            TraceNumber = "123456780000001",
            Addendas = [new AchTransactionAddenda { Information = "INFO" }]
        };

        var batch = new AchBatch
        {
            Id = 100,
            AchCycle = cycle,
            AchCycleId = cycle.Id,
            CompanyIdentification = "1234567890",
            CompanyName = "CO",
            OriginOrOdfi = "12345678",
            ServiceClassCode = "220",
            EffectiveEntryDate = new DateTime(2026, 4, 19),
            Transactions = [tx],
            CompanyEntryDescription = "PAGOS"
        };

        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync([batch]);
        loader.Setup(x => x.LoadHeaderAsync(cycle.Id, It.IsAny<CancellationToken>())).ReturnsAsync((NachaHeader?)null);
        loader.Setup(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, NachaRecordLayout>
        {
            ["1"] = new NachaRecordLayout { RecordCode = "1", TotalLength = 106, Fields = [] },
            ["5"] = new NachaRecordLayout { RecordCode = "5", TotalLength = 106, Fields = [] },
            ["6"] = new NachaRecordLayout { RecordCode = "6", TotalLength = 106, Fields = [] },
            ["7"] = new NachaRecordLayout { RecordCode = "7", TotalLength = 106, Fields = [] },
            ["8"] = new NachaRecordLayout { RecordCode = "8", TotalLength = 106, Fields = [] },
            ["9"] = new NachaRecordLayout { RecordCode = "9", TotalLength = 106, Fields = [] }
        });

        loader.Setup(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([
            new NachaRecordDefinition { RecordCode = "1", IsEnabled = true, Sequence = 1 },
            new NachaRecordDefinition { RecordCode = "5", IsEnabled = true, Sequence = 2 },
            new NachaRecordDefinition { RecordCode = "6", IsEnabled = true, Sequence = 3 },
            new NachaRecordDefinition { RecordCode = "7", IsEnabled = true, Sequence = 4 },
            new NachaRecordDefinition { RecordCode = "8", IsEnabled = true, Sequence = 5 },
            new NachaRecordDefinition { RecordCode = "9", IsEnabled = true, Sequence = 6 }
        ]);
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([(Term: "PAGOS", StandardEntryClassCode: "PPD")]);

        validation.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semantic.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));

        renderer.Setup(x => x.RenderRecordAsync("1", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('1', 106));
        renderer.Setup(x => x.RenderRecordAsync("5", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('5', 106));
        renderer.Setup(x => x.RenderRecordAsync("6", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('6', 106));
        renderer.Setup(x => x.RenderRecordAsync("7", It.IsAny<object>(), It.IsAny<NachaRecordLayout>())).ReturnsAsync(new string('7', 106));
        renderer.Setup(x => x.RenderRecordAsync("8", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()))
            .Callback<string, object, NachaRecordLayout>((_, entity, _) => record8 = entity)
            .ReturnsAsync(new string('8', 106));
        renderer.Setup(x => x.RenderRecordAsync("9", It.IsAny<object>(), It.IsAny<NachaRecordLayout>()))
            .Callback<string, object, NachaRecordLayout>((_, entity, _) => record9 = entity)
            .ReturnsAsync(new string('9', 106));
        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string code, IReadOnlyDictionary<string, object?> _, NachaRecordLayout _) => new string(code[0], 106));

        batchGenerator.Setup(x => x.AssignBatchNumbersAsync(It.IsAny<IReadOnlyList<AchBatch>>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchNumberAssignmentResult(
                new Dictionary<int, int> { [100] = 15 },
                "DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI",
                1,
                [new BatchNumberScopeTrace("DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI", "scope", 14, 15, false, 1)]));

        var dbOptions = new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AchDbContext(dbOptions);
        var options = Options.Create(new NachaGenerationOptions { Mode = "LEGACY" });

        var sut = new NachaFileBuilder(db, holiday.Object, loader.Object, validation.Object, renderer.Object, recordProvider.Object, semantic.Object,
            null, null, null, null, null, null, null, options, null, batchGenerator.Object);

        var content = await sut.BuildNachaFileAsync([100], CancellationToken.None);

        var records = Enumerable.Range(0, content.Length / 106).Select(i => content.Substring(i * 106, 106)).ToList();
        records.Should().OnlyContain(r => r.Length == 106);
        records.Count.Should().Be(10); // 6 records + 4 filler
        records[0][0].Should().Be('1');
        records[1][0].Should().Be('5');
        records[2][0].Should().Be('6');
        records[3][0].Should().Be('7');
        records[4][0].Should().Be('8');
        records[5][0].Should().Be('9');
        records.Skip(6).Should().OnlyContain(x => x.All(ch => ch == '9'));

        ReadIntProperty(record8!, "BatchNumber").Should().Be(15);
        ReadIntProperty(record9!, "BatchCount").Should().Be(1);
        ReadIntProperty(record9!, "BlockCount").Should().Be(1);
        ReadIntProperty(record9!, "EntryAddendaCount").Should().Be(2);
        ReadLongProperty(record9!, "TotalCreditAmount").Should().Be(12345);
        ReadLongProperty(record9!, "TotalDebitAmount").Should().Be(0);
    }

    private static int ReadIntProperty(object instance, string propertyName)
    {
        var prop = instance.GetType().GetProperty(propertyName);
        prop.Should().NotBeNull();
        return (int)(prop!.GetValue(instance) ?? 0);
    }

    private static long ReadLongProperty(object instance, string propertyName)
    {
        var prop = instance.GetType().GetProperty(propertyName);
        prop.Should().NotBeNull();
        return (long)(prop!.GetValue(instance) ?? 0L);
    }
}
