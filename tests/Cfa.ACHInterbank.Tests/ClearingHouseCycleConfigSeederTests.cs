using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
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
    public async Task SeedAsync_AchColombia_PreservesAllFiveV35Schedules()
    {
        await using var fixture = await SeederFixture.CreateAsync();

        await fixture.Seeder.SeedAsync();

        AssertSchedule(
            await fixture.CurrentCyclesAsync(31),
            RegulatoryCycleScheduleCatalog.GetRequired("ACHCOL"));
    }


    [Fact]
    public async Task SeedAsync_AchColombia_RunTwice_PreservesExactHistoricalFiveCycleSnapshot()
    {
        await using var fixture = await SeederFixture.CreateAsync();
        await fixture.Seeder.SeedAsync();
        var first = await fixture.CurrentCyclesAsync(31);
        var firstSnapshot = Snapshot(first);

        await fixture.Seeder.SeedAsync();
        var second = await fixture.CurrentCyclesAsync(31);

        Assert.Equal(firstSnapshot, Snapshot(second));
        AssertSchedule(second, RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalPolicy.Cycles);
        Assert.All(second, config =>
        {
            Assert.Equal(RegulatoryCycleScheduleCatalog.HistoricalBootstrapPolicyVersion, config.PolicyVersion);
            Assert.Equal(RegulatoryCycleScheduleCatalog.HistoricalBootstrapEffectiveFrom, config.EffectiveFrom);
            Assert.Null(config.EffectiveTo);
            Assert.Null(config.CompensationStatementStartTime);
            Assert.Null(config.CompensationStatementEndTime);
            Assert.Null(config.CompensationPaymentStartTime);
            Assert.Null(config.CompensationPaymentEndTime);
            Assert.Null(config.OutputReleaseStartTime);
            Assert.Null(config.OutputReleaseEndTime);
        });
        Assert.DoesNotContain(second, config =>
            string.Equals(config.PolicyVersion, RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidateVersion, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegulatoryCycleSeedRepair_NewVersion_ClosesPriorRangeWithoutRewritingHistoricalPayload()
    {
        await using var fixture = await SeederFixture.CreateAsync();
        await fixture.Seeder.SeedAsync();
        var before = await fixture.CurrentCyclesAsync(31);
        var historicalPayload = before
            .OrderBy(config => config.CycleName)
            .Select(PayloadSnapshot)
            .ToArray();
        var futureFrom = UtcDate(2099, 1, 2);
        var future = new RegulatoryCyclePolicySnapshot(
            RegulatoryCycleScheduleCatalog.AchColombiaCode,
            "TEST-ACH-7",
            futureFrom,
            BuildSyntheticSevenCyclePolicy());

        await RegulatoryCycleSeedRepair.ApplyAsync(
            fixture.Context,
            31,
            future,
            new CycleNumberResolver());

        var historical = await fixture.Context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(config => config.ClearingHouseId == 31
                && config.EffectiveFrom == RegulatoryCycleScheduleCatalog.HistoricalBootstrapEffectiveFrom)
            .OrderBy(config => config.CycleName)
            .ToListAsync();
        var futureRows = await fixture.Context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(config => config.ClearingHouseId == 31 && config.PolicyVersion == "TEST-ACH-7")
            .OrderBy(config => config.CycleName)
            .ToListAsync();

        Assert.Equal(historicalPayload, historical.Select(PayloadSnapshot).ToArray());
        Assert.All(historical, config => Assert.Equal(futureFrom.AddDays(-1), config.EffectiveTo));
        Assert.Equal(7, futureRows.Count);
        Assert.All(futureRows, config =>
        {
            Assert.Equal(futureFrom, config.EffectiveFrom);
            Assert.Null(config.EffectiveTo);
        });

        await fixture.Seeder.SeedAsync();
        Assert.Equal(12, await fixture.Context.ClearingHouseCycleConfigs.CountAsync(config => config.ClearingHouseId == 31));
        Assert.Equal(7, await fixture.Context.ClearingHouseCycleConfigs.CountAsync(config =>
            config.ClearingHouseId == 31 && config.PolicyVersion == "TEST-ACH-7"));

        var resolver = new ClearingHouseCyclePolicyResolver(fixture.Context, new CycleNumberResolver());
        var historicalResolved = await resolver.ResolveAsync(31, futureFrom.AddDays(-1));
        var futureResolved = await resolver.ResolveAsync(31, futureFrom);
        Assert.Equal(5, historicalResolved.Cycles.Count);
        Assert.Equal(RegulatoryCycleScheduleCatalog.HistoricalBootstrapPolicyVersion, historicalResolved.PolicyVersion);
        Assert.Equal(7, futureResolved.Cycles.Count);
        Assert.Equal("TEST-ACH-7", futureResolved.PolicyVersion);
    }

    [Fact]
    public async Task RegulatoryCycleSeedRepair_DifferentVersionWithSameEffectiveFrom_FailsClosedWithoutHistoricalRewrite()
    {
        await using var fixture = await SeederFixture.CreateAsync();
        await fixture.Seeder.SeedAsync();
        var historicalBefore = await fixture.Context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(config => config.ClearingHouseId == 31)
            .OrderBy(config => config.CycleName)
            .Select(config => new
            {
                config.PolicyVersion,
                config.CycleName,
                config.StartTime,
                config.EndTime,
                config.CutoffTime,
                config.OutputReleaseTime,
                config.EffectiveFrom,
                config.EffectiveTo
            })
            .ToArrayAsync();
        var conflicting = new RegulatoryCyclePolicySnapshot(
            RegulatoryCycleScheduleCatalog.AchColombiaCode,
            "TEST-CONFLICT",
            RegulatoryCycleScheduleCatalog.HistoricalBootstrapEffectiveFrom,
            BuildSyntheticSevenCyclePolicy());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RegulatoryCycleSeedRepair.ApplyAsync(fixture.Context, 31, conflicting, new CycleNumberResolver()));

        Assert.Contains("mismo EffectiveFrom", error.Message, StringComparison.OrdinalIgnoreCase);
        var historicalAfter = await fixture.Context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(config => config.ClearingHouseId == 31)
            .OrderBy(config => config.CycleName)
            .Select(config => new
            {
                config.PolicyVersion,
                config.CycleName,
                config.StartTime,
                config.EndTime,
                config.CutoffTime,
                config.OutputReleaseTime,
                config.EffectiveFrom,
                config.EffectiveTo
            })
            .ToArrayAsync();
        Assert.Equal(historicalBefore, historicalAfter);
    }

    [Fact]
    public async Task RegulatoryCycleSeedRepair_DefaultEffectiveFrom_FailsClosedWithoutWriting()
    {
        await using var fixture = await SeederFixture.CreateAsync();
        await fixture.Seeder.SeedAsync();
        var countBefore = await fixture.Context.ClearingHouseCycleConfigs.CountAsync(config => config.ClearingHouseId == 31);
        var invalid = new RegulatoryCyclePolicySnapshot(
            RegulatoryCycleScheduleCatalog.AchColombiaCode,
            "INVALID-NO-EFFECTIVE-FROM",
            default,
            BuildSyntheticSevenCyclePolicy());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RegulatoryCycleSeedRepair.ApplyAsync(fixture.Context, 31, invalid, new CycleNumberResolver()));

        Assert.Contains("EffectiveFrom", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(countBefore, await fixture.Context.ClearingHouseCycleConfigs.CountAsync(config => config.ClearingHouseId == 31));
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
            OutputReleaseTime = schedule.OutputReleaseTime,
            CompensationStatementStartTime = schedule.CompensationStatementStartTime,
            CompensationStatementEndTime = schedule.CompensationStatementEndTime,
            CompensationPaymentStartTime = schedule.CompensationPaymentStartTime,
            CompensationPaymentEndTime = schedule.CompensationPaymentEndTime,
            OutputReleaseStartTime = schedule.OutputReleaseStartTime,
            OutputReleaseEndTime = schedule.OutputReleaseEndTime,
            AllowsMonetaryCredit = schedule.AllowsMonetaryCredit,
            AllowsMonetaryDebit = schedule.AllowsMonetaryDebit,
            AllowsCreditPrenotification = schedule.AllowsCreditPrenotification,
            AllowsDebitPrenotification = schedule.AllowsDebitPrenotification,
            AllowsReturn = schedule.AllowsReturn,
            AllowsReturnOfReturn = schedule.AllowsReturnOfReturn,
            EffectiveFrom = UtcDate(2025, 1, 1),
            IsActive = true
        });

    private static void AssertSchedule(
        IReadOnlyCollection<ClearingHouseCycleConfig> actual,
        IReadOnlyList<RegulatoryCycleSchedule> expected)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var schedule in expected)
        {
            var config = actual.Single(item => item.CycleName == $"Ciclo {schedule.CycleNumber}");
            Assert.Equal(schedule.StartTime, config.StartTime);
            Assert.Equal(schedule.EndTime, config.EndTime);
            Assert.Equal(schedule.CutoffTime, config.CutoffTime);
            Assert.Equal(schedule.OutputReleaseTime, config.OutputReleaseTime);
            Assert.Equal(schedule.AllowsMonetaryCredit, config.AllowsMonetaryCredit);
            Assert.Equal(schedule.AllowsMonetaryDebit, config.AllowsMonetaryDebit);
            Assert.Equal(schedule.AllowsCreditPrenotification, config.AllowsCreditPrenotification);
            Assert.Equal(schedule.AllowsDebitPrenotification, config.AllowsDebitPrenotification);
            Assert.Equal(schedule.AllowsReturn, config.AllowsReturn);
            Assert.Equal(schedule.AllowsReturnOfReturn, config.AllowsReturnOfReturn);
            Assert.Equal(config.EndTime, config.CutoffTime);
        }
    }

    private static string[] Snapshot(IEnumerable<ClearingHouseCycleConfig> configs)
        => configs
            .OrderBy(config => config.Id)
            .Select(config => $"{config.Id}|{config.PolicyVersion}|{config.CycleName}|{config.StartTime}|{config.EndTime}|{config.CutoffTime}|{config.OutputReleaseTime}|{config.EffectiveFrom:O}|{config.EffectiveTo:O}")
            .ToArray();

    private static string PayloadSnapshot(ClearingHouseCycleConfig config)
        => $"{config.CycleName}|{config.StartTime}|{config.EndTime}|{config.CutoffTime}|{config.OutputReleaseTime}|"
            + $"{config.CompensationStatementStartTime}|{config.CompensationStatementEndTime}|"
            + $"{config.CompensationPaymentStartTime}|{config.CompensationPaymentEndTime}|"
            + $"{config.OutputReleaseStartTime}|{config.OutputReleaseEndTime}|"
            + $"{config.AllowsMonetaryCredit}|{config.AllowsMonetaryDebit}|"
            + $"{config.AllowsCreditPrenotification}|{config.AllowsDebitPrenotification}|"
            + $"{config.AllowsReturn}|{config.AllowsReturnOfReturn}";

    private static IReadOnlyList<RegulatoryCycleSchedule> BuildSyntheticSevenCyclePolicy()
        => Array.AsReadOnly(Enumerable.Range(1, 7)
            .Select(number =>
            {
                var start = new TimeSpan(6 + number, 0, 0);
                var end = start.Add(TimeSpan.FromMinutes(30));
                var release = end.Add(TimeSpan.FromMinutes(15));
                return new RegulatoryCycleSchedule(number, start, end, end, release);
            })
            .ToArray());

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
