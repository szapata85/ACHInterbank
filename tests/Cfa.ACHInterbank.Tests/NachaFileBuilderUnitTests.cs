using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaFileBuilderUnitTests
{
    [Fact]
    public async Task BuildNachaFileAsync_ShouldThrow_WhenNoBatchesFound()
    {
        var sut = CreateSut(out var loader, out _, out _, out _, out _, out _);

        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchBatch>());

        var act = async () => await sut.BuildNachaFileAsync([999], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se encontraron lotes para exportar.*");
    }

    [Fact]
    public async Task BuildNachaFileByCycleAsync_ShouldThrow_WhenCycleHasNoTransactions()
    {
        var sut = CreateSut(out var loader, out _, out _, out _, out _, out _);

        loader.Setup(x => x.LoadByCycleAsync("cycle-empty", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cfa.ACHInterbank.Application.ACH.Models.NachaBuildContext
            {
                Cycle = new AchCycle { Id = "cycle-empty", CycleName = "C1", ProcessingDate = DateTime.UtcNow },
                Batches = [new AchBatch { Id = 1 }],
                Transactions = []
            });

        var act = async () => await sut.BuildNachaFileByCycleAsync("cycle-empty", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no tiene transacciones para exportar*");
    }

    [Fact]
    public async Task BuildNachaFileByCycleAsync_ShouldThrow_WhenCycleHasNoBatches()
    {
        var sut = CreateSut(out var loader, out _, out _, out _, out _, out _);

        loader.Setup(x => x.LoadByCycleAsync("cycle-no-batches", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cfa.ACHInterbank.Application.ACH.Models.NachaBuildContext
            {
                Cycle = new AchCycle { Id = "cycle-no-batches", CycleName = "C1", ProcessingDate = DateTime.UtcNow },
                Batches = [],
                Transactions = [new AchTransaction { Id = 1 }]
            });

        var act = async () => await sut.BuildNachaFileByCycleAsync("cycle-no-batches", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no tiene lotes asociados para exportar*");
    }

    [Fact]
    public async Task BuildRecordAsync_ShouldThrow_WhenLayoutNotFound()
    {
        var sut = CreateSut(out var loader, out _, out _, out _, out _, out _);

        loader.Setup(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, NachaRecordLayout>());

        var act = async () => await sut.BuildRecordAsync("6", new { Id = 1 }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Layout no encontrado*");
    }

    [Fact]
    public async Task BuildNachaFileAsync_ShouldGenerateExpectedContent_ForMinimalValidScenario()
    {
        var sut = CreateSut(out var loader, out var validator, out var renderer, out _, out var semanticValidator, out _);

        var cycle = new AchCycle
        {
            Id = "cycle-1",
            CycleName = "CICLO-1",
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

        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([batch]);
        loader.Setup(x => x.LoadHeaderAsync(cycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NachaHeader?)null);
        loader.Setup(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalLayouts());
        loader.Setup(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalDefinitions());
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string Term, string StandardEntryClassCode)> { ("PAGOS", "PPD") });

        validator.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string recordType, object _, NachaRecordLayout _) => new string(recordType[0], 106));
        renderer.Setup(x => x.RenderRecordAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<NachaRecordLayout>()))
            .ReturnsAsync((string recordType, IReadOnlyDictionary<string, object?> _, NachaRecordLayout _) => new string(recordType[0], 106));

        semanticValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<Cfa.ACHInterbank.Application.ACH.Models.NachaBuildContext>()));

        var content = await sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        content.Length.Should().Be(1060); // 10 registros (bloque) de 106
        content[0].Should().Be('1');
        semanticValidator.Verify(x => x.Validate(content, It.IsAny<Cfa.ACHInterbank.Application.ACH.Models.NachaBuildContext>()), Times.Once);
    }

    private static Dictionary<string, NachaRecordLayout> BuildMinimalLayouts()
    {
        return new Dictionary<string, NachaRecordLayout>
        {
            ["1"] = new NachaRecordLayout { RecordCode = "1", TotalLength = 106, Fields = [] },
            ["5"] = new NachaRecordLayout { RecordCode = "5", TotalLength = 106, Fields = [] },
            ["6"] = new NachaRecordLayout
            {
                RecordCode = "6",
                TotalLength = 106,
                Fields =
                [
                    new NachaRecordField { FieldName = "ReceivingDFI", DbColumn = "ReceivingDFI", StartPosition = 4, Length = 8, Justification = 'R', PadChar = '0' }
                ]
            },
            ["7"] = new NachaRecordLayout { RecordCode = "7", TotalLength = 106, Fields = [] },
            ["8"] = new NachaRecordLayout { RecordCode = "8", TotalLength = 106, Fields = [] },
            ["9"] = new NachaRecordLayout { RecordCode = "9", TotalLength = 106, Fields = [] }
        };
    }

    private static List<NachaRecordDefinition> BuildMinimalDefinitions()
    {
        return
        [
            new NachaRecordDefinition { RecordCode = "1", Sequence = 10, IsEnabled = true, SourceType = NachaRecordSourceType.Custom },
            new NachaRecordDefinition { RecordCode = "9", Sequence = 20, IsEnabled = true, SourceType = NachaRecordSourceType.Custom }
        ];
    }

    private static NachaFileBuilder CreateSut(
        out Mock<INachaDataLoader> loader,
        out Mock<INachaTransactionValidationService> validator,
        out Mock<INachaFixedWidthRecordRenderer> renderer,
        out Mock<INachaRecordDataProvider> recordDataProvider,
        out Mock<INachaSemanticValidator> semanticValidator,
        out Mock<IBankHoliday> holidayService)
    {
        loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        validator = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);
        renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        recordDataProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Strict);
        semanticValidator = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        holidayService = new Mock<IBankHoliday>(MockBehavior.Strict);

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(CreateOpenConnection())
            .Options;
        var dbContext = new AchDbContext(options);
        dbContext.Database.EnsureCreated();

        return new NachaFileBuilder(
            dbContext,
            holidayService.Object,
            loader.Object,
            validator.Object,
            renderer.Object,
            recordDataProvider.Object,
            semanticValidator.Object,
            generationOptions: Options.Create(new NachaGenerationOptions { Mode = "LEGACY" }));
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }
}
