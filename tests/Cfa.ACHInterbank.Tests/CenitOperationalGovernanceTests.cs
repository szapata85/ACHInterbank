using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class CenitOperationalGovernanceTests
{
    [Fact]
    public async Task CenitCalendarPolicy_Throws_WhenCycleCountIsNotFive()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        context.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        context.ClearingHouseCycleConfigs.AddRange(
            new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 1", IsActive = true, EffectiveFrom = DateTime.UtcNow.Date, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 0, 0), CutoffTime = new TimeSpan(9, 0, 0) },
            new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 2", IsActive = true, EffectiveFrom = DateTime.UtcNow.Date, StartTime = new TimeSpan(9, 1, 0), EndTime = new TimeSpan(10, 0, 0), CutoffTime = new TimeSpan(10, 0, 0) });
        await context.SaveChangesAsync();

        var sut = new CenitOperatingCalendarPolicy(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ValidateCycleConsistencyAsync(2, DateTime.UtcNow.Date, CancellationToken.None));
    }

    [Fact]
    public async Task BatchResolver_MarksOutsideWindowForQueue_WhenCenitCycleNotOpenYet()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        context.FinancialInstitutions.AddRange(
            new FinancialInstitution { Id = 1, Name = "Banco Origen", IsDefaultSource = true, RoutingNumber = "1234", TransitCode = "5678", Status = FinancialInstitutionStatus.Active },
            new FinancialInstitution { Id = 2, Name = "Banco Destino", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active });
        context.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        context.AchCycles.Add(new AchCycle
        {
            Id = "cycle-1",
            ClearingHouseId = 2,
            CycleName = "Ciclo 2",
            ProcessingDate = DateTime.UtcNow.Date,
            StartTime = DateTime.UtcNow.AddHours(2).TimeOfDay,
            EndTime = DateTime.UtcNow.AddHours(4).TimeOfDay,
            CutoffTime = DateTime.UtcNow.AddHours(4).TimeOfDay
        });
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog { Id = 1, Term = "PAGO", Description = "Pago", IsActive = true, StandardEntryClassCode = "PPD" });
        await context.SaveChangesAsync();

        var batchRepo = new Mock<IAchBatchRepository>();
        batchRepo.Setup(x => x.FindForTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchBatch { Id = 9, AchCycleId = "cycle-1", CompanyName = "Comp", CompanyIdentification = "NIT", CompanyEntryDescription = "PAGO", CompanyEntryDescriptionId = 1, EffectiveEntryDate = DateTime.UtcNow.Date, OriginOrOdfi = "12345678" });
        batchRepo.Setup(x => x.GetUpcomingCyclesAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchCycle>());

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cycle-1");

        var sut = new BatchResolver(context, batchRepo.Object, routing.Object);
        var result = await sut.ResolveAsync(new AchTransactionRequestData
        {
            Amount = 10,
            Reference = "R1",
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "111",
            DestinationAccountNumber = "222",
            CompanyName = "Comp",
            CompanyIdentification = "NIT",
            CompanyEntryDescriptionId = 1
        }, CancellationToken.None);

        Assert.True(result.MustQueueForTargetCycle);
        Assert.Equal("OutsideOperatingWindowRoutedToNextCycle", result.QueueReason);
    }

    [Fact]
    public async Task LiquidityOptimization_DeferredInCycle2_MovesTransactionToNextCycle()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var setup = await SeedCycleExecutionScenarioAsync(context, "Ciclo 2", availableLiquidity: 0m);
        var sut = new LiquidityOptimizationService(context, new TransactionPriorityPolicy());

        var decisions = await sut.OptimizeCycleAsync(setup.Execution, CancellationToken.None);

        Assert.Contains(decisions, x => x.DecisionType == "Deferred");
        var tx = await context.AchTransactions.FirstAsync();
        Assert.Equal(setup.NextCycleId, tx.AchCycleId);
        Assert.True(await context.CenitCycleQueues.AnyAsync(x => x.QueueReason == "LiquidityDeferredByRule123"));
    }

    [Fact]
    public async Task LiquidityOptimization_RejectsInCycle4_UpdatesState()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var setup = await SeedCycleExecutionScenarioAsync(context, "Ciclo 4", availableLiquidity: 0m);
        var sut = new LiquidityOptimizationService(context, new TransactionPriorityPolicy());

        var decisions = await sut.OptimizeCycleAsync(setup.Execution, CancellationToken.None);

        Assert.Contains(decisions, x => x.DecisionType == "Rejected");
        var tx = await context.AchTransactions.FirstAsync();
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator, tx.State);
        Assert.Equal("DXX-LIQ", tx.ReturnReasonCode);
    }

    [Fact]
    public async Task ReturnOfReturnOrchestrator_RejectsExpiredDeadline()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var source = new AchTransaction { Id = 10, Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.ReturnedByOperator, SlaDeadlineAtUtc = DateTime.UtcNow.AddMinutes(-1), AchCycleId = "c1", Reference = "r", TransactionExternalId = "op-r", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = 1, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000010", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, AchBatchId = 1, EffectiveEntryDate = DateTime.UtcNow.Date };
        var ror = new AchTransaction { Id = 11, Type = TransactionTypeEnum.Return, AchCycleId = "c1", Reference = "r2", TransactionExternalId = "op-r2", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = 1, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000011", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, AchBatchId = 1, EffectiveEntryDate = DateTime.UtcNow.Date };
        context.AchTransactions.AddRange(source, ror);
        await context.SaveChangesAsync();

        var sut = new ReturnOfReturnOrchestrator(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(source, ror, "R01", CancellationToken.None));
    }

    [Fact]
    public async Task ReturnOfReturnOrchestrator_RejectsDuplicateFlow()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var source = new AchTransaction { Id = 21, Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.ReturnedByOperator, SlaDeadlineAtUtc = DateTime.UtcNow.AddHours(2), AchCycleId = "c1", Reference = "r21", TransactionExternalId = "op21", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = 1, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000021", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, AchBatchId = 1, EffectiveEntryDate = DateTime.UtcNow.Date };
        var ror = new AchTransaction { Id = 22, Type = TransactionTypeEnum.Return, AchCycleId = "c1", Reference = "r22", TransactionExternalId = "op22", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = 1, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000022", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, AchBatchId = 1, EffectiveEntryDate = DateTime.UtcNow.Date };
        context.AchTransactions.AddRange(source, ror);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { SourceReturnTransactionId = 21, ReturnOfReturnTransactionId = 22, ReasonCode = "R01" });
        await context.SaveChangesAsync();

        var sut = new ReturnOfReturnOrchestrator(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(source, ror, "R01", CancellationToken.None));
    }

    private static async Task<(CenitCycleExecution Execution, string NextCycleId)> SeedCycleExecutionScenarioAsync(AchDbContext context, string cycleName, decimal availableLiquidity)
    {
        context.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        context.FinancialInstitutions.AddRange(
            new FinancialInstitution { Id = 1, Name = "A", RoutingNumber = "1234", TransitCode = "5678", Status = FinancialInstitutionStatus.Active },
            new FinancialInstitution { Id = 2, Name = "B", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active });
        var cycle = new AchCycle { Id = "c1", ClearingHouseId = 2, CycleName = cycleName, ProcessingDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 0, 0), CutoffTime = new TimeSpan(9, 0, 0) };
        var next = new AchCycle { Id = "c2", ClearingHouseId = 2, CycleName = "Ciclo 5", ProcessingDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(9, 1, 0), EndTime = new TimeSpan(10, 0, 0), CutoffTime = new TimeSpan(10, 0, 0) };
        context.AchCycles.AddRange(cycle, next);
        var batch = new AchBatch { Id = 77, AchCycleId = "c1", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescription = "PAGO", EffectiveEntryDate = DateTime.UtcNow.Date, OriginOrOdfi = "12345678", CompanyEntryDescriptionId = 1 };
        context.AchBatches.Add(batch);
        var tx = new AchTransaction { Id = 100, Amount = 100, Reference = "ref", TransactionExternalId = "op", Type = TransactionTypeEnum.Credit, AchCycleId = "c1", AchBatchId = 77, CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = 1, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000001", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, EffectiveEntryDate = DateTime.UtcNow.Date };
        context.AchTransactions.Add(tx);
        var execution = new CenitCycleExecution { Id = 500, AchCycleId = "c1", Status = "Running" };
        var netting = new CenitNettingExecution { Id = 600, CenitCycleExecutionId = 500, TotalCredit = 0, TotalDebit = 0 };
        context.CenitCycleExecutions.Add(execution);
        context.CenitNettingExecutions.Add(netting);
        context.CenitNetPositions.Add(new CenitNetPosition { CenitNettingExecutionId = 600, FinancialInstitutionId = 1, DebitAmount = 0, CreditAmount = 0, NetAmount = 0, AvailableLiquidity = availableLiquidity, SimulatedLiquidity = availableLiquidity, LiquiditySourceType = "Simulated" });
        await context.SaveChangesAsync();
        return (execution, "c2");
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
