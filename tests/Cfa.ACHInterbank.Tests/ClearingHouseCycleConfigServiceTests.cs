using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ClearingHouseCycleConfigServiceTests
{
    [Fact]
    public async Task CreateVersionAsync_ValidCreation_ClosesPreviousVersionAndCreatesNewCurrent()
    {
        using var context = CreateContext();
        await SeedClearingHouseAsync(context);

        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = 1,
            CycleName = "CICLO-1",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            CutoffTime = new TimeSpan(10, 0, 0),
            EffectiveFrom = new DateTime(2026, 1, 1),
            IsActive = true
        });

        await context.SaveChangesAsync();

        var service = new ClearingHouseCycleConfigService(context);

        var created = await service.CreateVersionAsync(new UpsertClearingHouseCycleConfigDto
        {
            ClearingHouseId = 1,
            CycleName = "CICLO-1",
            StartTime = new TimeSpan(8, 10, 0),
            EndTime = new TimeSpan(10, 10, 0),
            CutoffTime = new TimeSpan(10, 10, 0),
            EffectiveFrom = new DateTime(2026, 2, 1)
        });

        var allConfigs = await context.ClearingHouseCycleConfigs
            .Where(c => c.ClearingHouseId == 1 && c.CycleName == "CICLO-1")
            .OrderBy(c => c.EffectiveFrom)
            .ToListAsync();

        Assert.Equal(2, allConfigs.Count);
        Assert.True(allConfigs[0].IsActive);
        Assert.Equal(new DateTime(2026, 1, 31), allConfigs[0].EffectiveTo!.Value.Date);
        Assert.True(allConfigs[1].IsActive);
        Assert.Equal(created.Id, allConfigs[1].Id);
    }

    [Fact]
    public async Task CreateVersionAsync_RejectsOverlapWithHistoricalClosedVersion()
    {
        using var context = CreateContext();
        await SeedClearingHouseAsync(context);

        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = 1,
            CycleName = "CICLO-1",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            CutoffTime = new TimeSpan(10, 0, 0),
            EffectiveFrom = new DateTime(2026, 1, 1),
            EffectiveTo = new DateTime(2026, 1, 31),
            IsActive = false
        });

        await context.SaveChangesAsync();

        var service = new ClearingHouseCycleConfigService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateVersionAsync(new UpsertClearingHouseCycleConfigDto
        {
            ClearingHouseId = 1,
            CycleName = "CICLO-1",
            StartTime = new TimeSpan(8, 15, 0),
            EndTime = new TimeSpan(10, 15, 0),
            CutoffTime = new TimeSpan(10, 15, 0),
            EffectiveFrom = new DateTime(2026, 1, 15)
        }));

        Assert.Contains("traslape", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateVersionAsync_RejectsInvalidCutoff()
    {
        using var context = CreateContext();
        await SeedClearingHouseAsync(context);

        var service = new ClearingHouseCycleConfigService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateVersionAsync(new UpsertClearingHouseCycleConfigDto
        {
            ClearingHouseId = 1,
            CycleName = "CICLO-2",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            CutoffTime = new TimeSpan(11, 0, 0),
            EffectiveFrom = new DateTime(2026, 2, 1)
        }));

        Assert.Contains("corte", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InactivateAsync_RejectsInconsistentValidity()
    {
        using var context = CreateContext();
        await SeedClearingHouseAsync(context);

        var config = new ClearingHouseCycleConfig
        {
            ClearingHouseId = 1,
            CycleName = "CICLO-1",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            CutoffTime = new TimeSpan(10, 0, 0),
            EffectiveFrom = new DateTime(2026, 2, 1),
            IsActive = true
        };

        context.ClearingHouseCycleConfigs.Add(config);
        await context.SaveChangesAsync();

        var service = new ClearingHouseCycleConfigService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InactivateAsync(config.Id, new DateTime(2026, 1, 31)));

        Assert.Contains("vigencia", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCurrentByClearingHouseAsync_ReturnsLatestVersionPerCycleForDate()
    {
        using var context = CreateContext();
        await SeedClearingHouseAsync(context);

        context.ClearingHouseCycleConfigs.AddRange(
            new ClearingHouseCycleConfig
            {
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
                ClearingHouseId = 1,
                CycleName = "CICLO-2",
                StartTime = new TimeSpan(10, 30, 0),
                EndTime = new TimeSpan(12, 0, 0),
                CutoffTime = new TimeSpan(12, 0, 0),
                EffectiveFrom = new DateTime(2026, 1, 1),
                IsActive = true
            });

        await context.SaveChangesAsync();

        var service = new ClearingHouseCycleConfigService(context);

        var current = await service.GetCurrentByClearingHouseAsync(1, new DateTime(2026, 2, 10));

        Assert.Equal(2, current.Count);
        Assert.Contains(current, c => c.CycleName == "CICLO-1" && c.StartTime == new TimeSpan(8, 10, 0));
        Assert.Contains(current, c => c.CycleName == "CICLO-2");
    }

    [Fact]
    public async Task InactivateAsync_SetsInactiveAndEffectiveTo()
    {
        using var context = CreateContext();
        await SeedClearingHouseAsync(context);

        var config = new ClearingHouseCycleConfig
        {
            ClearingHouseId = 1,
            CycleName = "CICLO-3",
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            CutoffTime = new TimeSpan(12, 0, 0),
            EffectiveFrom = new DateTime(2026, 2, 1),
            IsActive = true
        };

        context.ClearingHouseCycleConfigs.Add(config);
        await context.SaveChangesAsync();

        var service = new ClearingHouseCycleConfigService(context);
        var result = await service.InactivateAsync(config.Id, new DateTime(2026, 2, 20));

        Assert.False(result.IsActive);
        Assert.Equal(new DateTime(2026, 2, 20), result.EffectiveTo!.Value.Date);
    }

    [Fact]
    public async Task CreateVersionAsync_CanCloneVersionWithNewCycleName()
    {
        using var context = CreateContext();
        await SeedClearingHouseAsync(context);

        var service = new ClearingHouseCycleConfigService(context);

        var clone = await service.CreateVersionAsync(new UpsertClearingHouseCycleConfigDto
        {
            ClearingHouseId = 1,
            CycleName = "CICLO-CLON",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(9, 0, 0),
            CutoffTime = new TimeSpan(9, 0, 0),
            EffectiveFrom = new DateTime(2026, 3, 1)
        });

        Assert.Equal("CICLO-CLON", clone.CycleName);
        Assert.True(clone.IsActive);
    }

    private static AchDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedClearingHouseAsync(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            HolidayStrategy = "Colombian"
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        await context.SaveChangesAsync();
    }
}
