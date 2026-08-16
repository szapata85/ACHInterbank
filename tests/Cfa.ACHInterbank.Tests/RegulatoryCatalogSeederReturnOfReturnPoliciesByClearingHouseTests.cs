using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class RegulatoryCatalogSeederReturnOfReturnPoliciesByClearingHouseTests
{
    [Fact]
    public async Task Seeder_ShouldCreateReturnOfReturnPoliciesForCenitAndAchClearingHouses()
    {
        await using var c = await Seed();
        var cenit = await c.ClearingHouses.SingleAsync(x => x.Code == "CENIT");
        var ach = await c.ClearingHouses.SingleAsync(x => x.Code == "ACH");
        Assert.True(await c.AchReturnOfReturnPolicies.AnyAsync(x => x.ClearingHouseId == cenit.Id));
        Assert.True(await c.AchReturnOfReturnPolicies.AnyAsync(x => x.ClearingHouseId == ach.Id));
        Assert.False(await c.AchReturnOfReturnPolicies.AnyAsync(x => x.ClearingHouseId == 0));
    }

    [Fact]
    public async Task Seeder_ShouldCreateExactCenitR60ThroughR74CatalogAndPolicies()
    {
        await using var c = await Seed();
        var cenit = await c.ClearingHouses.SingleAsync(x => x.Code == "CENIT");
        var expected = Enumerable.Range(60, 15).Select(value => $"R{value}").ToArray();
        var codes = await c.AchReturnCodes
            .Where(x => x.ClearingHouseId == cenit.Id && x.FlowType == AchReturnFlowType.ReturnOfReturn)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Code, x.RegulatorySource })
            .ToArrayAsync();

        Assert.Equal(expected, codes.Select(x => x.Code));
        Assert.All(codes, code => Assert.Equal("CENIT Anexo A T2", code.RegulatorySource));
        var policies = await c.AchReturnOfReturnPolicies.Where(x => x.ClearingHouseId == cenit.Id).ToListAsync();
        Assert.NotEmpty(policies);
        Assert.All(policies, policy =>
        {
            Assert.True(policy.IsUniquePerTransaction);
            Assert.Equal(1, policy.MaxDays);
            Assert.Equal(expected, policy.AllowedNewReturnCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        });
    }

    [Fact]
    public async Task Seeder_ShouldUseOnlyOriginalReturnCodesFromSameClearingHouse()
    {
        await using var c = await Seed();
        foreach (var p in await c.AchReturnOfReturnPolicies.ToListAsync())
            Assert.True(await c.AchReturnCodes.AnyAsync(x => x.ClearingHouseId == p.ClearingHouseId && x.Code == p.OriginalReturnCode));
    }

    [Fact]
    public async Task Seeder_ShouldUseOnlyAllowedNewReturnCodesFromSameClearingHouse()
    {
        await using var c = await Seed();
        foreach (var p in await c.AchReturnOfReturnPolicies.ToListAsync())
        {
            var codes = p.AllowedNewReturnCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var code in codes)
                Assert.True(await c.AchReturnCodes.AnyAsync(x => x.ClearingHouseId == p.ClearingHouseId && x.Code == code));
        }
    }

    [Fact]
    public async Task Seeder_ShouldNotMixCenitCodesIntoAchReturnOfReturnPolicies()
    {
        await using var c = await Seed();
        var cenit = await c.ClearingHouses.SingleAsync(x => x.Code == "CENIT");
        var ach = await c.ClearingHouses.SingleAsync(x => x.Code == "ACH");
        var exclusive = (await c.AchReturnCodes.Where(x => x.ClearingHouseId == cenit.Id).Select(x => x.Code).ToListAsync())
            .Except(await c.AchReturnCodes.Where(x => x.ClearingHouseId == ach.Id).Select(x => x.Code).ToListAsync(), StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var achPolicies = await c.AchReturnOfReturnPolicies.Where(x => x.ClearingHouseId == ach.Id).ToListAsync();
        Assert.DoesNotContain(achPolicies, p => exclusive.Contains(p.OriginalReturnCode)
            || p.AllowedNewReturnCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(exclusive.Contains));
    }

    [Fact]
    public async Task Seeder_ShouldNotMixAchCodesIntoCenitReturnOfReturnPolicies()
    {
        await using var c = await Seed();
        var cenit = await c.ClearingHouses.SingleAsync(x => x.Code == "CENIT");
        var ach = await c.ClearingHouses.SingleAsync(x => x.Code == "ACH");
        var exclusive = (await c.AchReturnCodes.Where(x => x.ClearingHouseId == ach.Id).Select(x => x.Code).ToListAsync())
            .Except(await c.AchReturnCodes.Where(x => x.ClearingHouseId == cenit.Id).Select(x => x.Code).ToListAsync(), StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cenitPolicies = await c.AchReturnOfReturnPolicies.Where(x => x.ClearingHouseId == cenit.Id).ToListAsync();
        Assert.DoesNotContain(cenitPolicies, p => exclusive.Contains(p.OriginalReturnCode)
            || p.AllowedNewReturnCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(exclusive.Contains));
    }

    [Fact]
    public async Task Seeder_ShouldUpsertReturnOfReturnPoliciesByClearingHouseOriginalReturnCodeDirectionAndFlowType()
    {
        await using var c = await Ctx();
        var cenit = await Ch(c, "CENIT", "CENIT"); var ach = await Ch(c, "ACH", "ACH Colombia");
        c.AchReturnOfReturnPolicies.Add(new AchReturnOfReturnPolicy{ClearingHouseId = ach.Id, OriginalReturnCode="R01", AllowedNewReturnCodesCsv="R02", MaxDays=5, RequiredOriginalState="ReturnedByOperator", IsUniquePerTransaction=true, IsActive=true, Direction="Any", FlowType="ReturnOfReturn", EffectiveFrom=DateTime.UtcNow.Date});
        await c.SaveChangesAsync();
        await new RegulatoryCatalogSeeder(c).SeedAsync();

        var achRule = await c.AchReturnOfReturnPolicies.SingleAsync(x=>x.ClearingHouseId==ach.Id && x.OriginalReturnCode=="R01" && x.Direction=="Any" && x.FlowType=="ReturnOfReturn");
        var cenitRule = await c.AchReturnOfReturnPolicies.SingleAsync(x=>x.ClearingHouseId==cenit.Id && x.OriginalReturnCode=="R01" && x.Direction=="Any" && x.FlowType=="ReturnOfReturn");
        Assert.NotEqual(achRule.ClearingHouseId, cenitRule.ClearingHouseId);
    }

    static async Task<AchDbContext> Seed(){var c=await Ctx(); await Ch(c,"CENIT","CENIT"); await Ch(c,"ACH","ACH Colombia"); await new RegulatoryCatalogSeeder(c).SeedAsync(); return c;}
    static async Task<AchDbContext> Ctx(){var cn=new SqliteConnection("DataSource=:memory:"); await cn.OpenAsync(); var o=new DbContextOptionsBuilder<AchDbContext>().UseSqlite(cn).EnableSensitiveDataLogging().Options; var c=new AchDbContext(o); c.Database.EnsureCreated(); return c;}
    static async Task<ClearingHouse> Ch(AchDbContext c,string code,string name){var e=await c.ClearingHouses.FirstOrDefaultAsync(x=>x.Code==code); if(e!=null)return e; var cfg=new ClearingHouseConfig{ClearingHouseId=code.Equals("CENIT",StringComparison.OrdinalIgnoreCase)?9001:code.Equals("ACH",StringComparison.OrdinalIgnoreCase)?9002:9003,HolidayStrategy="Colombian"}; c.ClearingHouseConfigs.Add(cfg); await c.SaveChangesAsync(); var ch=new ClearingHouse{Name=name,Code=code,OriginCode="000101006",ClearingHouseId=cfg.Id}; c.ClearingHouses.Add(ch); await c.SaveChangesAsync(); return ch;}
}
