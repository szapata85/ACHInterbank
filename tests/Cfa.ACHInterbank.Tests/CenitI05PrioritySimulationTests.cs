using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I05 / PT-06. Ten normative levels, stable ties, unsupported codes and
/// no side effects. These tests need the .NET SDK and SQLite provider to run.
/// A simulation is not evidence of a Banco de la República liquidation.
/// </summary>
public sealed class CenitI05PrioritySimulationTests
{
    [Theory]
    [InlineData("CCD", TransactionTypeEnum.Return, "21", 10)]
    [InlineData("PPD", TransactionTypeEnum.Return, "21", 9)]
    [InlineData("CCD", TransactionTypeEnum.Return, "26", 8)]
    [InlineData("PPD", TransactionTypeEnum.Return, "26", 7)]
    [InlineData("CTX", TransactionTypeEnum.Return, "26", 6)]
    [InlineData("CCD", TransactionTypeEnum.Debit, "27", 5)]
    [InlineData("PPD", TransactionTypeEnum.Debit, "27", 4)]
    [InlineData("CCD", TransactionTypeEnum.Credit, "22", 3)]
    [InlineData("PPD", TransactionTypeEnum.Credit, "22", 2)]
    [InlineData("CTX", TransactionTypeEnum.Credit, "22", 1)]
    public async Task NormativeTable_ResolvesAllTenLevels(string sec, TransactionTypeEnum type, string code, int expected)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"cenit-i05-table-{Guid.NewGuid():N}").Options;
        await using var context = new AchDbContext(options);
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 901, Term = "PRIORITY", Description = "I05", IsActive = true, StandardEntryClassCode = sec
        });
        await context.SaveChangesAsync();
        var policy = new TransactionPriorityPolicy(new AchRegulatoryCatalogService(context), context);
        var priority = await policy.ResolvePriorityAsync(new AchTransaction
        {
            Type = type, TransactionCode = code, CompanyEntryDescriptionId = 901
        }, CancellationToken.None);
        Assert.Equal(expected, priority);
    }

    [Theory]
    [InlineData("XYZ", TransactionTypeEnum.Credit, "22")]
    [InlineData("CTX", TransactionTypeEnum.Debit, "27")]
    [InlineData("PPD", TransactionTypeEnum.Return, "22")]
    [InlineData("PPD", TransactionTypeEnum.Return, "23")]
    [InlineData("CCD", TransactionTypeEnum.Return, "27")]
    [InlineData("CCD", TransactionTypeEnum.Return, "28")]
    [InlineData("PPD", TransactionTypeEnum.Credit, "27")]
    [InlineData("CCD", TransactionTypeEnum.Debit, "22")]
    [InlineData("PPD", TransactionTypeEnum.Return, "")]
    [InlineData("PPD", TransactionTypeEnum.Return, "55")]
    [InlineData("PPD", TransactionTypeEnum.Return, "57")]
    [InlineData("CTX", TransactionTypeEnum.Return, "21")]
    public async Task InvalidSecOrCode_NeverReceivesPriority(string sec, TransactionTypeEnum type, string code)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"cenit-i05-invalid-{Guid.NewGuid():N}").Options;
        await using var context = new AchDbContext(options);
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 901, Term = "PRIORITY", Description = "I05", IsActive = true, StandardEntryClassCode = sec
        });
        await context.SaveChangesAsync();
        var policy = new TransactionPriorityPolicy(new AchRegulatoryCatalogService(context), context);
        Assert.Equal(0, await policy.ResolvePriorityAsync(new AchTransaction
        {
            Type = type, TransactionCode = code, CompanyEntryDescriptionId = 901
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Simulation_OrdersDescending_ThenAmount_ThenId_AcrossInputPermutations()
    {
        var first = await RunFixtureAsync(reverseInput: false);
        var second = await RunFixtureAsync(reverseInput: true);
        var expectedIds = new[] { 110, 112, 101, 102, 103, 104, 105, 106, 100, 107, 108, 109, 111 };
        var expectedPriorities = new[] { 10, 10, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        Assert.Equal(expectedIds, first.Select(x => x.AchTransactionId));
        Assert.Equal(expectedPriorities, first.Select(x => x.Priority));
        Assert.Equal(first.Select(x => (x.AchTransactionId, x.Priority, x.DecisionType)),
            second.Select(x => (x.AchTransactionId, x.Priority, x.DecisionType)));
        Assert.Equal("SimulatedCandidate", first[0].DecisionType);
        Assert.Equal("SimulatedCandidate", first[1].DecisionType);
        Assert.Equal("SimulatedInsufficientLiquidity", first[2].DecisionType);
        Assert.Equal("SimulatedUnclassified", first[^1].DecisionType);
        Assert.Contains("NoLiquidityConsumption", first[^1].DecisionReason);
        Assert.All(first, x => Assert.Null(x.ToCycleId));
        Assert.All(first, x => Assert.Equal("Simulated", x.LiquidityModelUsed));
    }

    [Fact]
    public async Task Simulation_RefusesForeignClearingHouse_BeforeWritingDecisions()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = Context(connection);
        var execution = await SeedAsync(context, reverseInput: false);
        (await context.ClearingHouses.SingleAsync()).Code = "ACH";
        await context.SaveChangesAsync();
        var service = new LiquidityOptimizationService(context,
            new TransactionPriorityPolicy(new AchRegulatoryCatalogService(context), context));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.OptimizeCycleAsync(execution, CancellationToken.None));
        Assert.False(await context.LiquidityOptimizationDecisions.AnyAsync());
    }

    [Fact]
    public async Task Simulation_RefusesForeignClearingHouse_EvenWithPreviousSnapshot()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = Context(connection);
        var execution = await SeedAsync(context, reverseInput: false);
        var service = new LiquidityOptimizationService(context,
            new TransactionPriorityPolicy(new AchRegulatoryCatalogService(context), context));
        var previous = await service.OptimizeCycleAsync(execution, CancellationToken.None);
        Assert.Equal(13, previous.Count);
        (await context.ClearingHouses.SingleAsync()).Code = "ACH";
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.OptimizeCycleAsync(execution, CancellationToken.None));
        Assert.Equal(13, await context.LiquidityOptimizationDecisions.CountAsync());
    }

    private static async Task<List<LiquidityOptimizationDecision>> RunFixtureAsync(bool reverseInput)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = Context(connection);
        var execution = await SeedAsync(context, reverseInput);
        var service = new LiquidityOptimizationService(context,
            new TransactionPriorityPolicy(new AchRegulatoryCatalogService(context), context));
        var first = (await service.OptimizeCycleAsync(execution, CancellationToken.None)).ToList();
        var second = await service.OptimizeCycleAsync(execution, CancellationToken.None);
        Assert.Equal(first.Select(x => x.AchTransactionId), second.Select(x => x.AchTransactionId));
        Assert.Equal(first.Count, await context.LiquidityOptimizationDecisions.CountAsync());
        Assert.Equal(13, await context.AchTransactions.CountAsync());
        Assert.False(await context.CenitCycleQueues.AnyAsync());
        Assert.False(await context.AchTransactionStateEvents.AnyAsync());
        Assert.All(await context.AchTransactions.ToListAsync(),
            tx => Assert.Equal(AchTransferStateEnum.Pending, tx.State));
        return first;
    }

    private static AchDbContext Context(SqliteConnection connection)
    {
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<CenitCycleExecution> SeedAsync(AchDbContext context, bool reverseInput)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011111111", ClearingHouseId = 1 });
        var a = new FinancialInstitution { Id = 1, Name = "A", RoutingNumber = "1234", TransitCode = "5678", Status = FinancialInstitutionStatus.Active };
        var b = new FinancialInstitution { Id = 2, Name = "B", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active };
        a.CalculateCheckDigit();
        b.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(a, b);
        context.CompanyEntryDescriptionCatalogs.AddRange(
            new CompanyEntryDescriptionCatalog { Id = 901, Term = "PPD", Description = "PPD", IsActive = true, StandardEntryClassCode = "PPD" },
            new CompanyEntryDescriptionCatalog { Id = 902, Term = "CCD", Description = "CCD", IsActive = true, StandardEntryClassCode = "CCD" },
            new CompanyEntryDescriptionCatalog { Id = 903, Term = "CTX", Description = "CTX", IsActive = true, StandardEntryClassCode = "CTX" },
            new CompanyEntryDescriptionCatalog { Id = 904, Term = "OTHER", Description = "Unknown SEC", IsActive = true, StandardEntryClassCode = "XXX" });
        context.AchCycles.Add(new AchCycle { Id = "i05", ClearingHouseId = 2, CycleName = "Ciclo 3", ProcessingDate = new DateTime(2026, 11, 13) });
        context.AchBatches.Add(new AchBatch { Id = 77, AchCycleId = "i05", CompanyName = "T", CompanyIdentification = "N", CompanyEntryDescription = "PPD", CompanyEntryDescriptionId = 901, EffectiveEntryDate = new DateTime(2026, 11, 13), OriginOrOdfi = "12345678" });
        var execution = new CenitCycleExecution { Id = 500, AchCycleId = "i05", Status = "Running" };
        context.CenitCycleExecutions.Add(execution);
        context.CenitNettingExecutions.Add(new CenitNettingExecution { Id = 600, CenitCycleExecutionId = 500 });
        context.CenitNetPositions.Add(new CenitNetPosition { CenitNettingExecutionId = 600, FinancialInstitutionId = 1, SimulatedLiquidity = 45m, LiquiditySourceType = "Simulated" });

        var specs = new (int id, int sec, TransactionTypeEnum type, string code, decimal amount)[]
        {
            (100, 901, TransactionTypeEnum.Debit, "27", 10m), // level 4
            (101, 902, TransactionTypeEnum.Return, "21", 10m), // 10
            (102, 901, TransactionTypeEnum.Return, "21", 10m), // 9
            (103, 902, TransactionTypeEnum.Return, "26", 10m), // 8
            (104, 901, TransactionTypeEnum.Return, "26", 10m), // 7
            (105, 903, TransactionTypeEnum.Return, "26", 10m), // 6
            (106, 902, TransactionTypeEnum.Debit, "27", 10m), // 5
            (107, 902, TransactionTypeEnum.Credit, "22", 10m), // 3
            (108, 901, TransactionTypeEnum.Credit, "22", 10m), // 2
            (109, 903, TransactionTypeEnum.Credit, "22", 10m), // 1
            (110, 902, TransactionTypeEnum.Return, "21", 20m), // amount tie
            (111, 904, TransactionTypeEnum.Credit, "22", 1m), // 0 = unclassified
            (112, 902, TransactionTypeEnum.Return, "21", 20m) // exact tie: ID asc
        };
        if (reverseInput) Array.Reverse(specs);
        foreach (var (id, sec, type, code, amount) in specs)
            context.AchTransactions.Add(new AchTransaction
            {
                Id = id, AchCycleId = "i05", AchBatchId = 77,
                CompanyEntryDescriptionId = sec, CompanyName = "T", CompanyIdentification = "N",
                Reference = $"I05-{id}", TransactionExternalId = $"I05-OP-{id}",
                TraceNumber = $"12345678{id:0000000}", OriginatingDFI = "12345678", ReceivingDFI = "87654321",
                SourceAccountNumber = "1", DestinationAccountNumber = "2", SourceInstitutionId = 1,
                DestinationInstitutionId = 2, Amount = amount, Type = type,
                TransactionCode = code, EffectiveEntryDate = new DateTime(2026, 11, 13),
                State = AchTransferStateEnum.Pending
            });
        await context.SaveChangesAsync();
        return execution;
    }
}
