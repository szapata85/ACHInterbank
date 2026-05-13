using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class RegulatoryCatalogSeederReturnCodesByClearingHouseTests
{
    [Fact]
    public async Task Seeder_ShouldAssignReturnCodesToCenitAndAchClearingHouses()
    {
        await using var context = await CreateContextAsync();
        var cenit = await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        var ach = await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");

        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();

        Assert.True(await context.AchReturnCodes.AnyAsync(x => x.ClearingHouseId == cenit.Id));
        Assert.True(await context.AchReturnCodes.AnyAsync(x => x.ClearingHouseId == ach.Id));
        Assert.False(await context.AchReturnCodes.AnyAsync(x => x.ClearingHouseId == 0));
    }

    [Fact]
    public async Task Seeder_ShouldAssignCenitRegulatorySourceToCenit()
    {
        await using var context = await CreateContextAsync();
        var cenit = await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");

        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();

        var rows = await context.AchReturnCodes.Where(x => x.RegulatorySource == "CENIT").ToListAsync();
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.Equal(cenit.Id, row.ClearingHouseId));
    }

    [Fact]
    public async Task Seeder_ShouldAssignAchRegulatorySourceToAchColombia()
    {
        await using var context = await CreateContextAsync();
        await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        var ach = await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");

        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();

        var rows = await context.AchReturnCodes.Where(x => x.RegulatorySource == "ACH").ToListAsync();
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.Equal(ach.Id, row.ClearingHouseId));
    }

    [Fact]
    public async Task Seeder_ShouldAssignOperadorRegulatorySourceToAchColombia()
    {
        await using var context = await CreateContextAsync();
        await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        var ach = await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");

        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();

        var rows = await context.AchReturnCodes.Where(x => x.RegulatorySource == "OPERADOR").ToListAsync();
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.Equal(ach.Id, row.ClearingHouseId));
    }

    [Fact]
    public async Task Seeder_ShouldUpsertReturnCodesByClearingHouseCodeAndFlowType()
    {
        await using var context = await CreateContextAsync();
        var cenit = await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        var ach = await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");

        context.AchReturnCodes.Add(new AchReturnCode
        {
            ClearingHouseId = ach.Id,
            Code = "R01",
            FlowType = "Any",
            Description = "ACH R01 manual",
            AppliesToDebit = true,
            AppliesToCredit = true,
            AppliesToPrenotification = false,
            AppliesToReturn = true,
            RequiresAddenda = true,
            MaxDaysAllowed = 10,
            EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1),
            IsActive = true,
            RegulatorySource = "ACH"
        });
        await context.SaveChangesAsync();

        var sut = new RegulatoryCatalogSeeder(context);
        await sut.SeedAsync();

        var achR01 = await context.AchReturnCodes.SingleAsync(x => x.ClearingHouseId == ach.Id && x.Code == "R01" && x.FlowType == "Any");
        var cenitR01 = await context.AchReturnCodes.SingleAsync(x => x.ClearingHouseId == cenit.Id && x.Code == "R01" && x.FlowType == "Any");

        Assert.Equal("ACH R01 manual", achR01.Description);
        Assert.NotEqual(achR01.ClearingHouseId, cenitR01.ClearingHouseId);
    }

    private static async Task<AchDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<ClearingHouse> EnsureClearingHouseAsync(AchDbContext context, string code, string name)
    {
        var existing = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == code);
        if (existing is not null) return existing;

        var config = new ClearingHouseConfig
        {
            ClearingHouseId = code.Equals("CENIT", StringComparison.OrdinalIgnoreCase)
                ? 9001
                : code.Equals("ACH", StringComparison.OrdinalIgnoreCase)
                    ? 9002
                    : 9003,
            HolidayStrategy = "Colombian"
        };

        context.ClearingHouseConfigs.Add(config);
        await context.SaveChangesAsync();

        var clearingHouse = new ClearingHouse
        {
            Name = name,
            Code = code,
            OriginCode = "000101006",
            ClearingHouseId = config.Id
        };

        context.ClearingHouses.Add(clearingHouse);
        await context.SaveChangesAsync();
        return clearingHouse;
    }
}
