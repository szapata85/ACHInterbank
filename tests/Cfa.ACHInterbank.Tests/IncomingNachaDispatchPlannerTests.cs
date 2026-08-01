using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Cfa.ACHInterbank.Tests.TestSupport;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaDispatchPlannerTests
{
    [Fact]
    public async Task PlanForIngestionAsync_EnqueuesOnlyEligibleTransactions()
    {
        await using var context = BuildContext();
        var ingestion = SeedCommonGraph(context);

        var eligibility = new IncomingNachaDispatchEligibilityPolicy();
        var sut = new IncomingNachaDispatchPlanner(context, eligibility, timeProvider: TestClock.Create());

        var created = await sut.PlanForIngestionAsync(ingestion.Id, "tester");

        Assert.Equal(2, created);
        var queued = await context.IncomingNachaDispatchQueue.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Queued);
        var blocked = await context.IncomingNachaDispatchQueue.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Blocked);
        Assert.Equal(1, queued);
        Assert.Equal(1, blocked);
    }

    [Fact]
    public async Task PlanForIngestionAsync_WaitingWindow_PersistsDeterministicNextEligibleUtc()
    {
        await using var context = BuildContext();
        var ingestion = SeedCommonGraph(context);
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == "C1");
        cycle.StartTime = new TimeSpan(13, 0, 0);
        cycle.EndTime = new TimeSpan(14, 0, 0);
        await context.SaveChangesAsync();

        var sut = new IncomingNachaDispatchPlanner(
            context,
            new IncomingNachaDispatchEligibilityPolicy(),
            timeProvider: TestClock.Create());

        await sut.PlanForIngestionAsync(ingestion.Id, "tester");

        var queue = await context.IncomingNachaDispatchQueue.SingleAsync(x => x.AchTransactionId == 100);
        Assert.Equal(IncomingNachaDispatchQueueStatus.WaitingWindow, queue.QueueStatus);
        Assert.Equal(new DateTime(2026, 7, 24, 18, 0, 0, DateTimeKind.Utc), queue.NextAttemptAtUtc);
        Assert.Empty(queue.LastErrorCode);
    }

    [Fact]
    public async Task PlanForIngestionAsync_ExpiredWindow_BlocksFailClosed()
    {
        await using var context = BuildContext();
        var ingestion = SeedCommonGraph(context);
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == "C1");
        cycle.StartTime = new TimeSpan(8, 0, 0);
        cycle.EndTime = new TimeSpan(10, 0, 0);
        await context.SaveChangesAsync();

        var sut = new IncomingNachaDispatchPlanner(
            context,
            new IncomingNachaDispatchEligibilityPolicy(),
            timeProvider: TestClock.Create());

        await sut.PlanForIngestionAsync(ingestion.Id, "tester");

        var queue = await context.IncomingNachaDispatchQueue.SingleAsync(x => x.AchTransactionId == 100);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Equal("WINDOW_EXPIRED", queue.LastErrorCode);
        Assert.Null(queue.NextAttemptAtUtc);
    }

    [Fact]
    public async Task PlanForIngestionAsync_UsesIngestionAndClassificationInUniqueIdempotencyKey()
    {
        await using var context = BuildContext();
        var firstIngestion = SeedCommonGraph(context);
        var eligibility = new IncomingNachaDispatchEligibilityPolicy();
        var sut = new IncomingNachaDispatchPlanner(context, eligibility, timeProvider: TestClock.Create());

        await sut.PlanForIngestionAsync(firstIngestion.Id, "tester");
        var firstQueue = await context.IncomingNachaDispatchQueue
            .SingleAsync(x => x.IncomingNachaFileIngestionId == firstIngestion.Id && x.AchTransactionId == 100);

        var secondIngestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = "in-second.ach",
            FileHashSha256 = "h-second",
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "tester",
            CorrelationId = "c-second",
            Notes = "n",
            IngestionStatus = IncomingNachaIngestionStatus.Completado,
            CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
            ResolvedAchCycleId = "C1",
            ResolvedClearingHouseId = 1,
            OperationalDate = TestClock.OperationalDate
        };
        var secondClassification = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = secondIngestion.Id,
            EntryDetailId = 3,
            FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible
        };
        var secondLink = new IncomingNachaTransactionLink
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = secondIngestion.Id,
            EntryDetailId = 3,
            AchTransactionId = 100,
            LinkType = IncomingNachaLinkType.ExactTrace15,
            IsFinal = true
        };
        context.EntryDetails.Add(new EntryDetail { EntryDetailID = 3, TransactionCode = "22", ReceivingParticipantEntityCode = "22222222", AccountNumber = "D", Amount = 100m, RecipUserName = "Receiver" });
        context.AddRange(secondIngestion, secondClassification, secondLink);
        await context.SaveChangesAsync();

        var created = await sut.PlanForIngestionAsync(secondIngestion.Id, "tester");
        var secondQueue = await context.IncomingNachaDispatchQueue
            .SingleAsync(x => x.IncomingNachaFileIngestionId == secondIngestion.Id && x.AchTransactionId == 100);

        Assert.Equal(1, created);
        Assert.NotEqual(firstQueue.IdempotencyDispatchKey, secondQueue.IdempotencyDispatchKey);
        Assert.Equal(64, firstQueue.IdempotencyDispatchKey.Length);
        Assert.Equal(64, secondQueue.IdempotencyDispatchKey.Length);
    }

    private static IncomingNachaFileIngestion SeedCommonGraph(AchDbContext context)
    {
        var cycle = new AchCycle
        {
            Id = "C1",
            CycleName = "ciclo 1",
            ClearingHouseId = 1,
            ProcessingDate = TestClock.OperationalDate,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0)
        };
        context.AchCycles.Add(cycle);
        context.AchBatches.Add(new AchBatch { Id = 1, AchCycleId = "C1", CompanyEntryDescriptionId = 1, EffectiveEntryDate = TestClock.OperationalDate });

        var tx1 = new AchTransaction
        {
            Id = 100,
            Amount = 100m,
            TransactionExternalId = "EXT-100",
            Reference = "R100",
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
            EffectiveEntryDate = TestClock.OperationalDate
        };
        var tx2 = new AchTransaction
        {
            Id = 101,
            Amount = 101m,
            TransactionExternalId = "EXT-101",
            Reference = "R101",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            SourceAccountNumber = "S",
            DestinationAccountNumber = "D",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1,
            OriginatingDFI = "11111111",
            ReceivingDFI = "222222220",
            TraceNumber = "123456789012346",
            CompanyName = "C",
            CompanyIdentification = "I",
            AchCycleId = "C1",
            AchBatchId = 1,
            EffectiveEntryDate = TestClock.OperationalDate
        };
        context.AchTransactions.AddRange(tx1, tx2);

        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = "in.ach",
            FileHashSha256 = "h",
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "tester",
            CorrelationId = "c",
            Notes = "n",
            IngestionStatus = IncomingNachaIngestionStatus.Completado,
            CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
            ResolvedAchCycleId = "C1",
            ResolvedClearingHouseId = 1,
            OperationalDate = TestClock.OperationalDate
        };
        context.IncomingNachaFileIngestions.Add(ingestion);

        var classEligible = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = 1,
            FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible
        };
        var classBlocked = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = 2,
            FunctionalClass = IncomingNachaFunctionalClass.Prenotificacion,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible
        };
        context.IncomingNachaEntryClassifications.AddRange(classEligible, classBlocked);

        context.IncomingNachaTransactionLinks.AddRange(
            new IncomingNachaTransactionLink
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = ingestion.Id,
                EntryDetailId = 1,
                AchTransactionId = tx1.Id,
                LinkType = IncomingNachaLinkType.ExactTrace15,
                IsFinal = true
            },
            new IncomingNachaTransactionLink
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = ingestion.Id,
                EntryDetailId = 2,
                AchTransactionId = tx2.Id,
                LinkType = IncomingNachaLinkType.ExactTrace15,
                IsFinal = true
            });

        context.SaveChanges();
        return ingestion;
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }
}
