using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ClearingHouseCycleConfigSeederTests
{
    [Fact]
    public async Task SeedAsync_NewDatabase_CreatesExactlyFiveNormativeCenitCycles()
    {
        await using var fixture = await SeederFixture.CreateAsync();

        await fixture.Seeder.SeedAsync();

        var cenit = await fixture.CurrentCyclesAsync(47);
        AssertSchedule(cenit, RegulatoryCycleScheduleCatalog.GetRequired("CENIT"));
    }

    [Fact]
    public async Task SeedAsync_CompleteKnownInvalidCenitFingerprint_RepairsInPlaceWithoutDuplicates()
    {
        await using var fixture = await SeederFixture.CreateAsync();
        var invalid = RegulatoryCycleScheduleCatalog.GetRequired("ACHCOL");
        fixture.Context.ClearingHouseCycleConfigs.AddRange(BuildConfigs(47, invalid, firstId: 100));
        await fixture.Context.SaveChangesAsync();
        var originalIds = await fixture.Context.ClearingHouseCycleConfigs
            .Where(config => config.ClearingHouseId == 47)
            .Select(config => config.Id)
            .OrderBy(id => id)
            .ToArrayAsync();

        await fixture.Seeder.SeedAsync();

        var cenit = await fixture.CurrentCyclesAsync(47);
        AssertSchedule(cenit, RegulatoryCycleScheduleCatalog.GetRequired("CENIT"));
        Assert.Equal(originalIds, cenit.Select(config => config.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task SeedAsync_RunTwice_LeavesSameFiveCenitRecordsAndValues()
    {
        await using var fixture = await SeederFixture.CreateAsync();
        await fixture.Seeder.SeedAsync();
        var first = Snapshot(await fixture.CurrentCyclesAsync(47));

        await fixture.Seeder.SeedAsync();
        var second = Snapshot(await fixture.CurrentCyclesAsync(47));

        Assert.Equal(first, second);
        Assert.Equal(5, second.Length);
    }

    [Fact]
    public async Task SeedAsync_PossibleManualCenitConfiguration_DoesNotOverwriteIt()
    {
        await using var fixture = await SeederFixture.CreateAsync();
        fixture.Context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = 47,
            CycleName = "Ciclo 1",
            StartTime = new TimeSpan(7, 31, 0),
            EndTime = new TimeSpan(10, 30, 0),
            CutoffTime = new TimeSpan(10, 30, 0),
            EffectiveFrom = UtcDate(2025, 1, 1),
            IsActive = true
        });
        await fixture.Context.SaveChangesAsync();

        await fixture.Seeder.SeedAsync();

        var cenit = await fixture.CurrentCyclesAsync(47);
        Assert.Equal(5, cenit.Count);
        Assert.Equal(new TimeSpan(7, 31, 0), cenit.Single(config => config.CycleName == "Ciclo 1").StartTime);
    }

    [Fact]
    public async Task SeedAsync_OneMissingCenitCycle_InsertsOnlyMissingCycle()
    {
        await using var fixture = await SeederFixture.CreateAsync();
        var normative = RegulatoryCycleScheduleCatalog.GetRequired("CENIT");
        fixture.Context.ClearingHouseCycleConfigs.AddRange(BuildConfigs(47, normative.Where(item => item.CycleNumber != 3)));
        await fixture.Context.SaveChangesAsync();
        var originalIds = await fixture.Context.ClearingHouseCycleConfigs
            .Where(config => config.ClearingHouseId == 47)
            .Select(config => config.Id)
            .ToArrayAsync();

        await fixture.Seeder.SeedAsync();

        var cenit = await fixture.CurrentCyclesAsync(47);
        AssertSchedule(cenit, normative);
        Assert.All(originalIds, id => Assert.Contains(cenit, config => config.Id == id));
    }

    [Fact]
    public async Task SeedAsync_AchColombia_PreservesAllFiveV32Schedules()
    {
        await using var fixture = await SeederFixture.CreateAsync();

        await fixture.Seeder.SeedAsync();

        AssertSchedule(
            await fixture.CurrentCyclesAsync(31),
            RegulatoryCycleScheduleCatalog.GetRequired("ACHCOL"));
    }

    [Fact]
    public async Task SeedAsync_AnotherClearingHouse_RemainsUnchanged()
    {
        await using var fixture = await SeederFixture.CreateAsync(includeFutureHouse: true);
        var manual = new ClearingHouseCycleConfig
        {
            ClearingHouseId = 88,
            CycleName = "Ciclo 5",
            StartTime = new TimeSpan(1, 2, 0),
            EndTime = new TimeSpan(3, 4, 0),
            CutoffTime = new TimeSpan(3, 4, 0),
            EffectiveFrom = UtcDate(2025, 1, 1),
            IsActive = true
        };
        fixture.Context.ClearingHouseCycleConfigs.Add(manual);
        await fixture.Context.SaveChangesAsync();

        await fixture.Seeder.SeedAsync();

        var persisted = await fixture.Context.ClearingHouseCycleConfigs.SingleAsync(config => config.Id == manual.Id);
        Assert.Equal(new TimeSpan(1, 2, 0), persisted.StartTime);
        Assert.Equal(new TimeSpan(3, 4, 0), persisted.EndTime);
        Assert.Single(await fixture.Context.ClearingHouseCycleConfigs.Where(config => config.ClearingHouseId == 88).ToListAsync());
    }

    private static IEnumerable<ClearingHouseCycleConfig> BuildConfigs(
        int clearingHouseId,
        IEnumerable<RegulatoryCycleSchedule> schedules,
        int? firstId = null)
        => schedules.Select((schedule, index) => new ClearingHouseCycleConfig
        {
            Id = firstId.HasValue ? firstId.Value + index : 0,
            ClearingHouseId = clearingHouseId,
            CycleName = $"Ciclo {schedule.CycleNumber}",
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            CutoffTime = schedule.CutoffTime,
            EffectiveFrom = UtcDate(2025, 1, 1),
            IsActive = true
        });

    private static void AssertSchedule(
        IReadOnlyCollection<ClearingHouseCycleConfig> actual,
        IReadOnlyList<RegulatoryCycleSchedule> expected)
    {
        Assert.Equal(5, actual.Count);
        foreach (var schedule in expected)
        {
            var config = actual.Single(item => item.CycleName == $"Ciclo {schedule.CycleNumber}");
            Assert.Equal(schedule.StartTime, config.StartTime);
            Assert.Equal(schedule.EndTime, config.EndTime);
            Assert.Equal(schedule.CutoffTime, config.CutoffTime);
            Assert.Equal(config.EndTime, config.CutoffTime);
        }
    }

    private static string[] Snapshot(IEnumerable<ClearingHouseCycleConfig> configs)
        => configs
            .OrderBy(config => config.Id)
            .Select(config => $"{config.Id}|{config.CycleName}|{config.StartTime}|{config.EndTime}|{config.CutoffTime}|{config.EffectiveFrom:O}|{config.EffectiveTo:O}")
            .ToArray();

    private static DateTime UtcDate(int year, int month, int day)
        => DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

    private sealed class SeederFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SeederFixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            Seeder = new ClearingHouseCycleConfigSeeder(context);
        }

        public AchDbContext Context { get; }
        public ClearingHouseCycleConfigSeeder Seeder { get; }

        public static async Task<SeederFixture> CreateAsync(bool includeFutureHouse = false)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 10,
                ClearingHouseId = 0,
                HolidayStrategy = "Colombian",
                TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId
            });
            context.ClearingHouses.AddRange(
                new ClearingHouse { Id = 31, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "12345678", ClearingHouseId = 10 },
                new ClearingHouse { Id = 47, Name = "CENIT", Code = "CENIT", OriginCode = "87654321", ClearingHouseId = 10 });
            if (includeFutureHouse)
            {
                context.ClearingHouses.Add(new ClearingHouse { Id = 88, Name = "Future", Code = "FUTURE", OriginCode = "11223344", ClearingHouseId = 10 });
            }

            await context.SaveChangesAsync();
            return new SeederFixture(connection, context);
        }

        public Task<List<ClearingHouseCycleConfig>> CurrentCyclesAsync(int clearingHouseId)
            => Context.ClearingHouseCycleConfigs
                .AsNoTracking()
                .Where(config => config.ClearingHouseId == clearingHouseId
                    && config.IsActive
                    && config.EffectiveFrom.Year <= 2026
                    && (!config.EffectiveTo.HasValue || config.EffectiveTo.Value.Year >= 2026))
                .OrderBy(config => config.CycleName)
                .ToListAsync();

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
