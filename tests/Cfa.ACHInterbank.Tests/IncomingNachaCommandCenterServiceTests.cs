using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaCommandCenterServiceTests
{
    [Fact]
    public async Task RetryManualAsync_ShouldQueueBlockedItem_AndCreateAuditEvent()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Blocked);
        var sut = new IncomingNachaCommandCenterService(context);

        var result = await sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-1",
            Justification = "retry manual por incidente"
        }, "ops.user");

        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, result.PreviousStatus);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Queued, result.CurrentStatus);
        Assert.False(result.IsIdempotentReplay);

        var refreshed = await context.IncomingNachaDispatchQueue.FirstAsync(x => x.Id == queue.Id);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Queued, refreshed.QueueStatus);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x => x.EventType == "ManualActionRetry" && x.Message == "IdempotencyKey:retry-1"));
    }

    [Fact]
    public async Task RetryManualAsync_ShouldRejectConfirmed()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Confirmed);
        var sut = new IncomingNachaCommandCenterService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-confirmed",
            Justification = "retry manual no permitido"
        }, "ops.user"));
    }

    [Fact]
    public async Task UnblockManualAsync_ShouldRejectNonBlocked()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.RetryPending);
        var sut = new IncomingNachaCommandCenterService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UnblockManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "unblock-1",
            Justification = "desbloqueo"
        }, "ops.user"));
    }

    [Fact]
    public async Task RetryManualAsync_ShouldBeIdempotent_OnRepeatedIdempotencyKey()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Blocked);
        var sut = new IncomingNachaCommandCenterService(context);

        _ = await sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-replay",
            Justification = "retry manual por incidente"
        }, "ops.user");

        var replay = await sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-replay",
            Justification = "retry manual por incidente"
        }, "ops.user");

        Assert.True(replay.IsIdempotentReplay);
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "ManualActionRetry" && x.Message == "IdempotencyKey:retry-replay"));
    }

    private static async Task<IncomingNachaDispatchQueue> SeedQueueAsync(AchDbContext context, IncomingNachaDispatchQueueStatus status)
    {
        var clearing = new ClearingHouse { Id = 1, Name = "CENIT", Code = "CENIT", OriginCode = "00000000", ClearingHouseId = 1 };
        var cycle = new AchCycle
        {
            Id = "CC-001",
            CycleName = "Ciclo",
            ProcessingDate = DateTime.UtcNow.Date,
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(18),
            ClearingHouseId = 1
        };
        var tx = new AchTransaction
        {
            AchCycleId = cycle.Id,
            Type = TransactionTypeEnum.Debit,
            State = AchTransferStateEnum.Pending,
            Amount = 100,
            EffectiveEntryDate = DateTime.UtcNow.Date,
            TraceNumber = "123456789012345",
            OriginatingDFI = "00000000",
            ReceivingDFI = "11111111",
            SourceAccountNumber = "S1",
            DestinationAccountNumber = "D1",
            TransactionCode = "22",
            Reference = "ref"
        };
        var ingestion = new IncomingNachaFileIngestion { FileName = "in.ach", FileHashSha256 = "h", FileSize = 106, ContentType = "text/plain", CorrelationId = Guid.NewGuid().ToString("N") };
        var classification = new IncomingNachaEntryClassification { IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1, FunctionalClass = IncomingNachaFunctionalClass.Devolucion, EligibilityStatus = IncomingNachaEligibilityStatus.Elegible, BusinessMeaning = "x" };
        context.ClearingHouses.Add(clearing);
        context.AchCycles.Add(cycle);
        context.AchTransactions.Add(tx);
        context.IncomingNachaFileIngestions.Add(ingestion);
        context.IncomingNachaEntryClassifications.Add(classification);
        await context.SaveChangesAsync();

        var link = new IncomingNachaTransactionLink { IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1, AchTransactionId = tx.Id, LinkType = IncomingNachaLinkType.ExactTrace15, IsFinal = true, LinkedBy = "sys" };
        context.IncomingNachaTransactionLinks.Add(link);
        await context.SaveChangesAsync();

        var queue = new IncomingNachaDispatchQueue
        {
            IncomingNachaFileIngestionId = ingestion.Id,
            IncomingNachaEntryClassificationId = classification.Id,
            IncomingNachaTransactionLinkId = link.Id,
            AchTransactionId = tx.Id,
            AchCycleId = cycle.Id,
            ClearingHouseId = 1,
            OperationalDate = DateTime.UtcNow.Date,
            QueueStatus = status,
            IdempotencyDispatchKey = Guid.NewGuid().ToString("N"),
            Priority = 10
        };

        context.IncomingNachaDispatchQueue.Add(queue);
        await context.SaveChangesAsync();
        return queue;
    }

    private static Task<AchDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        return Task.FromResult(context);
    }
}
