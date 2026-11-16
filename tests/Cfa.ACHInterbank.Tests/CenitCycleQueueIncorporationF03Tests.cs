using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitCycleQueueIncorporationF03Tests
{
    private static readonly DateTime ValueDate = new(2026, 11, 2);

    [Fact]
    public async Task QueuedOperation_ChangesCycleAndBatchBeforeExport_WithoutChangingFinancialState()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        var sut = new CenitCycleQueueService(null!, db);

        Assert.Equal(1, await sut.IncorporateForCycleAsync("C2", CancellationToken.None));
        db.ChangeTracker.Clear();
        var moved = await db.AchTransactions.SingleAsync(x => x.Id == 100);
        var other = await db.AchTransactions.SingleAsync(x => x.Id == 101);
        var targetBatch = await db.AchBatches.SingleAsync(x => x.Id == moved.AchBatchId);
        var sourceBatch = await db.AchBatches.SingleAsync(x => x.Id == 77);
        var queue = await db.CenitCycleQueues.SingleAsync();
        var evidence = await db.AchTransactionStateEvents.SingleAsync();

        Assert.Equal("C2", moved.AchCycleId);
        Assert.Equal("C2", targetBatch.AchCycleId);
        Assert.Equal(ValueDate, moved.EffectiveEntryDate);
        Assert.Equal(AchTransferStateEnum.Pending, moved.State);
        Assert.Equal("C1", other.AchCycleId);
        Assert.Equal(150m, sourceBatch.TotalDebitAmount);
        Assert.Equal(100m, targetBatch.TotalDebitAmount);
        Assert.Equal("Consumed", queue.Status);
        Assert.NotNull(queue.DequeuedAtUtc);
        Assert.Equal("CENIT_QUEUE_ATTACHED", evidence.ReasonCode);
        Assert.Equal(AchTransferStateEnum.Pending, evidence.FromState);
        Assert.Equal(AchTransferStateEnum.Pending, evidence.ToState);
        Assert.Equal(0, await sut.IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.Single(await db.AchTransactionStateEvents.ToListAsync());
        Assert.Single(await db.AchBatches.Where(x => x.AchCycleId == "C2").ToListAsync());
    }

    [Fact]
    public async Task TargetExportAlreadyExists_LeavesQueueAndTransactionUnchanged()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        db.AchFileExports.Add(new AchFileExport { AchCycleId = "C2", ClearingHouseId = 2, ExportKind = "NACHA", Version = 1 });
        await db.SaveChangesAsync();
        var sut = new CenitCycleQueueService(null!, db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.Equal("CENIT_QUEUE_TARGET_ALREADY_EXPORTED", ex.Message);
        await VerifyPendingAsync(connection);
    }

    [Fact]
    public async Task PresentedSourceTransaction_IsNotQueuedIntoAnotherCycle()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        db.ContrapartidaDispatchItems.Add(new ContrapartidaDispatchItem
        {
            AchTransactionId = 100, AchCycleId = "C1", AchBatchId = 77, ClearingHouseId = 2
        });
        await db.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CenitCycleQueueService(null!, db).IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.StartsWith("CENIT_QUEUE_ALREADY_PRESENTED", ex.Message);
        await VerifyPendingAsync(connection);
    }

    [Fact]
    public async Task SqlFailureOnAuditEvent_RollsBackBatchMovementAndQueueStatus()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER f03_fail_event BEFORE INSERT ON AchTransactionStateEvents BEGIN SELECT RAISE(ABORT, 'F03 test failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new CenitCycleQueueService(null!, db).IncorporateForCycleAsync("C2", CancellationToken.None));
        await VerifyPendingAsync(connection);
        await using var verify = Context(connection);
        Assert.Empty(await verify.AchBatches.Where(x => x.AchCycleId == "C2").ToListAsync());
        Assert.Empty(await verify.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task DifferentBusinessDay_DoesNotSilentlyChangeValueDate()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        (await db.AchCycles.SingleAsync(x => x.Id == "C2")).ProcessingDate = ValueDate.AddDays(1);
        await db.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CenitCycleQueueService(null!, db).IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.StartsWith("CENIT_QUEUE_VALUE_DATE_REVIEW_REQUIRED", ex.Message);
        await VerifyPendingAsync(connection);
    }

    [Fact]
    public async Task InvalidSecondQueue_RollsBackFirstQueueAndItsNewBatch()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        db.CenitCycleQueues.Add(new CenitCycleQueue
        {
            AchTransactionId = 101, OriginalAchCycleId = "C1", TargetAchCycleId = "C2",
            QueueReason = "OutsideOperatingWindowRoutedToNextCycle", Status = "Queued"
        });
        (await db.AchTransactions.SingleAsync(x => x.Id == 101)).State = AchTransferStateEnum.ReturnedByOperator;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CenitCycleQueueService(null!, db).IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.StartsWith("CENIT_QUEUE_SOURCE_CHANGED", ex.Message);
        await using var verification = Context(connection);
        Assert.Equal(2, await verification.CenitCycleQueues.CountAsync(x => x.Status == "Queued"));
        Assert.Equal("C1", (await verification.AchTransactions.SingleAsync(x => x.Id == 100)).AchCycleId);
        Assert.Empty(await verification.AchBatches.Where(x => x.AchCycleId == "C2").ToListAsync());
        Assert.Empty(await verification.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task AnotherChamber_RejectsDirectQueueProcessing()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        (await db.ClearingHouses.SingleAsync()).Code = "ACH";
        await db.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CenitCycleQueueService(null!, db).IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.Equal("CENIT_QUEUE_WRONG_CHAMBER", ex.Message);
        await VerifyPendingAsync(connection);
    }

    [Fact]
    public async Task AlreadyAssignedToTargetBeforeWindow_IsAcknowledgedOnceWithoutDuplicateBatch()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        db.CenitCycleQueues.Remove(await db.CenitCycleQueues.SingleAsync());
        var tx = await db.AchTransactions.SingleAsync(x => x.Id == 100);
        tx.AchCycleId = "C2";
        var batch = await db.AchBatches.SingleAsync(x => x.Id == 77);
        batch.AchCycleId = "C2";
        db.CenitCycleQueues.Add(new CenitCycleQueue
        {
            AchTransactionId = 100, OriginalAchCycleId = "C2", TargetAchCycleId = "C2",
            QueueReason = "BeforeWindow", Status = "Queued"
        });
        await db.SaveChangesAsync();
        var sut = new CenitCycleQueueService(null!, db);
        Assert.Equal(1, await sut.IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.Equal(0, await sut.IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.Single(await db.AchTransactionStateEvents.ToListAsync());
        Assert.Equal("Consumed", (await db.CenitCycleQueues.SingleAsync()).Status);
    }

    // I01 / PT-02: una entrada ordinaria tardía de C4 jamás consume la cola C5,
    // incluso si una política de ciclos configurada erradamente la clasificara como permitida.
    [Fact]
    public async Task FifthSession_RejectsOrdinaryMonetaryBeforeConsumingQueue()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db, "C4", "C5", 4, 5);
        var result = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CenitCycleQueueService(null!, db, AllowedPolicy("MonetaryDebit"))
                .IncorporateForCycleAsync("C5", CancellationToken.None));
        Assert.StartsWith("CENIT_QUEUE_CYCLE5_NOT_ALLOWED", result.Message);
        await AssertStillQueuedInSourceAsync(connection, "C4", "C5");
    }

    // I01 / PT-01 y PT-21: devolución de C4 habilitada para C5, exactamente una vez.
    [Fact]
    public async Task FifthSession_EligibleReturnFromFourthSession_IsMovedExactlyOnce()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db, "C4", "C5", 4, 5, TransactionTypeEnum.Return);
        var sut = new CenitCycleQueueService(null!, db, AllowedPolicy("Return"));
        Assert.Equal(1, await sut.IncorporateForCycleAsync("C5", CancellationToken.None));
        Assert.Equal(0, await sut.IncorporateForCycleAsync("C5", CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.Equal("Consumed", (await db.CenitCycleQueues.SingleAsync()).Status);
        Assert.Equal("C5", (await db.AchTransactions.SingleAsync(x => x.Id == 100)).AchCycleId);
        Assert.Equal(ValueDate, (await db.AchTransactions.SingleAsync(x => x.Id == 100)).EffectiveEntryDate);
        Assert.Single(await db.AchTransactionStateEvents.ToListAsync());
        Assert.Single(await db.AchBatches.Where(x => x.AchCycleId == "C5").ToListAsync());
    }

    // I01 / PT-02: el rechazo de la política vigente también deja intacta la cola.
    [Fact]
    public async Task FifthSession_DeniedReturnPolicy_RemainsQueued()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db, "C4", "C5", 4, 5, TransactionTypeEnum.Return);
        var policy = new Mock<ICycleTransactionPolicy>();
        policy.Setup(x => x.EvaluateAsync(It.IsAny<CycleTransactionPolicyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CycleTransactionPolicyResult(false, "CYCLE_TRANSACTION_NOT_ALLOWED",
                "Denied by fixture", "CENIT", 5, "Return"));
        var result = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CenitCycleQueueService(null!, db, policy.Object)
                .IncorporateForCycleAsync("C5", CancellationToken.None));
        Assert.Contains("CYCLE_TRANSACTION_NOT_ALLOWED", result.Message);
        await AssertStillQueuedInSourceAsync(connection, "C4", "C5");
    }

    // I01 / PT-20: si el segundo elemento viola C5, el primero tampoco se consume.
    [Fact]
    public async Task FifthSession_MixedValidReturnAndMonetary_RollsBackAllQueueMovements()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db, "C4", "C5", 4, 5, TransactionTypeEnum.Return);
        db.CenitCycleQueues.Add(new CenitCycleQueue
        {
            AchTransactionId = 101, OriginalAchCycleId = "C4", TargetAchCycleId = "C5",
            QueueReason = "OutsideOperatingWindowRoutedToNextCycle", Status = "Queued"
        });
        await db.SaveChangesAsync();
        var policy = new Mock<ICycleTransactionPolicy>();
        policy.Setup(x => x.EvaluateAsync(It.IsAny<CycleTransactionPolicyRequest>(), It.IsAny<CancellationToken>()))
            .Returns((CycleTransactionPolicyRequest request, CancellationToken _) =>
                Task.FromResult(AllowedDecision(request.TransactionType == TransactionTypeEnum.Return ? "Return" : "MonetaryDebit")));
        var result = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CenitCycleQueueService(null!, db, policy.Object)
                .IncorporateForCycleAsync("C5", CancellationToken.None));
        Assert.StartsWith("CENIT_QUEUE_CYCLE5_NOT_ALLOWED", result.Message);
        await AssertStillQueuedInSourceAsync(connection, "C4", "C5", expectedCount: 2);
    }

    // I01 / PT-02: el número se obtiene de CycleName, no del identificador de BD.
    [Fact]
    public async Task UnparseableCenitSession_FailsClosedWithoutMovingQueue()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = Context(connection);
        await SeedAsync(db);
        (await db.AchCycles.SingleAsync(x => x.Id == "C2")).CycleName = "Sesión desconocida";
        await db.SaveChangesAsync();
        var result = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CenitCycleQueueService(null!, db).IncorporateForCycleAsync("C2", CancellationToken.None));
        Assert.Equal("CENIT_QUEUE_TARGET_SESSION_UNRESOLVED", result.Message);
        await VerifyPendingAsync(connection);
    }

    private static ICycleTransactionPolicy AllowedPolicy(string functionalClass)
    {
        var policy = new Mock<ICycleTransactionPolicy>();
        policy.Setup(x => x.EvaluateAsync(It.IsAny<CycleTransactionPolicyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllowedDecision(functionalClass));
        return policy.Object;
    }

    private static CycleTransactionPolicyResult AllowedDecision(string functionalClass)
        => new(true, "ALLOWED", "Permitted by fixture", "CENIT", 5, functionalClass);

    private static async Task AssertStillQueuedInSourceAsync(
        SqliteConnection connection, string sourceId, string targetId, int expectedCount = 1)
    {
        await using var verification = Context(connection);
        Assert.Equal(expectedCount, await verification.CenitCycleQueues.CountAsync(x =>
            x.TargetAchCycleId == targetId && x.Status == "Queued"));
        Assert.Equal(sourceId, (await verification.AchTransactions.SingleAsync(x => x.Id == 100)).AchCycleId);
        Assert.Empty(await verification.AchBatches.Where(x => x.AchCycleId == targetId).ToListAsync());
        Assert.Empty(await verification.AchTransactionStateEvents.ToListAsync());
    }

    private static async Task VerifyPendingAsync(SqliteConnection connection)
    {
        await using var verify = Context(connection);
        Assert.Equal("Queued", (await verify.CenitCycleQueues.SingleAsync()).Status);
        Assert.Equal("C1", (await verify.AchTransactions.SingleAsync(x => x.Id == 100)).AchCycleId);
        Assert.Null((await verify.CenitCycleQueues.SingleAsync()).DequeuedAtUtc);
    }

    private static async Task SeedAsync(
        AchDbContext db,
        string sourceCycleId = "C1",
        string targetCycleId = "C2",
        int sourceSession = 1,
        int targetSession = 2,
        TransactionTypeEnum queuedTransactionType = TransactionTypeEnum.Debit)
    {
        db.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
        db.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        db.AchCycles.AddRange(
            new AchCycle { Id = sourceCycleId, ClearingHouseId = 2, CycleName = $"Ciclo {sourceSession}", ProcessingDate = ValueDate },
            new AchCycle { Id = targetCycleId, ClearingHouseId = 2, CycleName = $"Ciclo {targetSession}", ProcessingDate = ValueDate });
        db.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        { Id = 9001, Term = "PAGO", Description = "Pago", IsActive = true, StandardEntryClassCode = "PPD" });
        var a = new FinancialInstitution { Id = 1, Name = "A", RoutingNumber = "1234", TransitCode = "5678", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active };
        var b = new FinancialInstitution { Id = 2, Name = "B", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active };
        a.CalculateCheckDigit(); b.CalculateCheckDigit();
        db.FinancialInstitutions.AddRange(a, b);
        db.AchBatches.Add(new AchBatch { Id = 77, AchCycleId = sourceCycleId, CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescription = "PAGO", CompanyEntryDescriptionId = 9001, OriginOrOdfi = "12345678", EffectiveEntryDate = ValueDate });
        foreach (var (id, amount) in new[] { (100, 100m), (101, 150m) })
        {
            db.AchTransactions.Add(new AchTransaction
            {
                Id = id, Amount = amount, Reference = $"ref{id}", TransactionExternalId = $"op{id}",
                Type = id == 100 ? queuedTransactionType : TransactionTypeEnum.Debit, State = AchTransferStateEnum.Pending,
                ReturnReasonCode = id == 100 && queuedTransactionType == TransactionTypeEnum.Return ? "R06" : string.Empty,
                AchCycleId = sourceCycleId, AchBatchId = 77, CompanyName = "C", CompanyIdentification = "N",
                CompanyEntryDescriptionId = 9001, OriginatingDFI = "12345678", ReceivingDFI = "87654321",
                TraceNumber = $"1234567800000{id:000}", SourceAccountNumber = "1", DestinationAccountNumber = "2",
                SourceInstitutionId = 1, DestinationInstitutionId = 2, EffectiveEntryDate = ValueDate
            });
        }
        db.CenitCycleQueues.Add(new CenitCycleQueue
        {
            AchTransactionId = 100, OriginalAchCycleId = sourceCycleId, TargetAchCycleId = targetCycleId,
            QueueReason = "OutsideOperatingWindowRoutedToNextCycle", Status = "Queued"
        });
        await db.SaveChangesAsync();
    }

    private static AchDbContext Context(SqliteConnection connection)
    {
        var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return db;
    }
}
