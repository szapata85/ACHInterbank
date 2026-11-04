using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaReturnReviewI04Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 15, 25, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApprovingCandidateOnlyChangesReviewStatusAndNotTheOriginalOrFinancialFiles()
    {
        await using var db = BuildDb();
        Seed(db);
        var service = NewService(db);
        var row = Assert.Single(await service.ListAsync(50));
        Assert.True(row.CanApprove);
        var result = await service.DecideAsync(row.Id, new("APROBAR"));
        Assert.Equal(AchColombiaReturnReviewService.Approved, result.Status);
        Assert.Empty(db.AchReturnsGenerated);
        Assert.Equal(AchTransferStateEnum.Pending, (await db.AchTransactions.SingleAsync()).State);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => service.DecideAsync(row.Id, new("APROBAR")));
    }

    [Fact]
    public async Task ManualReturnBlocksApprovalButDiscardLeavesTraceability()
    {
        await using var db = BuildDb();
        Seed(db);
        db.AchReturnsGenerated.Add(new AchReturnGenerated
        {
            OriginalTransactionId = 77, ReturnCycleId = "C4", ReturnReasonCode = "R02",
            NewSequenceNumber = "000000000000001", OriginalSequenceNumber = "000000000000077",
            FileName = "manual.ret"
        });
        await db.SaveChangesAsync();
        var service = NewService(db);
        Assert.False(Assert.Single(await service.ListAsync(50)).CanApprove);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => service.DecideAsync(1, new("APROBAR")));
        Assert.Equal(AchColombiaReturnReviewService.Discarded,
            (await service.DecideAsync(1, new("DESCARTAR"))).Status);
        Assert.Single(db.AchReturnsGenerated);
    }

    [Fact]
    public async Task DisabledModeAndLateCreditCannotBeApproved()
    {
        await using var db = BuildDb();
        Seed(db);
        db.ClearingHouseConfigs.Single().AutomaticReturnMode = "DESACTIVADO";
        await db.SaveChangesAsync();
        var service = NewService(db);
        Assert.False(Assert.Single(await service.ListAsync(50)).CanApprove);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => service.DecideAsync(1, new("APROBAR")));
        db.ClearingHouseConfigs.Single().AutomaticReturnMode = "CON_APROBACION";
        db.AchColombiaReturnPreparations.Single().Status = AchColombiaReturnPreparationService.LateCreditReview;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => service.DecideAsync(1, new("APROBAR")));
    }

    [Fact]
    public async Task CenitCandidateNeverAppearsInAchColombiaReview()
    {
        await using var db = BuildDb();
        Seed(db, "CENIT");
        var service = NewService(db);
        Assert.Empty(await service.ListAsync(50));
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => service.DecideAsync(1, new("DESCARTAR")));
    }

    [Fact]
    public async Task R10RequiresAdditionalProofAndIsNotApprovedImplicitly()
    {
        await using var db = BuildDb();
        Seed(db);
        db.AchColombiaReturnPreparations.Single().ReasonCode = "R10";
        await db.SaveChangesAsync();
        var service = NewService(db);
        Assert.False(Assert.Single(await service.ListAsync(50)).CanApprove);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => service.DecideAsync(1, new("APROBAR")));
    }

    [Fact]
    public void PreparationStatusIsAnEfConcurrencyToken()
    {
        using var db = BuildDb();
        var entity = db.Model.FindEntityType(typeof(AchColombiaReturnPreparation));
        Assert.True(entity!.FindProperty(nameof(AchColombiaReturnPreparation.Status))!.IsConcurrencyToken);
    }

    private static AchColombiaReturnReviewService NewService(AchDbContext db) =>
        new(db, new TestClock(), Moq.Mock.Of<IAchReturnsService>());

    private static AchDbContext BuildDb() => new(
        new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"i04-ach-{Guid.NewGuid():N}").Options);

    private static void Seed(AchDbContext db, string camera = "ACHCOL")
    {
        db.ClearingHouses.Add(new ClearingHouse
        {
            Id = 41, Code = camera, Name = camera, OriginCode = "00000000",
            ClearingHouseId = 41
        });
        db.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 41, ClearingHouseId = 41, AutomaticReturnMode = "CON_APROBACION"
        });
        for (var i = 1; i <= 5; i++)
        {
            db.AchCycles.Add(new AchCycle
            {
                Id = $"C{i}", ClearingHouseId = 41, CycleName = $"Ciclo {i}",
                ProcessingDate = new DateTime(2026, 11, 2),
                StartTime = TimeSpan.FromHours(6 + i), EndTime = TimeSpan.FromHours(7 + i),
                OperationalStatus = i <= 2 ? AchCycleOperationalStatus.Closed : AchCycleOperationalStatus.Scheduled,
                ClearingHouseCycleConfig = new ClearingHouseCycleConfig
                {
                    Id = 100 + i, ClearingHouseId = 41, CycleName = $"Ciclo {i}", AllowsReturn = true
                }
            });
        }
        var original = new AchTransaction
        {
            Id = 77, AchCycleId = "C1", Type = TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.Pending, EffectiveEntryDate = new DateTime(2026, 11, 2),
            Reference = "REF-77", Amount = 500000m,
            SourceAccountNumber = "123456", DestinationAccountNumber = "654321"
        };
        db.AchTransactions.Add(original);
        db.ReturnReasons.Add(new ReturnReason { Code = "R02", Description = "Cuenta cerrada", IsForReturn = true });
        db.Set<AchReturnCode>().Add(new AchReturnCode
        {
            Id = 8, ClearingHouseId = 41, Code = "R02", IsActive = true, AppliesToCredit = true
        });
        var evt = new IncomingNachaProcessingEvent
        {
            Id = Guid.NewGuid(), AchTransactionId = 77,
            IncomingNachaFileIngestionId = Guid.NewGuid(),
            EventType = AchColombiaApplicationFailureDetector.DetectedEvent,
            RaisedBy = "achcol.application-failure.detector", EventStatus = "Detectada",
            OccurredAtUtc = Now.UtcDateTime, Message = "Rechazo funcional",
            EvidenceJson = JsonSerializer.Serialize(new
            {
                camera = "ACHCOL", origin = "CORE_FUNCIONAL", transactionId = 77,
                causeVerified = true, returnGenerated = false, returnSent = false,
                suggestedReturnReason = "R02", responseCode = "R02"
            })
        };
        db.IncomingNachaProcessingEvents.Add(evt);
        db.AchColombiaReturnPreparations.Add(new AchColombiaReturnPreparation
        {
            Id = 1, OriginalTransactionId = 77, SourceEventId = evt.Id,
            OriginalCycleId = "C1", SuggestedReturnCycleId = "C4", ReasonCode = "R02",
            Status = AchColombiaReturnPreparationService.Prepared, PreparedAtUtc = Now.UtcDateTime
        });
        db.SaveChanges();
    }

    private sealed class TestClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
