using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchOutboundReturnConcurrencyIdempotencyTests
{
    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldPreventDuplicateRows_WithSeparateDbContexts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();

        var options = BuildSqliteOptions(connection);
        await using (var seedContext = new AchDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            SeedSqliteScenario(seedContext, 9101, "ACH-SQL-1");
        }

        await using var contextA = new AchDbContext(options);
        await using var contextB = new AchDbContext(options);

        var request = new GenerateReturnsFileRequest("ACH-SQL-1", [new ReturnSelectionItemDto(9101, "DEV14")]);
        var sutA = BuildSut(contextA, 9101, "DEV14", new AchReturnGenerationLockService());
        var sutB = BuildSut(contextB, 9101, "DEV14", new AchReturnGenerationLockService());

        await sutA.GenerateReturnsFileAsync(request, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sutB.GenerateReturnsFileAsync(request, CancellationToken.None));

        Assert.Contains("devolución registrada", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var assertContext = new AchDbContext(options);
        Assert.Equal(1, await assertContext.Set<AchReturnGenerated>().CountAsync(x => x.OriginalTransactionId == 9101));
        Assert.Equal(1, await assertContext.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 9101));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldPreventDuplicateRows_WhenTwoServiceInstancesRace()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();

        var options = BuildSqliteOptions(connection);
        await using (var seedContext = new AchDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            SeedSqliteScenario(seedContext, 9102, "ACH-SQL-2");
        }

        await using var contextA = new AchDbContext(options);
        await using var contextB = new AchDbContext(options);

        var request = new GenerateReturnsFileRequest("ACH-SQL-2", [new ReturnSelectionItemDto(9102, "DEV14")]);
        var sutA = BuildSut(contextA, 9102, "DEV14", new AchReturnGenerationLockService());
        var sutB = BuildSut(contextB, 9102, "DEV14", new AchReturnGenerationLockService());

        var t1 = ExecuteIgnoringFailureAsync(() => sutA.GenerateReturnsFileAsync(request, CancellationToken.None));
        var t2 = ExecuteIgnoringFailureAsync(() => sutB.GenerateReturnsFileAsync(request, CancellationToken.None));
        await Task.WhenAll(t1, t2);
        var r1 = await t1;
        var r2 = await t2;

        Assert.True(r1.Succeeded ^ r2.Succeeded);
        var error = r1.Succeeded ? r2.Error : r1.Error;
        Assert.NotNull(error);
        Assert.Contains("devolución registrada", error!.Message, StringComparison.OrdinalIgnoreCase);

        await using var assertContext = new AchDbContext(options);
        Assert.Equal(1, await assertContext.Set<AchReturnGenerated>().CountAsync(x => x.OriginalTransactionId == 9102));
        Assert.Equal(1, await assertContext.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 9102));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldAllowSameTransactionDifferentReason_OnlyAsCurrentBusinessRule()
    {
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        SeedSqliteScenario(context, 9103, "ACH-SQL-3");
        var request = new GenerateReturnsFileRequest("ACH-SQL-3", [new ReturnSelectionItemDto(9103, "DEV14")]);
        var sut = BuildSut(context, 9103, "DEV14", new TestReturnGenerationLockService());

        await sut.GenerateReturnsFileAsync(request, CancellationToken.None);

        var secondReasonRequest = new GenerateReturnsFileRequest("ACH-SQL-3", [new ReturnSelectionItemDto(9103, "R01")]);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(secondReasonRequest, CancellationToken.None));

        Assert.Contains("ya cuenta con una devolución registrada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AchReturnGeneratedUniqueIndex_ShouldBeEnforced_BySqliteModel()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();

        var options = BuildSqliteOptions(connection);
        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        SeedSqliteScenario(context, 9104, "ACH-SQL-4");
        context.Set<AchReturnGenerated>().Add(new AchReturnGenerated
        {
            OriginalTransactionId = 9104,
            ReturnCycleId = "ACH-SQL-4",
            ReturnReasonCode = "DEV14",
            Amount = 1,
            NewSequenceNumber = "000000000000001",
            OriginalSequenceNumber = "000000000000002",
            ReceiverEntityCode = "09100001",
            OriginatorEntityCode = "09100002",
            FileName = "A.RET",
            GeneratedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        context.Set<AchReturnGenerated>().Add(new AchReturnGenerated
        {
            OriginalTransactionId = 9104,
            ReturnCycleId = "ACH-SQL-4",
            ReturnReasonCode = "DEV14",
            Amount = 1,
            NewSequenceNumber = "000000000000003",
            OriginalSequenceNumber = "000000000000004",
            ReceiverEntityCode = "09100001",
            OriginatorEntityCode = "09100002",
            FileName = "B.RET",
            GeneratedAtUtc = DateTime.UtcNow
        });

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static DbContextOptions<AchDbContext> BuildSqliteOptions(SqliteConnection connection)
        => new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;

    private static AchReturnsService BuildSut(AchDbContext context, int txId, string reasonCode, IAchReturnGenerationLockService lockService)
    {
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.Is<AchReturnEligibilityRequest>(r => r.TransactionId == txId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, reasonCode, 7002, "Debit", "Pending", []));

        return new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: lockService,
            externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(),
            nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
    }

    private static void SeedSqliteScenario(AchDbContext context, int transactionId, string cycleId)
    {
        if (!context.ClearingHouses.Any(x => x.Id == 7002))
        {
            context.ClearingHouses.Add(new ClearingHouse { Id = 7002, Code = "ACH", Name = "ACH Colombia", OriginCode = "901289999" });
        }

        if (!context.AchCycles.Any(x => x.Id == cycleId))
        {
            context.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = cycleId, ProcessingDate = new DateTime(2026, 05, 01), ClearingHouseId = 7002, CutoffTime = new TimeSpan(12, 0, 0) });
        }

        if (!context.FinancialInstitutions.Any(x => x.Id == 1))
        {
            var source = new FinancialInstitution
            {
                Id = 1,
                Name = "Bank Source",
                RoutingNumber = "0910",
                TransitCode = "0001"
            };
            source.CalculateCheckDigit();
            context.FinancialInstitutions.Add(source);
        }

        if (!context.FinancialInstitutions.Any(x => x.Id == 2))
        {
            var destination = new FinancialInstitution
            {
                Id = 2,
                Name = "Bank Destination",
                RoutingNumber = "0910",
                TransitCode = "0002"
            };
            destination.CalculateCheckDigit();
            context.FinancialInstitutions.Add(destination);
        }

        if (!context.AchBatches.Any(x => x.Id == 1))
        {
            context.AchBatches.Add(new AchBatch
            {
                Id = 1,
                AchCycleId = cycleId,
                CompanyEntryDescriptionId = 1,
                CompanyName = "ACME",
                CompanyIdentification = "1234567890",
                OriginOrOdfi = "09100001",
                EffectiveEntryDate = new DateTime(2026, 05, 01),
                BatchSequenceNumber = 1
            });
        }

        context.AchTransactions.Add(new AchTransaction
        {
            Id = transactionId,
            Amount = 125.55m,
            TransactionExternalId = $"EXT-{transactionId}",
            Reference = $"REF-{transactionId}",
            Type = TransactionTypeEnum.Debit,
            TransactionCode = "27",
            ServiceClassCode = "200",
            CompanyEntryDescriptionId = 1,
            CompanyName = "ACME",
            CompanyIdentification = "1234567890",
            OriginatingDFI = "09100001",
            ReceivingDFI = "09100002",
            TraceNumber = "091000020000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = new DateTime(2026, 05, 01),
            AddendaRecordIndicator = true,
            IsPrenotification = false,
            State = AchTransferStateEnum.Pending,
            StateChangedAtUtc = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycleId,
            AchBatchId = 1
        });

        context.SaveChanges();
    }

    private static async Task<(bool Succeeded, Exception? Error)> ExecuteIgnoringFailureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }
}
