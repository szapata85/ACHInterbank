using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class ClearingHouseCyclePolicyResolverTests
{
    [Fact]
    public async Task ResolveAsync_DifferentChambersAndCounts_RemainIsolated()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 1, 11, "ACHCOL", "America/Bogota");
        SeedHouse(context, 2, 22, "CENIT", "America/Bogota");
        AddPolicy(context, 1, "ACH-V1", 7, new DateTime(2026, 1, 1));
        AddPolicy(context, 2, "CENIT-V1", 5, new DateTime(2026, 1, 1));
        context.ClearingHouseCycleConfigs.Local.Single(cycle => cycle.ClearingHouseId == 1 && cycle.CycleName == "Ciclo 1").AllowsReturn = true;
        context.ClearingHouseCycleConfigs.Local.Single(cycle => cycle.ClearingHouseId == 2 && cycle.CycleName == "Ciclo 1").AllowsReturn = false;
        await context.SaveChangesAsync();
        var sut = new ClearingHouseCyclePolicyResolver(context, new CycleNumberResolver());

        var ach = await sut.ResolveAsync(1, new DateTime(2026, 8, 25));
        var cenit = await sut.ResolveAsync(2, new DateTime(2026, 8, 25));

        Assert.Equal(7, ach.Cycles.Count);
        Assert.Equal(5, cenit.Cycles.Count);
        Assert.All(ach.Cycles, cycle => Assert.Equal(1, cycle.ClearingHouseId));
        Assert.All(cenit.Cycles, cycle => Assert.Equal(2, cycle.ClearingHouseId));
        Assert.True(ach.Cycles.Single(cycle => cycle.CycleName == "Ciclo 1").AllowsReturn);
        Assert.False(cenit.Cycles.Single(cycle => cycle.CycleName == "Ciclo 1").AllowsReturn);
    }

    [Fact]
    public async Task ResolveAsync_VersionBoundary_IsDeterministic()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 1, 11, "ACHCOL", "America/Bogota");
        AddPolicy(context, 1, "V1", 2, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        AddPolicy(context, 1, "V2", 3, new DateTime(2026, 2, 1));
        await context.SaveChangesAsync();
        var sut = new ClearingHouseCyclePolicyResolver(context, new CycleNumberResolver());

        Assert.Equal("V1", (await sut.ResolveAsync(1, new DateTime(2026, 1, 31))).PolicyVersion);
        Assert.Equal("V2", (await sut.ResolveAsync(1, new DateTime(2026, 2, 1))).PolicyVersion);
    }

    [Fact]
    public async Task ResolveAsync_OverlappingVersions_FailsClosed()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 1, 11, "ACHCOL", "America/Bogota");
        AddPolicy(context, 1, "V1", 1, new DateTime(2026, 1, 1));
        AddPolicy(context, 1, "V2", 1, new DateTime(2026, 2, 1));
        await context.SaveChangesAsync();
        var sut = new ClearingHouseCyclePolicyResolver(context, new CycleNumberResolver());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ResolveAsync(1, new DateTime(2026, 2, 1)));

        Assert.Contains("múltiples versiones", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAsync_ReleaseBeforeClose_FailsClosed()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 1, 11, "ACHCOL", "America/Bogota");
        var cycle = Cycle(1, "V1", 1, new DateTime(2026, 1, 1));
        cycle.OutputReleaseTime = new TimeSpan(8, 30, 0);
        context.ClearingHouseCycleConfigs.Add(cycle);
        await context.SaveChangesAsync();
        var sut = new ClearingHouseCyclePolicyResolver(context, new CycleNumberResolver());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ResolveAsync(1, new DateTime(2026, 8, 25)));
    }

    [Fact]
    public async Task ResolveAtInstantAsync_UsesConfiguredTimeZone()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 1, 11, "ACHCOL", "America/Bogota");
        AddPolicy(context, 1, "V1", 1, new DateTime(2026, 8, 24), new DateTime(2026, 8, 24));
        AddPolicy(context, 1, "V2", 1, new DateTime(2026, 8, 25));
        await context.SaveChangesAsync();
        var sut = new ClearingHouseCyclePolicyResolver(context, new CycleNumberResolver());

        var resolved = await sut.ResolveAtInstantAsync(
            1,
            new DateTimeOffset(2026, 8, 25, 3, 30, 0, TimeSpan.Zero));

        Assert.Equal("V1", resolved.PolicyVersion);
        Assert.Equal(new DateTime(2026, 8, 24), resolved.OperationalDate);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedHouse(AchDbContext context, int houseId, int configId, string code, string timeZoneId)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = configId,
            ClearingHouseId = houseId,
            TimeZoneId = timeZoneId,
            HolidayStrategy = "Colombian"
        });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = houseId,
            ClearingHouseId = configId,
            Code = code,
            Name = code,
            OriginCode = $"{houseId:00000000}",
            IsActive = true
        });
    }

    private static void AddPolicy(
        AchDbContext context,
        int houseId,
        string version,
        int count,
        DateTime from,
        DateTime? to = null)
        => context.ClearingHouseCycleConfigs.AddRange(
            Enumerable.Range(1, count).Select(number => Cycle(houseId, version, number, from, to)));

    private static ClearingHouseCycleConfig Cycle(
        int houseId,
        string version,
        int number,
        DateTime from,
        DateTime? to = null)
        => new()
        {
            ClearingHouseId = houseId,
            PolicyVersion = version,
            CycleName = $"Ciclo {number}",
            StartTime = new TimeSpan(8, 0, 0),
            CutoffTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            OutputReleaseTime = new TimeSpan(10, 0, 0),
            EffectiveFrom = from,
            EffectiveTo = to,
            IsActive = true
        };
}
