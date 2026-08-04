using System.Reflection;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class CenitCycleCalendarCharacterizationTests
{
    [Fact]
    public async Task CenitCycleSeeder_ShouldCreateFiveCenitCycles_CurrentBehavior()
    {
        await using var context = await CreateSqliteContextAsync();
        SeedClearingHouse(context, 2, "CENIT");
        await context.SaveChangesAsync();

        var sut = new AchCycleSeeder(context);
        await sut.SeedCyclesIfNotExistsAsync(2, 2026);

        var cycles = await context.ClearingHouseCycleConfigs
            .Where(x => x.ClearingHouseId == 2)
            .OrderBy(x => x.CycleName)
            .ToListAsync();

        Assert.Equal(5, cycles.Count);
        Assert.Equal(new[] { "Ciclo 1", "Ciclo 2", "Ciclo 3", "Ciclo 4", "Ciclo 5" }, cycles.Select(x => x.CycleName));
        Assert.All(cycles, x => Assert.True(x.IsActive));
        Assert.All(cycles, x => Assert.Equal(new DateTime(2026, 1, 1), x.EffectiveFrom.Date));
        Assert.All(cycles, x => Assert.Null(x.EffectiveTo));
    }

    [Fact]
    public async Task CenitCycleSeeder_ShouldUseDsp152OperationalWindows()
    {
        await using var context = await CreateSqliteContextAsync();
        SeedClearingHouse(context, 2, "CENIT");
        await context.SaveChangesAsync();

        var sut = new AchCycleSeeder(context);
        await sut.SeedCyclesIfNotExistsAsync(2, 2026);

        var byCycle = await context.ClearingHouseCycleConfigs
            .Where(x => x.ClearingHouseId == 2)
            .ToDictionaryAsync(x => x.CycleName, x => (x.StartTime, x.EndTime));

        Assert.Equal((new TimeSpan(7, 30, 0), new TimeSpan(10, 30, 0)), byCycle["Ciclo 1"]);
        Assert.Equal((new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0)), byCycle["Ciclo 2"]);
        Assert.Equal((new TimeSpan(13, 30, 0), new TimeSpan(15, 0, 0)), byCycle["Ciclo 3"]);
        Assert.Equal((new TimeSpan(15, 30, 0), new TimeSpan(17, 15, 0)), byCycle["Ciclo 4"]);
        Assert.Equal((new TimeSpan(17, 45, 0), new TimeSpan(18, 45, 0)), byCycle["Ciclo 5"]);
    }

    [Fact]
    public async Task CenitCycleConfig_ShouldHaveCutoffWithinOrConsistentWithWindow_CurrentBehavior()
    {
        await using var context = await CreateSqliteContextAsync();
        SeedClearingHouse(context, 2, "CENIT");
        await context.SaveChangesAsync();

        var sut = new AchCycleSeeder(context);
        await sut.SeedCyclesIfNotExistsAsync(2, 2026);

        var cycles = await context.ClearingHouseCycleConfigs.Where(x => x.ClearingHouseId == 2).ToListAsync();
        Assert.All(cycles, c => Assert.NotEqual(default, c.CutoffTime));

        // Caracterización explícita de comportamiento actual: en ventanas que cruzan medianoche,
        // la consistencia se evalúa con regla circular (OR), no lineal simple.
        foreach (var c in cycles)
        {
            var withinWindow = c.StartTime < c.EndTime
                ? c.CutoffTime >= c.StartTime && c.CutoffTime <= c.EndTime
                : c.CutoffTime >= c.StartTime || c.CutoffTime <= c.EndTime;
            Assert.True(withinWindow, $"Cutoff fuera de ventana para {c.CycleName}");
        }
    }

    [Fact]
    public async Task CenitOperatingCalendarPolicy_ShouldRequireFiveConsecutiveCycles_CurrentBehavior()
    {
        await using var context = await CreateSqliteContextAsync();
        SeedClearingHouse(context, 2, "CENIT");
        context.ClearingHouseCycleConfigs.AddRange(BuildFiveConsecutiveActiveConfigs(2, new DateTime(2026, 4, 17)));
        await context.SaveChangesAsync();

        var sut = new CenitOperatingCalendarPolicy(context);
        await sut.ValidateCycleConsistencyAsync(2, new DateTime(2026, 4, 17), CancellationToken.None);

        context.ClearingHouseCycleConfigs.Remove(await context.ClearingHouseCycleConfigs.FirstAsync(x => x.CycleName == "Ciclo 5"));
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ValidateCycleConsistencyAsync(2, new DateTime(2026, 4, 17), CancellationToken.None));
    }

    [Fact]
    public async Task CenitOperatingCalendarPolicy_ShouldDetectInactiveOrMissingCycle_CurrentBehavior()
    {
        await using var context = await CreateSqliteContextAsync();
        SeedClearingHouse(context, 2, "CENIT");
        var items = BuildFiveConsecutiveActiveConfigs(2, new DateTime(2026, 4, 17));
        items[3].IsActive = false;
        context.ClearingHouseCycleConfigs.AddRange(items);
        await context.SaveChangesAsync();

        var sut = new CenitOperatingCalendarPolicy(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ValidateCycleConsistencyAsync(2, new DateTime(2026, 4, 17), CancellationToken.None));
    }

    [Fact]
    public async Task BankHolidayService_ShouldIdentifyBusinessAndNonBusinessDays_CurrentBehavior()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AchDbContext>(o => o.UseInMemoryDatabase(nameof(BankHolidayService_ShouldIdentifyBusinessAndNonBusinessDays_CurrentBehavior)));
        services.AddScoped<IHolidayStrategyFactory, FakeHolidayStrategyFactory>();
        services.AddSingleton<IBankHoliday, BankHoliday>();
        var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            db.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
            db.ClearingHouses.Add(new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "011", ClearingHouseId = 1 });
            await db.SaveChangesAsync();
        }

        var holiday = provider.GetRequiredService<IBankHoliday>();
        await holiday.SeedHolidaysIfNotExistsAsync(2026);

        Assert.False(holiday.IsBusinessDay(new DateOnly(2026, 1, 1), "CO"));
        Assert.True(holiday.IsBusinessDay(new DateOnly(2026, 1, 2), "CO"));
    }

    [Fact]
    public async Task AchCycleScheduler_ShouldSkipNonBusinessDay_CurrentBehavior()
    {
        await using var context = await CreateSqliteContextAsync();
        SeedClearingHouse(context, 2, "CENIT");
        context.ClearingHouseCycleConfigs.AddRange(BuildFiveConsecutiveActiveConfigs(2, new DateTime(2026, 1, 1)));
        context.BankHolidays.Add(new BankHolidayModel { Date = new DateOnly(2026, 1, 1), Description = "Holiday", CountryCode = "CO" });
        await context.SaveChangesAsync();

        var holidayMock = new Mock<IBankHoliday>();
        var txService = new Mock<IAchTransactionService>();
        txService.Setup(x => x.GetNextBusinessDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DateTime(2026, 1, 1));
        var sp = new ServiceCollection().AddScoped(_ => txService.Object).BuildServiceProvider();

        var sut = new AchCycleScheduler(context, holidayMock.Object, sp, new CenitOperatingCalendarPolicy(context));
        await sut.ScheduleCyclesForClearingHouseAsync(2, new DateTime(2026, 1, 1));

        Assert.False(await context.AchCycles.AnyAsync(x => x.ClearingHouseId == 2 && x.ProcessingDate.Date == new DateTime(2026, 1, 1).Date));
    }

    [Fact]
    public async Task AchCycleScheduler_ShouldScheduleOnBusinessDay_CurrentBehavior()
    {
        await using var context = await CreateSqliteContextAsync();
        SeedClearingHouse(context, 2, "CENIT");
        context.ClearingHouseCycleConfigs.AddRange(BuildFiveConsecutiveActiveConfigs(2, new DateTime(2026, 1, 2)));
        await context.SaveChangesAsync();

        var holidayMock = new Mock<IBankHoliday>();
        var txService = new Mock<IAchTransactionService>();
        txService.Setup(x => x.GetNextBusinessDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DateTime(2026, 1, 2));
        var sp = new ServiceCollection().AddScoped(_ => txService.Object).BuildServiceProvider();

        var sut = new AchCycleScheduler(context, holidayMock.Object, sp, new CenitOperatingCalendarPolicy(context));
        await sut.ScheduleCyclesForClearingHouseAsync(2, new DateTime(2026, 1, 2));

        var scheduled = await context.AchCycles.Where(x => x.ClearingHouseId == 2 && x.ProcessingDate.Date == new DateTime(2026, 1, 2).Date).ToListAsync();
        Assert.Equal(5, scheduled.Count);
    }

    [Fact]
    public void CycleWindow_ShouldUseInjectedTimeProviderInsteadOfHostLocalTime()
    {
        var file = GetRepoFile("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchCycleScheduler.cs");
        var content = File.ReadAllText(file);
        Assert.Contains("TimeProvider", content);
        Assert.DoesNotContain("DateTime.Now", content);
    }

    [Fact]
    public void TaskDefinition_ShouldDefaultOrUseAmericaBogota_WhenConfigured_CurrentBehavior()
    {
        var task = new TaskDefinition();
        Assert.Equal("America/Bogota", task.TimeZoneId);
    }

    [Fact]
    public void CenitNetting_ShouldPersistExecutionAndPositions_CurrentBehavior()
    {
        using var context = CreateInMemoryContext(nameof(CenitNetting_ShouldPersistExecutionAndPositions_CurrentBehavior));
        Assert.NotNull(context.CenitNettingExecutions);
        Assert.NotNull(context.CenitNetPositions);
        Assert.NotNull(context.CenitNettingDetails);
    }

    [Fact]
    public void CenitNetting_ShouldBeCycleScoped_CurrentBehavior()
    {
        var detailType = typeof(CenitNettingDetail);
        Assert.NotNull(detailType.GetProperty(nameof(CenitNettingDetail.ClearingHouseId)));
        Assert.NotNull(detailType.GetProperty(nameof(CenitNettingDetail.ClearingHouseCode)));
        Assert.NotNull(detailType.GetProperty(nameof(CenitNettingDetail.ValueDate)));
    }

    [Fact]
    public void LiquidityOptimization_ShouldUseInternalDxxLiq_ForInsufficientLiquidity_CurrentBehavior()
    {
        var file = GetRepoFile("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/LiquidityOptimizationService.cs");
        var content = File.ReadAllText(file);
        Assert.Contains("DXX-LIQ", content);
    }

    [Fact]
    public void LiquidityOptimization_ShouldProduceProcessedDeferredRejectedDecisions_CurrentBehavior()
    {
        var file = GetRepoFile("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/LiquidityOptimizationService.cs");
        var content = File.ReadAllText(file);
        Assert.Contains("decision = \"Processed\"", content);
        Assert.Contains("decision = \"Deferred\"", content);
        Assert.Contains("decision = \"Rejected\"", content);
    }

    [Fact]
    public void CudIntegration_RuntimeClientTypes_ShouldNotExist_CurrentBehavior()
    {
        var names = new[] { "ICudClient", "CudSettlementService", "CudApiClient" };
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && (a.GetName().Name?.StartsWith("Cfa.ACHInterbank", StringComparison.Ordinal) ?? false))
            .SelectMany(a => a.GetTypes())
            .Select(t => t.Name)
            .ToHashSet();
        Assert.DoesNotContain(names[0], allTypes);
        Assert.DoesNotContain(names[1], allTypes);
        Assert.DoesNotContain(names[2], allTypes);
    }

    [Fact]
    public void Returns_ShouldKeepCycleAndClearingHouseRelationship_CurrentBehavior()
    {
        var transactionType = typeof(AchTransaction);
        var cycleType = typeof(AchCycle);

        Assert.NotNull(transactionType.GetProperty(nameof(AchTransaction.AchCycleId)));
        Assert.NotNull(cycleType.GetProperty(nameof(AchCycle.ClearingHouseId)));
        Assert.NotNull(cycleType.GetProperty(nameof(AchCycle.ClearingHouse)));
    }

    private static async Task<AchDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static AchDbContext CreateInMemoryContext(string name)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AchDbContext(options);
    }

    private static List<ClearingHouseCycleConfig> BuildFiveConsecutiveActiveConfigs(int clearingHouseId, DateTime effectiveFrom) =>
    [
        new() { ClearingHouseId = clearingHouseId, CycleName = "Ciclo 1", IsActive = true, EffectiveFrom = effectiveFrom, StartTime = new TimeSpan(19,1,0), EndTime = new TimeSpan(8,30,0), CutoffTime = new TimeSpan(8,30,0) },
        new() { ClearingHouseId = clearingHouseId, CycleName = "Ciclo 2", IsActive = true, EffectiveFrom = effectiveFrom, StartTime = new TimeSpan(8,31,0), EndTime = new TimeSpan(11,0,0), CutoffTime = new TimeSpan(11,0,0) },
        new() { ClearingHouseId = clearingHouseId, CycleName = "Ciclo 3", IsActive = true, EffectiveFrom = effectiveFrom, StartTime = new TimeSpan(11,1,0), EndTime = new TimeSpan(14,0,0), CutoffTime = new TimeSpan(14,0,0) },
        new() { ClearingHouseId = clearingHouseId, CycleName = "Ciclo 4", IsActive = true, EffectiveFrom = effectiveFrom, StartTime = new TimeSpan(14,1,0), EndTime = new TimeSpan(16,0,0), CutoffTime = new TimeSpan(16,0,0) },
        new() { ClearingHouseId = clearingHouseId, CycleName = "Ciclo 5", IsActive = true, EffectiveFrom = effectiveFrom, StartTime = new TimeSpan(16,1,0), EndTime = new TimeSpan(18,0,0), CutoffTime = new TimeSpan(18,0,0) }
    ];

    private static void SeedClearingHouse(AchDbContext context, int clearingHouseId, string code)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse { Id = clearingHouseId, Code = code, Name = code, OriginCode = "011111111", ClearingHouseId = 1 });
    }

    private static void SeedFinancialInstitutions(AchDbContext context)
    {
        var fi1 = new FinancialInstitution { Id = 1, Name = "A", RoutingNumber = "1234", TransitCode = "5678", Status = FinancialInstitutionStatus.Active };
        fi1.CalculateCheckDigit();
        var fi2 = new FinancialInstitution { Id = 2, Name = "B", RoutingNumber = "8765", TransitCode = "4321", Status = FinancialInstitutionStatus.Active };
        fi2.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(fi1, fi2);
    }

    private static string GetRepoFile(string relativePath)
    {
        var probe = new DirectoryInfo(AppContext.BaseDirectory);
        while (probe is not null && !Directory.Exists(Path.Combine(probe.FullName, "src")))
        {
            probe = probe.Parent;
        }

        if (probe is null)
        {
            throw new InvalidOperationException("Repository root not found.");
        }

        return Path.Combine(probe.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed class FakeHolidayStrategyFactory : IHolidayStrategyFactory
    {
        public IHolidayStrategy GetStrategyForClearingHouse(int clearingHouseId) => new FakeHolidayStrategy();
    }

    private sealed class FakeHolidayStrategy : IHolidayStrategy
    {
        public List<BankHolidayModel> GenerateHolidays(int year)
            =>
            [
                new() { Date = new DateOnly(year, 1, 1), Description = "Test holiday", CountryCode = "CO" }
            ];
    }
}
