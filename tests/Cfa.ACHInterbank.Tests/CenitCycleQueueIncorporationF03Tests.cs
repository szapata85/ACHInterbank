using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

    private static async Task VerifyPendingAsync(SqliteConnection connection)
    {
        await using var verify = Context(connection);
        Assert.Equal("Queued", (await verify.CenitCycleQueues.SingleAsync()).Status);
        Assert.Equal("C1", (await verify.AchTransactions.SingleAsync(x => x.Id == 100)).AchCycleId);
        Assert.Null((await verify.CenitCycleQueues.SingleAsync()).DequeuedAtUtc);
    }

    private static async Task SeedAsync(AchDbContext db)
    {
        db.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
        db.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        db.AchCycles.AddRange(
            new AchCycle { Id = "C1", ClearingHouseId = 2, CycleName = "Ciclo 1", ProcessingDate = ValueDate },
            new AchCycle { Id = "C2", ClearingHouseId = 2, CycleName = "Ciclo 2", ProcessingDate = ValueDate });
        db.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        { Id = 9001, Term = "PAGO", Description = "Pago", IsActive = true, StandardEntryClassCode = "PPD" });
        var a = new FinancialInstitution { Id = 1, Name = "A", RoutingNumber = "1234", TransitCode = "5678", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active };
        var b = new FinancialInstitution { Id = 2, Name = "B", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active };
        a.CalculateCheckDigit(); b.CalculateCheckDigit();
        db.FinancialInstitutions.AddRange(a, b);
        db.AchBatches.Add(new AchBatch { Id = 77, AchCycleId = "C1", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescription = "PAGO", CompanyEntryDescriptionId = 9001, OriginOrOdfi = "12345678", EffectiveEntryDate = ValueDate });
        foreach (var (id, amount) in new[] { (100, 100m), (101, 150m) })
        {
            db.AchTransactions.Add(new AchTransaction
            {
                Id = id, Amount = amount, Reference = $"ref{id}", TransactionExternalId = $"op{id}",
                Type = TransactionTypeEnum.Debit, State = AchTransferStateEnum.Pending,
                AchCycleId = "C1", AchBatchId = 77, CompanyName = "C", CompanyIdentification = "N",
                CompanyEntryDescriptionId = 9001, OriginatingDFI = "12345678", ReceivingDFI = "87654321",
                TraceNumber = $"1234567800000{id:000}", SourceAccountNumber = "1", DestinationAccountNumber = "2",
                SourceInstitutionId = 1, DestinationInstitutionId = 2, EffectiveEntryDate = ValueDate
            });
        }
        db.CenitCycleQueues.Add(new CenitCycleQueue
        {
            AchTransactionId = 100, OriginalAchCycleId = "C1", TargetAchCycleId = "C2",
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
