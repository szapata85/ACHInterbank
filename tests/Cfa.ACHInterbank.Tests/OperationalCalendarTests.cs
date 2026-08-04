using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ColombianNationalHolidayGeneratorTests
{
    private readonly ColombianHolidayStrategy _generator = new();

    [Fact]
    public void EasterAndDependentHolidays_AreCalculatedDeterministically()
    {
        Assert.Equal(new DateOnly(2026, 4, 5), ColombianHolidayStrategy.GetEasterSunday(2026));
        var holidays = _generator.GenerateHolidays(2026).ToDictionary(x => x.RuleCode!);
        Assert.Equal(new DateOnly(2026, 4, 2), holidays["CO_HOLY_THURSDAY"].Date);
        Assert.Equal(new DateOnly(2026, 4, 3), holidays["CO_GOOD_FRIDAY"].Date);
        Assert.Equal(new DateOnly(2026, 5, 18), holidays["CO_ASCENSION"].Date);
        Assert.Equal(new DateOnly(2026, 6, 8), holidays["CO_CORPUS_CHRISTI"].Date);
        Assert.Equal(new DateOnly(2026, 6, 15), holidays["CO_SACRED_HEART"].Date);
    }

    [Theory]
    [InlineData(2026, 6, 29, 2026, 6, 29)]
    [InlineData(2026, 6, 30, 2026, 7, 6)]
    [InlineData(2026, 7, 4, 2026, 7, 6)]
    [InlineData(2026, 7, 5, 2026, 7, 6)]
    public void Emiliani_MovesMondayTuesdaySaturdayAndSundayCorrectly(
        int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
        => Assert.Equal(
            new DateOnly(expectedYear, expectedMonth, expectedDay),
            ColombianHolidayStrategy.MoveToNextMonday(new DateOnly(year, month, day)));

    [Fact]
    public void Chiquinquira_StartsIn2026_WithCommemorativeAndEffectiveDates()
    {
        Assert.DoesNotContain(_generator.GenerateHolidays(2025), x => x.RuleCode == "CO_CHIQUINQUIRA");
        var holiday = Assert.Single(_generator.GenerateHolidays(2026), x => x.RuleCode == "CO_CHIQUINQUIRA");
        Assert.Equal(new DateOnly(2026, 7, 9), holiday.CommemorativeDate);
        Assert.Equal(new DateOnly(2026, 7, 13), holiday.Date);
        Assert.Equal(BankHolidayRuleKind.ChiquinquiraEmiliani, holiday.RuleKind);
        Assert.Equal(2026, holiday.EffectiveFromYear);
    }

    [Theory]
    [InlineData(2025, 18)]
    [InlineData(2026, 19)]
    [InlineData(2024, 18)]
    [InlineData(2027, 19)]
    public void Calendar_HasExpectedLegalObservanceCountAndUniqueRuleIdentity(int year, int expected)
    {
        var holidays = _generator.GenerateHolidays(year);
        Assert.Equal(expected, holidays.Count);
        Assert.Equal(expected, holidays.Select(x => (x.RuleCode, x.CommemorativeDate)).Distinct().Count());
    }

    [Fact]
    public void AcceptanceCalendar2026_IsExact()
    {
        var expected = new[]
        {
            "2026-01-01", "2026-01-12", "2026-03-23", "2026-04-02", "2026-04-03",
            "2026-05-01", "2026-05-18", "2026-06-08", "2026-06-15", "2026-06-29",
            "2026-07-13", "2026-07-20", "2026-08-07", "2026-08-17", "2026-10-12",
            "2026-11-02", "2026-11-16", "2026-12-08", "2026-12-25"
        }.Select(DateOnly.Parse).ToArray();

        Assert.Equal(expected, _generator.GenerateHolidays(2026).Select(x => x.Date));
        Assert.Equal(19, expected.Distinct().Count());
    }
}

public sealed class OperationalCalendarIntegrationTests
{
    [Fact]
    public async Task WeekendsNationalHolidayAndNextBusinessDay_AreApplied()
    {
        await using var fixture = await CalendarFixture.CreateAsync();
        Assert.False(await fixture.Calendar.IsBusinessDayAsync(new DateOnly(2026, 5, 2), 1));
        Assert.False(await fixture.Calendar.IsBusinessDayAsync(new DateOnly(2026, 5, 3), 1));
        Assert.False(await fixture.Calendar.IsBusinessDayAsync(new DateOnly(2026, 5, 1), 1));
        Assert.Equal(
            new DateOnly(2026, 5, 4),
            await fixture.Calendar.GetNextBusinessDayAsync(new DateOnly(2026, 5, 1), 1));
    }

    [Fact]
    public async Task SpecialDates_AreIsolatedByClearingHouse_AndInactiveDoesNotBlock()
    {
        await using var fixture = await CalendarFixture.CreateAsync();
        fixture.Context.ClearingHouseSpecialDates.AddRange(
            new ClearingHouseSpecialDate
            {
                ClearingHouseId = 1,
                Date = new DateOnly(2026, 7, 15),
                Description = "Cierre ACH Colombia",
                IsActive = true
            },
            new ClearingHouseSpecialDate
            {
                ClearingHouseId = 2,
                Date = new DateOnly(2026, 7, 16),
                Description = "Cierre CENIT inactivo",
                IsActive = false
            });
        await fixture.Context.SaveChangesAsync();

        Assert.False(await fixture.Calendar.IsBusinessDayAsync(new DateOnly(2026, 7, 15), 1));
        Assert.True(await fixture.Calendar.IsBusinessDayAsync(new DateOnly(2026, 7, 15), 2));
        Assert.True(await fixture.Calendar.IsBusinessDayAsync(new DateOnly(2026, 7, 16), 1));
        Assert.True(await fixture.Calendar.IsBusinessDayAsync(new DateOnly(2026, 7, 16), 2));
        Assert.Equal(new DateOnly(2026, 7, 16), await fixture.Calendar.GetNextBusinessDayAsync(new DateOnly(2026, 7, 15), 1));
        Assert.Equal(new DateOnly(2026, 7, 15), await fixture.Calendar.GetNextBusinessDayAsync(new DateOnly(2026, 7, 15), 2));
    }

    [Fact]
    public async Task SameSpecialDate_CanExistIndependentlyForBothClearingHouses()
    {
        await using var fixture = await CalendarFixture.CreateAsync();
        var service = new ClearingHouseSpecialDateService(fixture.Context, fixture.Calendar);
        var date = new DateTime(2026, 9, 9);

        await service.CreateAsync(new() { ClearingHouseId = 1, Date = date, Description = "Cierre ACH", IsActive = true });
        await service.CreateAsync(new() { ClearingHouseId = 2, Date = date, Description = "Cierre CENIT", IsActive = true });

        Assert.Equal(2, await fixture.Context.ClearingHouseSpecialDates.CountAsync(x => x.Date == DateOnly.FromDateTime(date)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new() { ClearingHouseId = 1, Date = date, Description = "Duplicada", IsActive = true }));
    }

    [Fact]
    public async Task Provisioning_IsIdempotent_RepairsMissingHoliday_AndPreservesManualSpecialDates()
    {
        await using var fixture = await CalendarFixture.CreateAsync();
        var provisioning = new BankHolidayProvisioningService(fixture.Context);
        fixture.Context.ClearingHouseSpecialDates.Add(new ClearingHouseSpecialDate
        {
            ClearingHouseId = 1,
            Date = new DateOnly(2026, 9, 10),
            Description = "Configuración manual",
            IsActive = true
        });
        await fixture.Context.SaveChangesAsync();

        var first = await provisioning.EnsureYearsAsync([2025, 2026]);
        var second = await provisioning.EnsureYearsAsync([2025, 2026]);
        Assert.Equal(37, first.Inserted);
        Assert.Equal(0, second.Inserted);
        Assert.Equal(37, second.Existing);

        var missing = await fixture.Context.BankHolidays.SingleAsync(x => x.RuleCode == "CO_CHIQUINQUIRA");
        fixture.Context.BankHolidays.Remove(missing);
        await fixture.Context.SaveChangesAsync();
        var repaired = await provisioning.EnsureYearsAsync([2026]);

        Assert.Equal(1, repaired.Inserted);
        Assert.Equal(19, await fixture.Context.BankHolidays.CountAsync(x => x.Date.Year == 2026));
        Assert.Single(fixture.Context.ClearingHouseSpecialDates);
    }

    [Fact]
    public async Task TaskDefinitionSeeder_EnablesManualHolidaySeedWithoutDuplicatingTask()
    {
        await using var fixture = await CalendarFixture.CreateAsync();
        fixture.Context.TaskDefinitions.Add(new()
        {
            Code = "SeedBankHolidays",
            Name = "Festivos",
            TimeZoneId = "America/Bogota",
            ManualExecutionEnabled = false
        });
        await fixture.Context.SaveChangesAsync();
        var seeder = new TaskDefinitionSeeder(fixture.Context);

        await ((Cfa.ACHInterbank.Application.DataBase.IDbSeeder)seeder).SeedAsync();
        await ((Cfa.ACHInterbank.Application.DataBase.IDbSeeder)seeder).SeedAsync();

        var task = await fixture.Context.TaskDefinitions.SingleAsync(x => x.Code == "SeedBankHolidays");
        Assert.True(task.ManualExecutionEnabled);
    }

    [Fact]
    public async Task Provisioning_AdoptsOnlyUnambiguousLegacyLegalRecord()
    {
        await using var fixture = await CalendarFixture.CreateAsync();
        var legacyLegal = new BankHolidayModel
        {
            Date = new DateOnly(2026, 8, 17),
            Description = "La Asunción",
            CountryCode = "CO"
        };
        var manual = new BankHolidayModel
        {
            Date = new DateOnly(2026, 9, 10),
            Description = "Cierre manual",
            CountryCode = "CO"
        };
        fixture.Context.BankHolidays.AddRange(legacyLegal, manual);
        await fixture.Context.SaveChangesAsync();

        await new BankHolidayProvisioningService(fixture.Context).EnsureYearsAsync([2026]);

        Assert.True(legacyLegal.IsSystemGenerated);
        Assert.Equal("CO_ASSUMPTION", legacyLegal.RuleCode);
        Assert.Equal(new DateOnly(2026, 8, 15), legacyLegal.CommemorativeDate);
        Assert.False(manual.IsSystemGenerated);
        Assert.Null(manual.RuleCode);
        Assert.Equal("Cierre manual", manual.Description);
    }

    [Fact]
    public async Task CycleGuard_DefersOnce_PreservesIdentityTimesAndTransactionsLink()
    {
        await using var fixture = await CalendarFixture.CreateAsync();
        var cycle = new AchCycle
        {
            Id = "cycle-calendar-1",
            CycleName = "CICLO 1",
            ClearingHouseId = 1,
            ProcessingDate = new DateTime(2026, 7, 13),
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(10),
            CutoffTime = TimeSpan.FromHours(9),
            RescheduleOnHoliday = true,
            OperationalStatus = AchCycleOperationalStatus.Scheduled
        };
        fixture.Context.AchCycles.Add(cycle);
        await fixture.Context.SaveChangesAsync();
        var guard = new CycleCalendarGuard(fixture.Context, fixture.Calendar, new FixedClock(new DateTimeOffset(2026, 7, 13, 13, 0, 0, TimeSpan.Zero)));

        var result = await guard.EnsureExecutableAsync(cycle);

        Assert.False(result.CanExecute);
        Assert.Equal(new DateOnly(2026, 7, 14), result.RescheduledDate);
        Assert.Equal("cycle-calendar-1", cycle.Id);
        Assert.Equal(TimeSpan.FromHours(8), cycle.StartTime);
        Assert.Equal(TimeSpan.FromHours(10), cycle.EndTime);
        Assert.Equal(new DateTime(2026, 7, 13), cycle.OriginalProcessingDate);
        Assert.Equal(new DateTime(2026, 7, 14), cycle.ProcessingDate);
        Assert.Equal(1, cycle.CalendarDeferralCount);
        Assert.Contains("Chiquinquirá", cycle.CalendarDeferralReason);
        var staleConcurrentDecision = await guard.EnsureExecutableAsync(new AchCycle
        {
            Id = cycle.Id,
            CycleName = cycle.CycleName,
            ClearingHouseId = cycle.ClearingHouseId,
            ProcessingDate = new DateTime(2026, 7, 13),
            RescheduleOnHoliday = true
        });
        Assert.False(staleConcurrentDecision.CanExecute);
        Assert.False(staleConcurrentDecision.WasDeferred);
        Assert.Equal(new DateOnly(2026, 7, 14), staleConcurrentDecision.RescheduledDate);
        Assert.Equal(1, cycle.CalendarDeferralCount);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class CalendarFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private CalendarFixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            Calendar = new OperationalCalendarService(context);
        }

        public AchDbContext Context { get; }
        public OperationalCalendarService Calendar { get; }

        public static async Task<CalendarFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            context.ClearingHouseConfigs.AddRange(
                new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian", TimeZoneId = "America/Bogota" },
                new ClearingHouseConfig { Id = 2, ClearingHouseId = 2, HolidayStrategy = "Colombian", TimeZoneId = "America/Bogota" });
            context.ClearingHouses.AddRange(
                new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACH", OriginCode = "001", ClearingHouseId = 1 },
                new ClearingHouse { Id = 2, Name = "CENIT", Code = "CENIT", OriginCode = "002", ClearingHouseId = 2 });
            await context.SaveChangesAsync();
            return new CalendarFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
