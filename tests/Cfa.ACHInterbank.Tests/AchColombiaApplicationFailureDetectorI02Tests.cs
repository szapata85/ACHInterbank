using System.Text.Json;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaApplicationFailureDetectorI02Tests
{
    [Theory]
    [InlineData(TransactionTypeEnum.Credit)]
    [InlineData(TransactionTypeEnum.Debit)]
    [InlineData(TransactionTypeEnum.Prenotification)]
    public async Task ConfirmedFunctionalRejectionCreatesOnlyPassiveEvent(TransactionTypeEnum type)
    {
        await using var db = NewContext();
        db.ReturnReasons.Add(new ReturnReason { Code = "R02", Description = "Cuenta cerrada", IsForReturn = true });
        await db.SaveChangesAsync();
        var (queue, execution) = Build(type);
        execution.ResultCode = "R02";
        execution.AchReturnCodeId = 55; // Resultado ya resuelto por el catálogo por cámara/tipo.

        var detector = new AchColombiaApplicationFailureDetector(db, TimeProvider.System);
        Assert.True(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        await db.SaveChangesAsync();

        var eventRow = Assert.Single(db.IncomingNachaProcessingEvents);
        Assert.Equal(AchColombiaApplicationFailureDetector.DetectedEvent, eventRow.EventType);
        Assert.Equal(queue.AchTransactionId, eventRow.AchTransactionId);
        using var json = JsonDocument.Parse(eventRow.EvidenceJson);
        Assert.Equal("ACHCOL", json.RootElement.GetProperty("camera").GetString());
        Assert.Equal("R02", json.RootElement.GetProperty("suggestedReturnReason").GetString());
        Assert.False(json.RootElement.GetProperty("returnGenerated").GetBoolean());
        Assert.False(json.RootElement.GetProperty("returnSent").GetBoolean());
        Assert.False(await db.Set<AchReturnGenerated>().AnyAsync());
        Assert.Equal(AchTransferStateEnum.Pending, queue.AchTransaction!.State);
    }

    [Fact]
    public async Task UnknownCoreCodeCreatesReviewEventWithoutInventingReason()
    {
        await using var db = NewContext();
        var (queue, execution) = Build(TransactionTypeEnum.Credit);
        execution.ResultCode = "CORE_123";
        var detector = new AchColombiaApplicationFailureDetector(db, TimeProvider.System);
        Assert.True(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        await db.SaveChangesAsync();
        var eventRow = Assert.Single(db.IncomingNachaProcessingEvents);
        Assert.Equal(AchColombiaApplicationFailureDetector.ReviewEvent, eventRow.EventType);
        using var json = JsonDocument.Parse(eventRow.EvidenceJson);
        Assert.Equal(string.Empty, json.RootElement.GetProperty("suggestedReturnReason").GetString());
    }

    [Fact]
    public async Task UncataloguedRCodeIsNotAssumedToBeADevolucion()
    {
        await using var db = NewContext();
        var (queue, execution) = Build(TransactionTypeEnum.Debit);
        execution.ResultCode = "R01";
        execution.AchReturnCodeId = 1; // Sin entrada ReturnReasons autorizada.
        var detector = new AchColombiaApplicationFailureDetector(db, TimeProvider.System);
        Assert.True(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        Assert.Equal(AchColombiaApplicationFailureDetector.ReviewEvent,
            Assert.Single(db.IncomingNachaProcessingEvents.Local).EventType);
    }

    [Fact]
    public async Task SameTransactionDoesNotEmitRepeatedEventsAcrossRepeatedCalls()
    {
        await using var db = NewContext();
        var (queue, execution) = Build(TransactionTypeEnum.Credit);
        var detector = new AchColombiaApplicationFailureDetector(db, TimeProvider.System);
        Assert.True(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        await db.SaveChangesAsync();
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        Assert.Single(db.IncomingNachaProcessingEvents);
    }

    [Theory]
    [InlineData("DryRun")]
    [InlineData("Disabled")]
    public async Task DryRunOrDisabledNeverCreatesApplicationFailure(string mode)
    {
        await using var db = NewContext();
        var (queue, execution) = Build(TransactionTypeEnum.Credit);
        var detector = new AchColombiaApplicationFailureDetector(db, TimeProvider.System);
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, mode, CancellationToken.None));
        Assert.Empty(db.IncomingNachaProcessingEvents.Local);
    }

    [Fact]
    public async Task CenitAndFileBasedNeverCreateApplicationFailure()
    {
        await using var db = NewContext();
        var detector = new AchColombiaApplicationFailureDetector(db, TimeProvider.System);
        var (queue, execution) = Build(TransactionTypeEnum.Credit);
        queue.AchTransaction!.AchCycle.ClearingHouse!.Code = "CENIT";
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        queue.AchTransaction.AchCycle.ClearingHouse.Code = "ACHCOL";
        queue.SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased;
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        Assert.Empty(db.IncomingNachaProcessingEvents.Local);
    }

    [Fact]
    public async Task TechnicalRetryUnknownResponseOrMissingCatalogNeverBecomesApplicationFailure()
    {
        await using var db = NewContext();
        var detector = new AchColombiaApplicationFailureDetector(db, TimeProvider.System);
        var (queue, execution) = Build(TransactionTypeEnum.Debit);
        execution.TransportStatus = IntegrationTransportStatus.TimedOut;
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        execution.TransportStatus = IntegrationTransportStatus.Succeeded;
        execution.RetryAllowed = true;
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        execution.RetryAllowed = false;
        execution.RequiresManualReview = true;
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        execution.RequiresManualReview = false;
        execution.ResponseCatalogId = null;
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        execution.ResponseCatalogId = 5;
        execution.IsFunctionalRejection = false;
        Assert.False(await detector.RecordIfConfirmedAsync(queue, execution, "Live", CancellationToken.None));
        Assert.Empty(db.IncomingNachaProcessingEvents.Local);
    }

    private static AchDbContext NewContext() => new(
        new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"achcol-application-failure-i02-{Guid.NewGuid():N}").Options);

    private static (IncomingNachaDispatchQueue Queue, IncomingNachaIntegrationExecution Execution) Build(TransactionTypeEnum type)
    {
        const int clearingHouseId = 41;
        var queue = new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = Guid.NewGuid(),
            IncomingNachaTransactionLinkId = Guid.NewGuid(),
            AchCycleId = "ACH-I02-TEST",
            SourceKind = IncomingNachaDispatchSourceKinds.CycleBound,
            AchTransactionId = 77,
            ClearingHouseId = clearingHouseId,
            AchTransaction = new AchTransaction
            {
                Id = 77,
                AchCycleId = "ACH-I02-TEST",
                Type = type,
                State = AchTransferStateEnum.Pending,
                AchCycle = new AchCycle
                {
                    Id = "ACH-I02-TEST",
                    ClearingHouseId = clearingHouseId,
                    ClearingHouse = new ClearingHouse
                    {
                        Id = clearingHouseId,
                        Code = "ACHCOL",
                        Name = "ACH Colombia",
                        OriginCode = "TEST"
                    }
                }
            }
        };
        var execution = new IncomingNachaIntegrationExecution
        {
            Id = Guid.NewGuid(),
            DispatchQueueId = queue.Id,
            ClearingHouseId = clearingHouseId,
            ExecutionMode = "Live",
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed,
            ProcessedAtUtc = DateTime.UtcNow,
            ResponseCatalogId = 5,
            TransportStatus = IntegrationTransportStatus.Succeeded,
            BusinessStatus = IntegrationResponseBusinessStatus.Rejected,
            IsFunctionalRejection = true,
            IsSuccessful = false,
            IsTechnicalFailure = false,
            RetryAllowed = false,
            RequiresManualReview = false,
            ResultCode = "CORE_123"
        };
        return (queue, execution);
    }
}
