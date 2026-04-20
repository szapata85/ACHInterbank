using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchCycleSchedulerTests
{
    [Fact]
    public async Task ScheduleCyclesForClearingHouseAsync_CreatesMultipleDailyCyclesIncludingContingency()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AchDbContext(options);
        context.Database.EnsureCreated();

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
                                                        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        context.ClearingHouseCycleConfigs.AddRange(
            new ClearingHouseCycleConfig
            {
                ClearingHouseId = 1,
                CycleName = "CICLO-1",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                CutoffTime = new TimeSpan(10, 0, 0),
                EffectiveFrom = new DateTime(2026, 1, 1),
                IsActive = true
            },
            new ClearingHouseCycleConfig
            {
                ClearingHouseId = 1,
                CycleName = "CICLO-2",
                StartTime = new TimeSpan(10, 1, 0),
                EndTime = new TimeSpan(12, 0, 0),
                CutoffTime = new TimeSpan(12, 0, 0),
                EffectiveFrom = new DateTime(2026, 1, 1),
                IsActive = true
            },
            new ClearingHouseCycleConfig
            {
                ClearingHouseId = 1,
                CycleName = "REPROCESO-1",
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(20, 0, 0),
                CutoffTime = new TimeSpan(20, 0, 0),
                EffectiveFrom = new DateTime(2026, 1, 1),
                IsActive = true
            });

        await context.SaveChangesAsync();

        var holidayService = new Mock<IBankHoliday>();
        holidayService.Setup(x => x.GetHolidays(It.IsAny<int>())).Returns([]);
        var provider = new Mock<IServiceProvider>();
        var cenitPolicy = new Mock<ICenitOperatingCalendarPolicy>();
        cenitPolicy.Setup(x => x.ValidateCycleConsistencyAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var scheduler = new AchCycleScheduler(context, holidayService.Object, provider.Object, cenitPolicy.Object);

        await scheduler.ScheduleCyclesForClearingHouseAsync(1, new DateTime(2026, 03, 23));

        var cycles = await context.AchCycles
            .Where(x => x.ClearingHouseId == 1)
            .OrderBy(x => x.CutoffTime)
            .ToListAsync();

        Assert.Equal(3, cycles.Count);
        Assert.Contains(cycles, x => x.CycleName == "CICLO-1");
        Assert.Contains(cycles, x => x.CycleName == "CICLO-2");
        Assert.Contains(cycles, x => x.CycleName == "REPROCESO-1");
        Assert.All(cycles, x => Assert.True(x.ClearingHouseCycleConfigId.HasValue));
    }

    [Fact]
    public async Task ScheduleCyclesForClearingHouseAsync_UsesOnlyEffectiveActiveLatestConfiguration()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AchDbContext(options);
        context.Database.EnsureCreated();

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
                                                        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        context.ClearingHouseCycleConfigs.AddRange(
            new ClearingHouseCycleConfig
            {
                Id = 100,
                ClearingHouseId = 1,
                CycleName = "CICLO-1",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                CutoffTime = new TimeSpan(10, 0, 0),
                EffectiveFrom = new DateTime(2026, 1, 1),
                EffectiveTo = new DateTime(2026, 1, 31),
                IsActive = false
            },
            new ClearingHouseCycleConfig
            {
                Id = 101,
                ClearingHouseId = 1,
                CycleName = "CICLO-1",
                StartTime = new TimeSpan(8, 10, 0),
                EndTime = new TimeSpan(10, 10, 0),
                CutoffTime = new TimeSpan(10, 10, 0),
                EffectiveFrom = new DateTime(2026, 2, 1),
                IsActive = true
            },
            new ClearingHouseCycleConfig
            {
                Id = 102,
                ClearingHouseId = 1,
                CycleName = "CICLO-2",
                StartTime = new TimeSpan(10, 30, 0),
                EndTime = new TimeSpan(12, 0, 0),
                CutoffTime = new TimeSpan(12, 0, 0),
                EffectiveFrom = new DateTime(2026, 2, 1),
                IsActive = true
            });

        await context.SaveChangesAsync();

        var holidayService = new Mock<IBankHoliday>();
        holidayService.Setup(x => x.GetHolidays(It.IsAny<int>())).Returns([]);
        var provider = new Mock<IServiceProvider>();
        var cenitPolicy = new Mock<ICenitOperatingCalendarPolicy>();
        cenitPolicy.Setup(x => x.ValidateCycleConsistencyAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var scheduler = new AchCycleScheduler(context, holidayService.Object, provider.Object, cenitPolicy.Object);

        await scheduler.ScheduleCyclesForClearingHouseAsync(1, new DateTime(2026, 02, 10));

        var cycles = await context.AchCycles
            .Where(x => x.ClearingHouseId == 1)
            .OrderBy(x => x.CutoffTime)
            .ToListAsync();

        Assert.Equal(2, cycles.Count);
        Assert.Contains(cycles, c => c.CycleName == "CICLO-1" && c.StartTime == new TimeSpan(8, 10, 0));
        Assert.Contains(cycles, c => c.CycleName == "CICLO-2");
        Assert.DoesNotContain(cycles, c => c.ClearingHouseCycleConfigId == 100);
        Assert.Contains(cycles, c => c.ClearingHouseCycleConfigId == 101);
        Assert.Contains(cycles, c => c.ClearingHouseCycleConfigId == 102);
    }
}
