using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaPostProcessingOrchestratorTests
{
    [Fact]
    public async Task ExecuteAsync_BlocksQueue_WhenMappingIsInvalid()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = new Mock<IProcTransaccionesRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("mapping inválido"));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            Mock.Of<IWscfaachSoapClient>());

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Equal("MAPPING_INVALID", queue.LastErrorCode);
        Assert.True(await context.IncomingNachaIntegrationExecution.AnyAsync());
    }

    [Fact]
    public async Task ExecuteAsync_SetsRetryPending_WhenTechnicalErrorOccurs()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = new Mock<IProcTransaccionesRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcTransaccionesRequestResolution(
                new ProcTransaccionesRequestContract(new Dictionary<string, string> { ["TREG"] = "6", ["TIPTRAN"] = "22", ["MONTO"] = "10", ["IDTRAN"] = "1", ["IDCAMCOMPE"] = "1" }),
                Guid.NewGuid(),
                1,
                "hash"));
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>())).Returns("<request/>");

        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("timeout"));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.RetryPending);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.RetryPending, queue.QueueStatus);
        Assert.NotNull(queue.NextAttemptAtUtc);
    }

    private static void SeedDispatchItem(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACH",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 1,
            Term = "PAGOS",
            Description = "Pagos",
            StandardEntryClassCode = "PPD",
            IsActive = true
        });
        var fi = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Test",
            RoutingNumber = "12345",
            TransitCode = "678",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        };
        fi.CalculateCheckDigit();
        context.FinancialInstitutions.Add(fi);

        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = "in.ach",
            FileHashSha256 = "h",
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "tester",
            CorrelationId = "c",
            Notes = "n"
        };
        var cycle = new AchCycle
        {
            Id = "C1",
            CycleName = "c1",
            ClearingHouseId = 1,
            ProcessingDate = DateTime.Today,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0)
        };
        context.AchCycles.Add(cycle);
        context.AchBatches.Add(new AchBatch { Id = 1, AchCycleId = "C1", CompanyEntryDescriptionId = 1, EffectiveEntryDate = DateTime.Today });
        var tx = new AchTransaction
        {
            Id = 100,
            Amount = 100m,
            TransactionExternalId = "EXT-1",
            Reference = "R",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            SourceAccountNumber = "S",
            DestinationAccountNumber = "D",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1,
            OriginatingDFI = "11111111",
            ReceivingDFI = "222222220",
            TraceNumber = "123456789012345",
            CompanyName = "C",
            CompanyIdentification = "I",
            AchCycleId = "C1",
            AchBatchId = 1,
            EffectiveEntryDate = DateTime.Today
        };
        context.AchTransactions.Add(tx);
        var classification = new IncomingNachaEntryClassification { Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1 };
        var link = new IncomingNachaTransactionLink { Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1, AchTransactionId = tx.Id, IsFinal = true, LinkType = IncomingNachaLinkType.ExactTrace15 };

        context.IncomingNachaFileIngestions.Add(ingestion);
        context.IncomingNachaEntryClassifications.Add(classification);
        context.IncomingNachaTransactionLinks.Add(link);
        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            IncomingNachaEntryClassificationId = classification.Id,
            IncomingNachaTransactionLinkId = link.Id,
            AchTransactionId = tx.Id,
            AchCycleId = "C1",
            ClearingHouseId = 1,
            OperationalDate = DateTime.Today,
            QueueStatus = IncomingNachaDispatchQueueStatus.Queued,
            Priority = 100,
            IdempotencyDispatchKey = Guid.NewGuid().ToString("N"),
            NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.SaveChanges();
    }

    private static AchDbContext BuildContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
