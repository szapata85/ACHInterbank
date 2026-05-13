using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class RegulatoryCatalogSeederReturnPoliciesByClearingHouseTests
{
    [Fact]
    public async Task Seeder_ShouldCreateReturnPoliciesForCenitAndAchClearingHouses()
    {
        await using var context = await CreateContextAsync();
        var cenit = await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        var ach = await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");
        await new RegulatoryCatalogSeeder(context).SeedAsync();

        Assert.True(await context.AchReturnPolicies.AnyAsync(x => x.ClearingHouseId == cenit.Id));
        Assert.True(await context.AchReturnPolicies.AnyAsync(x => x.ClearingHouseId == ach.Id));
        Assert.False(await context.AchReturnPolicies.AnyAsync(x => x.ClearingHouseId == 0));
    }

    [Fact]
    public async Task Seeder_ShouldNotMixCenitCodesIntoAchPolicies()
    {
        await using var context = await Seed();
        var cenit = await context.ClearingHouses.SingleAsync(x => x.Code == "CENIT");
        var ach = await context.ClearingHouses.SingleAsync(x => x.Code == "ACH");
        var cenitCodes = await context.AchReturnCodes.Where(x => x.ClearingHouseId == cenit.Id).Select(x => x.Code).ToHashSetAsync();
        var achCodes = await context.AchReturnCodes.Where(x => x.ClearingHouseId == ach.Id).Select(x => x.Code).ToHashSetAsync();
        var exclusiveCenit = cenitCodes.Except(achCodes).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var achPolicyCodes = (await context.AchReturnPolicies.Where(x => x.ClearingHouseId == ach.Id).ToListAsync())
            .SelectMany(p => p.AllowedReturnCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        Assert.DoesNotContain(achPolicyCodes, code => exclusiveCenit.Contains(code));
    }

    [Fact]
    public async Task Seeder_ShouldNotMixAchCodesIntoCenitPolicies()
    {
        await using var context = await Seed();
        var cenit = await context.ClearingHouses.SingleAsync(x => x.Code == "CENIT");
        var ach = await context.ClearingHouses.SingleAsync(x => x.Code == "ACH");
        var cenitCodes = await context.AchReturnCodes.Where(x => x.ClearingHouseId == cenit.Id).Select(x => x.Code).ToHashSetAsync();
        var achCodes = await context.AchReturnCodes.Where(x => x.ClearingHouseId == ach.Id).Select(x => x.Code).ToHashSetAsync();
        var exclusiveAch = achCodes.Except(cenitCodes).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cenitPolicyCodes = (await context.AchReturnPolicies.Where(x => x.ClearingHouseId == cenit.Id).ToListAsync())
            .SelectMany(p => p.AllowedReturnCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        Assert.DoesNotContain(cenitPolicyCodes, code => exclusiveAch.Contains(code));
    }

    [Fact]
    public async Task Seeder_ShouldUseOnlyCodesFromSameClearingHouseInAllowedReturnCodesCsv()
    {
        await using var context = await Seed();
        var policies = await context.AchReturnPolicies.ToListAsync();

        foreach (var policy in policies)
        {
            var codes = policy.AllowedReturnCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var code in codes)
            {
                Assert.True(await context.AchReturnCodes.AnyAsync(x => x.ClearingHouseId == policy.ClearingHouseId && x.Code == code));
            }
        }
    }

    [Fact]
    public async Task Seeder_ShouldUpsertReturnPoliciesByClearingHouseTransactionTypeDirectionAndFlowType()
    {
        await using var context = await CreateContextAsync();
        var cenit = await EnsureClearingHouseAsync(context, "CENIT", "CENIT");
        var ach = await EnsureClearingHouseAsync(context, "ACH", "ACH Colombia");
        context.AchReturnPolicies.Add(new AchReturnPolicy
        {
            ClearingHouseId = ach.Id,
            TransactionType = "Debit",
            Direction = "Any",
            FlowType = "Return",
            AllowedReturnCodesCsv = "R07",
            MaxDays = 30,
            RequiredOriginalTransactionState = "Pending",
            AllowsReturnOfReturn = true,
            RequiresAddenda = true,
            EffectiveFrom = DateTime.UtcNow.Date,
            IsActive = true
        });
        await context.SaveChangesAsync();

        await new RegulatoryCatalogSeeder(context).SeedAsync();

        var achDebit = await context.AchReturnPolicies.SingleAsync(x => x.ClearingHouseId == ach.Id && x.TransactionType == "Debit" && x.Direction == "Any" && x.FlowType == "Return");
        var cenitDebit = await context.AchReturnPolicies.SingleAsync(x => x.ClearingHouseId == cenit.Id && x.TransactionType == "Debit" && x.Direction == "Any" && x.FlowType == "Return");
        Assert.NotEqual(achDebit.ClearingHouseId, cenitDebit.ClearingHouseId);
    }

    private static async Task<AchDbContext> Seed()
    {
        var c = await CreateContextAsync();
        await EnsureClearingHouseAsync(c, "CENIT", "CENIT");
        await EnsureClearingHouseAsync(c, "ACH", "ACH Colombia");
        await new RegulatoryCatalogSeeder(c).SeedAsync();
        return c;
    }

    private static async Task<AchDbContext> CreateContextAsync()
    {
        var cn = new SqliteConnection("DataSource=:memory:");
        await cn.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(cn).EnableSensitiveDataLogging().Options;
        var c = new AchDbContext(options);
        c.Database.EnsureCreated();
        return c;
    }

    private static async Task<ClearingHouse> EnsureClearingHouseAsync(AchDbContext context, string code, string name)
    {
        var existing = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == code);
        if (existing is not null) return existing;

        var config = new ClearingHouseConfig { ClearingHouseId = code.Equals("CENIT", StringComparison.OrdinalIgnoreCase) ? 9001 : code.Equals("ACH", StringComparison.OrdinalIgnoreCase) ? 9002 : 9003, HolidayStrategy = "Colombian" };
        context.ClearingHouseConfigs.Add(config);
        await context.SaveChangesAsync();

        var ch = new ClearingHouse { Name = name, Code = code, OriginCode = "000101006", ClearingHouseId = config.Id };
        context.ClearingHouses.Add(ch);
        await context.SaveChangesAsync();
        return ch;
    }
}
