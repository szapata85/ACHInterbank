using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class CenitOperationalGovernanceTests
{
    private const int TestCompanyEntryDescriptionId = 9001;
    [Fact]
    public async Task CenitCalendarPolicy_Throws_WhenCycleCountIsNotFive()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
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
        var operationalDate = new DateTime(2026, 8, 26);
        var fixedInstant = new DateTimeOffset(2026, 8, 26, 14, 0, 0, TimeSpan.Zero);
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var fiSource = new FinancialInstitution { Id = 1, Name = "Banco Origen", IsDefaultSource = true, RoutingNumber = "1234", TransitCode = "5678", Status = FinancialInstitutionStatus.Active };
        fiSource.CalculateCheckDigit();
        var fiDestination = new FinancialInstitution { Id = 2, Name = "Banco Destino", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active };
        fiDestination.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(fiSource, fiDestination);
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian", TimeZoneId = "America/Bogota" });
        context.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        var cycleConfig = new ClearingHouseCycleConfig
        {
            ClearingHouseId = 2,
            PolicyVersion = "CENIT-TEST-V1",
            CycleName = "Ciclo 2",
            StartTime = new TimeSpan(11, 0, 0),
            CutoffTime = new TimeSpan(12, 0, 0),
            EndTime = new TimeSpan(13, 0, 0),
            OutputReleaseTime = new TimeSpan(14, 0, 0),
            AllowsMonetaryCredit = true,
            EffectiveFrom = operationalDate,
            EffectiveTo = operationalDate,
            IsActive = true
        };
        var cycle = new AchCycle
        {
            Id = "cycle-1",
            ClearingHouseId = 2,
            CycleName = "Ciclo 2",
            ProcessingDate = operationalDate,
            StartTime = cycleConfig.StartTime,
            EndTime = cycleConfig.EndTime,
            CutoffTime = cycleConfig.CutoffTime,
            OutputReleaseTime = cycleConfig.OutputReleaseTime,
            ClearingHouseCycleConfig = cycleConfig
        };
        context.ClearingHouseCycleConfigs.Add(cycleConfig);
        context.AchCycles.Add(cycle);
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog { Id = TestCompanyEntryDescriptionId, Term = "PAGO", Description = "Pago", IsActive = true, StandardEntryClassCode = "PPD" });
        await context.SaveChangesAsync();

        var batchRepo = new Mock<IAchBatchRepository>();
        batchRepo.Setup(x => x.FindForTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchBatch { Id = 9, AchCycleId = "cycle-1", CompanyName = "Comp", CompanyIdentification = "NIT", CompanyEntryDescription = "PAGO", CompanyEntryDescriptionId = TestCompanyEntryDescriptionId, EffectiveEntryDate = operationalDate, OriginOrOdfi = "12345678" });
        batchRepo.Setup(x => x.GetUpcomingCyclesAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchCycle>());

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cycle-1");

        var sut = new BatchResolver(context, batchRepo.Object, routing.Object, new FixedTimeProvider(fixedInstant, TimeZoneInfo.Utc));
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
            CompanyEntryDescriptionId = TestCompanyEntryDescriptionId
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
        var catalog = new AchRegulatoryCatalogService(context);
        var sut = new LiquidityOptimizationService(context, new TransactionPriorityPolicy(catalog));

        var decisions = await sut.OptimizeCycleAsync(setup.Execution, CancellationToken.None);

        Assert.Contains(decisions, x => x.DecisionType == "Deferred");
        var tx = await context.AchTransactions.FirstAsync();
        Assert.Equal(setup.NextCycleId, tx.AchCycleId);
        Assert.NotEqual(77, tx.AchBatchId);
        Assert.Equal(setup.NextCycleId, (await context.AchBatches.SingleAsync(x => x.Id == tx.AchBatchId)).AchCycleId);
        var dispatch = await context.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == tx.Id);
        Assert.Equal(setup.NextCycleId, dispatch.AchCycleId);
        Assert.Equal(tx.AchBatchId, dispatch.AchBatchId);
        Assert.True(await context.CenitCycleQueues.AnyAsync(x => x.QueueReason == "LiquidityDeferredByRule123"));

        var repeated = await sut.OptimizeCycleAsync(setup.Execution, CancellationToken.None);
        Assert.Equal(decisions.Count, repeated.Count);
        Assert.Single(await context.LiquidityOptimizationDecisions.Where(x => x.AchTransactionId == tx.Id).ToListAsync());
    }

    [Fact]
    public async Task LiquidityOptimization_WhenFinalWriteFails_RollsBackCycleBatchQueueAndDispatch()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var setup = await SeedCycleExecutionScenarioAsync(context, "Ciclo 2", availableLiquidity: 0m);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER phase1a_fail_liquidity BEFORE INSERT ON LiquidityOptimizationDecisions BEGIN SELECT RAISE(ABORT, 'phase1a rollback test'); END;");
        var sut = new LiquidityOptimizationService(context, new TransactionPriorityPolicy(new AchRegulatoryCatalogService(context)));

        await Assert.ThrowsAsync<DbUpdateException>(() => sut.OptimizeCycleAsync(setup.Execution, CancellationToken.None));

        await using var verification = CreateContext(connection);
        var transaction = await verification.AchTransactions.AsNoTracking().SingleAsync(x => x.Id == 100);
        var dispatch = await verification.ContrapartidaDispatchItems.AsNoTracking().SingleAsync(x => x.AchTransactionId == 100);
        Assert.Equal("c1", transaction.AchCycleId);
        Assert.Equal(77, transaction.AchBatchId);
        Assert.Equal("c1", dispatch.AchCycleId);
        Assert.Equal(77, dispatch.AchBatchId);
        Assert.False(await verification.AchBatches.AnyAsync(x => x.AchCycleId == setup.NextCycleId));
        Assert.Empty(await verification.CenitCycleQueues.ToListAsync());
        Assert.Empty(await verification.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task LiquidityOptimization_RejectsInCycle4_UpdatesState()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var setup = await SeedCycleExecutionScenarioAsync(context, "Ciclo 4", availableLiquidity: 0m);
        var catalog = new AchRegulatoryCatalogService(context);
        var sut = new LiquidityOptimizationService(context, new TransactionPriorityPolicy(catalog));

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

        SeedReturnOfReturnPrerequisites(context);
        var source = new AchTransaction { Id = 10, Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.ReturnedByOperator, SlaDeadlineAtUtc = DateTime.UtcNow.AddMinutes(-1), AchCycleId = "c1", Reference = "r", TransactionExternalId = "op-r", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = TestCompanyEntryDescriptionId, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000010", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, AchBatchId = 1, EffectiveEntryDate = DateTime.UtcNow.Date };
        var ror = new AchTransaction { Id = 11, Type = TransactionTypeEnum.Return, AchCycleId = "c1", Reference = "r2", TransactionExternalId = "op-r2", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = TestCompanyEntryDescriptionId, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000011", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, AchBatchId = 1, EffectiveEntryDate = DateTime.UtcNow.Date };
        context.AchTransactions.AddRange(source, ror);
        await context.SaveChangesAsync();

        var catalog = new AchRegulatoryCatalogService(context);
        var sut = new ReturnOfReturnOrchestrator(context, new AchReturnOfReturnEligibilityService(context, catalog));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(source, ror, "R01", CancellationToken.None));
    }

    [Fact]
    public async Task ReturnOfReturnOrchestrator_RejectsDuplicateFlow()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        SeedReturnOfReturnPrerequisites(context);
        var source = new AchTransaction { Id = 21, Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.ReturnedByOperator, SlaDeadlineAtUtc = DateTime.UtcNow.AddHours(2), AchCycleId = "c1", Reference = "r21", TransactionExternalId = "op21", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = TestCompanyEntryDescriptionId, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000021", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, AchBatchId = 1, EffectiveEntryDate = DateTime.UtcNow.Date };
        var ror = new AchTransaction { Id = 22, Type = TransactionTypeEnum.Return, AchCycleId = "c1", Reference = "r22", TransactionExternalId = "op22", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = TestCompanyEntryDescriptionId, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000022", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, AchBatchId = 1, EffectiveEntryDate = DateTime.UtcNow.Date };
        context.AchTransactions.AddRange(source, ror);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { SourceReturnTransactionId = 21, ReturnOfReturnTransactionId = 22, ReasonCode = "R01" });
        await context.SaveChangesAsync();

        var catalog = new AchRegulatoryCatalogService(context);
        var sut = new ReturnOfReturnOrchestrator(context, new AchReturnOfReturnEligibilityService(context, catalog));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(source, ror, "R01", CancellationToken.None));
    }


    [Fact]
    public async Task ReturnOfReturnOrchestrator_WhenUniqueFalse_RejectsDuplicateReturnOfReturnTransactionId()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        SeedReturnOfReturnPrerequisites(context);
        var source = BuildReturnTx(31, "r31", "op31");
        var ror = BuildReturnTx(32, "r32", "op32");
        context.AchTransactions.AddRange(source, ror);
        var otherSource = BuildReturnTx(33, "r33", "op33");
        context.AchTransactions.Add(otherSource);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { SourceReturnTransactionId = 33, ReturnOfReturnTransactionId = 32, ReasonCode = "R01" });
        await context.SaveChangesAsync();

        var eligibility = BuildEligibilityMock(isEligible: true, isUniquePerTransaction: false);
        var sut = new ReturnOfReturnOrchestrator(context, eligibility.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(source, ror, "R01", CancellationToken.None));
        Assert.Equal("La transacción de devolución de devolución ya fue registrada.", ex.Message);
    }

    [Fact]
    public async Task ReturnOfReturnOrchestrator_WhenUniqueTrue_RejectsDuplicateSourceReturnTransactionId()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        SeedReturnOfReturnPrerequisites(context);
        var source = BuildReturnTx(41, "r41", "op41");
        var ror = BuildReturnTx(42, "r42", "op42");
        context.AchTransactions.AddRange(source, ror);
        var otherRor = BuildReturnTx(43, "r43", "op43");
        context.AchTransactions.Add(otherRor);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { SourceReturnTransactionId = 41, ReturnOfReturnTransactionId = 43, ReasonCode = "R01" });
        await context.SaveChangesAsync();

        var eligibility = BuildEligibilityMock(isEligible: true, isUniquePerTransaction: true);
        var sut = new ReturnOfReturnOrchestrator(context, eligibility.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(source, ror, "R01", CancellationToken.None));
        Assert.Equal("Ya existe una devolución de devolución para la devolución origen.", ex.Message);
    }

    [Fact]
    public async Task ReturnOfReturnOrchestrator_RejectsDuplicateExactSourceAndReturnOfReturnCombination()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        SeedReturnOfReturnPrerequisites(context);
        var source = BuildReturnTx(51, "r51", "op51");
        var ror = BuildReturnTx(52, "r52", "op52");
        context.AchTransactions.AddRange(source, ror);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { SourceReturnTransactionId = 51, ReturnOfReturnTransactionId = 52, ReasonCode = "R01" });
        await context.SaveChangesAsync();

        var eligibility = BuildEligibilityMock(isEligible: true, isUniquePerTransaction: false);
        var sut = new ReturnOfReturnOrchestrator(context, eligibility.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAsync(source, ror, "R01", CancellationToken.None));
        Assert.Equal("La devolución de devolución ya está registrada para esta combinación origen/destino.", ex.Message);
    }

    private static async Task<(CenitCycleExecution Execution, string NextCycleId)> SeedCycleExecutionScenarioAsync(AchDbContext context, string cycleName, decimal availableLiquidity)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog { Id = TestCompanyEntryDescriptionId, Term = "PAGO", Description = "Pago", IsActive = true, StandardEntryClassCode = "PPD" });
        var fi1 = new FinancialInstitution { Id = 1, Name = "A", RoutingNumber = "1234", TransitCode = "5678", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active };
        fi1.CalculateCheckDigit();
        var fi2 = new FinancialInstitution { Id = 2, Name = "B", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active };
        fi2.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(fi1, fi2);
        var cycle = new AchCycle { Id = "c1", ClearingHouseId = 2, CycleName = cycleName, ProcessingDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 0, 0), CutoffTime = new TimeSpan(9, 0, 0) };
        var next = new AchCycle { Id = "c2", ClearingHouseId = 2, CycleName = "Ciclo 5", ProcessingDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(9, 1, 0), EndTime = new TimeSpan(10, 0, 0), CutoffTime = new TimeSpan(10, 0, 0) };
        context.AchCycles.AddRange(cycle, next);
        var batch = new AchBatch { Id = 77, AchCycleId = "c1", CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescription = "PAGO", EffectiveEntryDate = DateTime.UtcNow.Date, OriginOrOdfi = "12345678", CompanyEntryDescriptionId = TestCompanyEntryDescriptionId };
        context.AchBatches.Add(batch);
        var tx = new AchTransaction { Id = 100, Amount = 100, Reference = "ref", TransactionExternalId = "op", Type = TransactionTypeEnum.Debit, AchCycleId = "c1", AchBatchId = 77, CompanyName = "C", CompanyIdentification = "N", CompanyEntryDescriptionId = TestCompanyEntryDescriptionId, OriginatingDFI = "123456789", ReceivingDFI = "987654321", TraceNumber = "123456780000001", SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1, DestinationInstitutionId = 2, EffectiveEntryDate = DateTime.UtcNow.Date, Direction = AchTransactionDirection.Outgoing, Origin = AchTransactionOrigin.Cfa, MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ProcContrapartidas, ClassificationStatus = AchTransactionClassificationStatus.Determined, SourceInstitutionWasDefaultAtCreation = true, ClassifiedAtUtc = DateTime.UtcNow, ClassificationVersion = 1 };
        context.AchTransactions.Add(tx);
        context.ContrapartidaDispatchItems.Add(new ContrapartidaDispatchItem { AchTransactionId = 100, AchCycleId = "c1", AchBatchId = 77, ClearingHouseId = 2, State = ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport });
        var execution = new CenitCycleExecution { Id = 500, AchCycleId = "c1", Status = "Running" };
        var netting = new CenitNettingExecution { Id = 600, CenitCycleExecutionId = 500, TotalCredit = 0, TotalDebit = 0 };
        context.AchTransactionTypePolicies.AddRange(
            new AchTransactionTypePolicy { TransactionType = "Credit", PriorityOrder = 80, IsMonetary = true, IsActive = true },
            new AchTransactionTypePolicy { TransactionType = "Debit", PriorityOrder = 80, IsMonetary = true, IsActive = true },
            new AchTransactionTypePolicy { TransactionType = "Return", PriorityOrder = 100, IsMonetary = true, IsActive = true });
        context.CenitCycleExecutions.Add(execution);
        context.CenitNettingExecutions.Add(netting);
        context.CenitNetPositions.Add(new CenitNetPosition { CenitNettingExecutionId = 600, FinancialInstitutionId = 1, DebitAmount = 0, CreditAmount = 0, NetAmount = 0, AvailableLiquidity = availableLiquidity, SimulatedLiquidity = availableLiquidity, LiquiditySourceType = "Simulated" });
        await context.SaveChangesAsync();
        return (execution, "c2");
    }

    private static void SeedReturnOfReturnPrerequisites(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog { Id = TestCompanyEntryDescriptionId, Term = "PAGO", Description = "Pago", IsActive = true, StandardEntryClassCode = "PPD" });
        var fi1 = new FinancialInstitution { Id = 1, Name = "Banco A", RoutingNumber = "1234", TransitCode = "5678", Status = FinancialInstitutionStatus.Active };
        fi1.CalculateCheckDigit();
        var fi2 = new FinancialInstitution { Id = 2, Name = "Banco B", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active };
        fi2.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(fi1, fi2);
        context.AchCycles.Add(new AchCycle
        {
            Id = "c1",
            ClearingHouseId = 2,
            CycleName = "Ciclo 1",
            ProcessingDate = DateTime.UtcNow.Date,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(9, 0, 0),
            CutoffTime = new TimeSpan(9, 0, 0)
        });
        context.AchBatches.Add(new AchBatch
        {
            Id = 1,
            AchCycleId = "c1",
            CompanyName = "C",
            CompanyIdentification = "N",
            CompanyEntryDescription = "PAGO",
            EffectiveEntryDate = DateTime.UtcNow.Date,
            OriginOrOdfi = "12345678",
            CompanyEntryDescriptionId = TestCompanyEntryDescriptionId
        });
    }


    private static Mock<IAchReturnOfReturnEligibilityService> BuildEligibilityMock(bool isEligible, bool isUniquePerTransaction)
    {
        var mock = new Mock<IAchReturnOfReturnEligibilityService>();
        mock.Setup(x => x.EvaluateAsync(It.IsAny<AchReturnOfReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnOfReturnEligibilityResult(
                isEligible,
                2,
                0,
                "R01",
                "R02",
                isUniquePerTransaction,
                Array.Empty<AchReturnOfReturnEligibilityFailure>()));
        return mock;
    }

    private static AchTransaction BuildReturnTx(int id, string reference, string externalId)
        => new()
        {
            Id = id,
            Type = TransactionTypeEnum.Return,
            State = AchTransferStateEnum.ReturnedByOperator,
            SlaDeadlineAtUtc = DateTime.UtcNow.AddHours(2),
            AchCycleId = "c1",
            Reference = reference,
            TransactionExternalId = externalId,
            CompanyName = "C",
            CompanyIdentification = "N",
            CompanyEntryDescriptionId = TestCompanyEntryDescriptionId,
            OriginatingDFI = "123456789",
            ReceivingDFI = "987654321",
            TraceNumber = $"1234567800000{id:000}",
            SourceAccountNumber = "1",
            DestinationAccountNumber = "2",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchBatchId = 1,
            EffectiveEntryDate = DateTime.UtcNow.Date
        };

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
