using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ProjectedOperationalCycleTests
{
    [Theory]
    [InlineData(2026, 10, 17, 2026, 10, 19)] // sábado -> lunes
    [InlineData(2026, 10, 18, 2026, 10, 19)] // domingo -> lunes
    [InlineData(2026, 11, 16, 2026, 11, 17)] // festivo Emiliani -> martes
    [InlineData(2026, 12, 25, 2026, 12, 28)] // navidad, sábado y domingo -> lunes
    public async Task Scheduler_WhenInvokedOnNonBusinessDay_CreatesSevenCyclesOnlyOnNextBusinessDate(
        int year, int month, int day, int targetYear, int targetMonth, int targetDay)
    {
        await using var fixture = await Fixture.CreateAsync(new DateTimeOffset(year, month, day, 15, 0, 0, TimeSpan.Zero));
        await fixture.Scheduler.ScheduleCyclesForClearingHouseAsync(1);
        var target = new DateTime(targetYear, targetMonth, targetDay);
        var generated = await fixture.Context.AchCycles.ToListAsync();

        Assert.Equal(7, generated.Count);
        Assert.All(generated, cycle => Assert.Equal(target, cycle.ProcessingDate));
        Assert.Equal(7, generated.Select(cycle => cycle.CycleName).Distinct().Count());
        Assert.All(generated, cycle => Assert.NotNull(cycle.ClearingHouseCycleConfigId));
        Assert.Equal(new TimeSpan(11, 1, 0), generated.Single(cycle => cycle.CycleName == "Ciclo 4").StartTime);

        await fixture.Scheduler.ScheduleCyclesForClearingHouseAsync(1);
        Assert.Equal(7, await fixture.Context.AchCycles.CountAsync());
    }

    [Fact]
    public async Task Scheduler_WhenOnlyOneCycleExists_RepairsMissingSixWithoutReplacingExisting()
    {
        await using var fixture = await Fixture.CreateAsync(new DateTimeOffset(2026, 10, 17, 16, 0, 0, TimeSpan.Zero));
        var target = new DateTime(2026, 10, 19);
        await fixture.Scheduler.ScheduleCyclesForClearingHouseAsync(1, target);
        var existing = await fixture.Context.AchCycles.SingleAsync(cycle => cycle.CycleName == "Ciclo 1");
        var originalId = existing.Id;
        fixture.Context.AchCycles.RemoveRange(await fixture.Context.AchCycles
            .Where(cycle => cycle.CycleName != "Ciclo 1").ToListAsync());
        await fixture.Context.SaveChangesAsync();

        await fixture.Scheduler.ScheduleCyclesForClearingHouseAsync(1);
        Assert.Equal(7, await fixture.Context.AchCycles.CountAsync());
        Assert.Equal(originalId, (await fixture.Context.AchCycles.SingleAsync(cycle => cycle.CycleName == "Ciclo 1")).Id);
    }

    [Fact]
    public async Task Scheduler_ReconcilesUnreferencedLegacyFiveCycleSnapshotToCurrentSevenCyclePolicy()
    {
        await using var fixture = await Fixture.CreateAsync(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var target = new DateTime(2026, 10, 19);
        await fixture.Scheduler.ScheduleCyclesForClearingHouseAsync(1, target);
        var legacy = await fixture.Context.AchCycles
            .Where(cycle => cycle.CycleName == "Ciclo 6" || cycle.CycleName == "Ciclo 7")
            .ToListAsync();
        fixture.Context.AchCycles.RemoveRange(legacy);
        var oldFirst = await fixture.Context.AchCycles.SingleAsync(cycle => cycle.CycleName == "Ciclo 1");
        var stableId = oldFirst.Id;
        oldFirst.StartTime = new TimeSpan(18, 30, 0);
        oldFirst.ClearingHouseCycleConfigId = null;
        await fixture.Context.SaveChangesAsync();

        await fixture.Scheduler.ScheduleCyclesForClearingHouseAsync(1, target);
        var current = await fixture.Context.AchCycles.ToListAsync();
        Assert.Equal(7, current.Count);
        var first = Assert.Single(current.Where(cycle => cycle.CycleName == "Ciclo 1"));
        Assert.Equal(stableId, first.Id);
        Assert.Equal(new TimeSpan(19, 1, 0), first.StartTime);
        Assert.NotNull(first.ClearingHouseCycleConfigId);
    }

    [Fact]
    public async Task Scheduler_DoesNotRewriteLegacyCycleWithAlreadyLinkedNachaFile()
    {
        await using var fixture = await Fixture.CreateAsync(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var target = new DateTime(2026, 10, 19);
        await fixture.Scheduler.ScheduleCyclesForClearingHouseAsync(1, target);
        var first = await fixture.Context.AchCycles.SingleAsync(cycle => cycle.CycleName == "Ciclo 1");
        first.StartTime = new TimeSpan(18, 30, 0);
        fixture.Context.NachaHeaders.Add(new NachaHeader
        {
            NachaID = "fixture-cycle-used",
            AchCycleId = first.Id
        });
        await fixture.Context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Scheduler.ScheduleCyclesForClearingHouseAsync(1, target));
        Assert.Contains("conciliación manual", exception.Message);
    }

    [Theory]
    [InlineData(2026, 10, 17, 2026, 10, 19)]
    [InlineData(2026, 11, 16, 2026, 11, 17)]
    public async Task NewTransaction_WithoutPriorQuartzRun_CreatesNextBusinessDayCyclesAndRoutesToCycleOne(
        int year, int month, int day, int targetYear, int targetMonth, int targetDay)
    {
        await using var fixture = await Fixture.CreateAsync(new DateTimeOffset(year, month, day, 16, 0, 0, TimeSpan.Zero));
        var routing = new RoutingStrategyService(
            fixture.Context,
            Mock.Of<IBankHoliday>(),
            fixture.Scheduler);

        // Time is supplied in Bogota local time; the next operational date is resolved per chamber.
        var cycleId = await routing.ResolveClearingHouseForTransactionAsync(
            2, new DateTime(year, month, day, 11, 0, 0), CancellationToken.None);
        var selected = await fixture.Context.AchCycles.SingleAsync(cycle => cycle.Id == cycleId);
        Assert.Equal(new DateTime(targetYear, targetMonth, targetDay), selected.ProcessingDate);
        Assert.Equal("Ciclo 1", selected.CycleName);
        Assert.Equal(7, await fixture.Context.AchCycles.CountAsync());

        var secondId = await routing.ResolveClearingHouseForTransactionAsync(
            2, new DateTime(year, month, day, 11, 10, 0), CancellationToken.None);
        Assert.Equal(cycleId, secondId);
        Assert.Equal(7, await fixture.Context.AchCycles.CountAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AchDbContext Context { get; }
        public AchCycleScheduler Scheduler { get; }

        private Fixture(SqliteConnection connection, AchDbContext context, AchCycleScheduler scheduler)
        {
            _connection = connection;
            Context = context;
            Scheduler = scheduler;
        }

        public static async Task<Fixture> CreateAsync(DateTimeOffset utcNow)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            var config = new ClearingHouseConfig
            {
                Id = 1, ClearingHouseId = 1, TimeZoneId = "America/Bogota", HolidayStrategy = "Colombian"
            };
            context.ClearingHouseConfigs.Add(config);
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "00112233"
            });
            var institution = new FinancialInstitution
            {
                Id = 2, Name = "Banco prueba", RoutingNumber = "7654321", TransitCode = "0",
                Status = FinancialInstitutionStatus.Active
            };
            institution.CalculateCheckDigit();
            institution.ClearingHousePreferences.Add(new InstitutionClearingHousePreference
            {
                ClearingHouseId = 1, IsActive = true, IsDefault = true, Priority = 1
            });
            context.FinancialInstitutions.Add(institution);
            await context.SaveChangesAsync();
            await RegulatoryCycleSeedRepair.ApplyAsync(
                context, 1, RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleConfirmedPolicy);

            var cenitPolicy = new Mock<ICenitOperatingCalendarPolicy>();
            cenitPolicy.Setup(policy => policy.ValidateCycleConsistencyAsync(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var scheduler = new AchCycleScheduler(
                context, Mock.Of<IBankHoliday>(), Mock.Of<IServiceProvider>(), cenitPolicy.Object,
                new FrozenClock(utcNow));
            return new Fixture(connection, context, scheduler);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class FrozenClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
