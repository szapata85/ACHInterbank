using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchRegulatoryCatalogClearingHouseTests
{
    [Fact]
    public async Task ValidateReturnCode_ShouldAllowCode_ForMatchingClearingHouse()
    {
        await using var c = await Ctx(); var cenit = await Ch(c,"CENIT");
        c.AchReturnCodes.Add(new AchReturnCode { ClearingHouseId = cenit.Id, Code = "R01", Description = "x", FlowType = "Any", AppliesToDebit = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1) });
        await c.SaveChangesAsync();
        var sut = new AchRegulatoryCatalogService(c);
        var r = await sut.ValidateReturnCodeAsync(cenit.Id,"R01",TransactionTypeEnum.Debit,DateTime.UtcNow.Date,DateTime.UtcNow.Date,CancellationToken.None);
        Assert.True(r.IsAllowed);
    }

    [Fact]
    public async Task ValidateReturnCode_ShouldRejectCode_ForDifferentClearingHouse()
    {
        await using var c = await Ctx(); var cenit=await Ch(c,"CENIT"); var ach=await Ch(c,"ACH");
        c.AchReturnCodes.Add(new AchReturnCode { ClearingHouseId = cenit.Id, Code = "R01", Description = "x", FlowType = "Any", AppliesToDebit = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1) });
        await c.SaveChangesAsync();
        var sut = new AchRegulatoryCatalogService(c);
        var r = await sut.ValidateReturnCodeAsync(ach.Id,"R01",TransactionTypeEnum.Debit,DateTime.UtcNow.Date,DateTime.UtcNow.Date,CancellationToken.None);
        Assert.False(r.IsAllowed);
    }

    [Fact]
    public async Task ValidateReturnPolicy_ShouldUseClearingHouseSpecificPolicy()
    {
        await using var c = await Ctx(); var cenit=await Ch(c,"CENIT"); var ach=await Ch(c,"ACH");
        c.AchReturnPolicies.AddRange(
            new AchReturnPolicy { ClearingHouseId = cenit.Id, TransactionType="Debit", Direction="Any", FlowType="Return", AllowedReturnCodesCsv="R01", MaxDays=5, RequiredOriginalTransactionState="Pending", IsActive=true, EffectiveFrom=DateTime.UtcNow.Date.AddDays(-1) },
            new AchReturnPolicy { ClearingHouseId = ach.Id, TransactionType="Debit", Direction="Any", FlowType="Return", AllowedReturnCodesCsv="R02", MaxDays=5, RequiredOriginalTransactionState="Pending", IsActive=true, EffectiveFrom=DateTime.UtcNow.Date.AddDays(-1) });
        await c.SaveChangesAsync();
        var sut=new AchRegulatoryCatalogService(c);
        Assert.True((await sut.ValidateReturnPolicyAsync(cenit.Id,TransactionTypeEnum.Debit,"R01",DateTime.UtcNow.Date,DateTime.UtcNow.Date,true,"Pending",CancellationToken.None)).IsAllowed);
        Assert.False((await sut.ValidateReturnPolicyAsync(cenit.Id,TransactionTypeEnum.Debit,"R02",DateTime.UtcNow.Date,DateTime.UtcNow.Date,true,"Pending",CancellationToken.None)).IsAllowed);
    }

    [Fact]
    public async Task ValidateReturnPolicy_ShouldRespectEffectiveDates()
    {
        await using var c = await Ctx(); var cenit=await Ch(c,"CENIT");
        c.AchReturnPolicies.Add(new AchReturnPolicy { ClearingHouseId = cenit.Id, TransactionType="Debit", Direction="Any", FlowType="Return", AllowedReturnCodesCsv="R01", MaxDays=5, RequiredOriginalTransactionState="Pending", IsActive=true, EffectiveFrom=DateTime.UtcNow.Date.AddDays(2) });
        await c.SaveChangesAsync();
        var sut=new AchRegulatoryCatalogService(c);
        var r=await sut.ValidateReturnPolicyAsync(cenit.Id,TransactionTypeEnum.Debit,"R01",DateTime.UtcNow.Date,DateTime.UtcNow.Date,true,"Pending",CancellationToken.None);
        Assert.False(r.IsAllowed);
    }

    [Fact]
    public async Task ValidateReturnOfReturnPolicy_ShouldUseClearingHouseSpecificPolicy()
    {
        await using var c = await Ctx(); var cenit=await Ch(c,"CENIT"); var ach=await Ch(c,"ACH");
        c.AchReturnOfReturnPolicies.AddRange(
            new AchReturnOfReturnPolicy{ClearingHouseId=cenit.Id,OriginalReturnCode="R01",AllowedNewReturnCodesCsv="R02",RequiredOriginalState="ReturnedByOperator",Direction="Any",FlowType="ReturnOfReturn",MaxDays=5,IsActive=true,EffectiveFrom=DateTime.UtcNow.Date.AddDays(-1)},
            new AchReturnOfReturnPolicy{ClearingHouseId=ach.Id,OriginalReturnCode="R01",AllowedNewReturnCodesCsv="R03",RequiredOriginalState="ReturnedByOperator",Direction="Any",FlowType="ReturnOfReturn",MaxDays=5,IsActive=true,EffectiveFrom=DateTime.UtcNow.Date.AddDays(-1)});
        await c.SaveChangesAsync();
        var sut=new AchRegulatoryCatalogService(c);
        Assert.True((await sut.ValidateReturnOfReturnAsync(cenit.Id,"R01","R02","ReturnedByOperator",DateTime.UtcNow.Date,DateTime.UtcNow.Date,CancellationToken.None)).IsAllowed);
        Assert.False((await sut.ValidateReturnOfReturnAsync(cenit.Id,"R01","R03","ReturnedByOperator",DateTime.UtcNow.Date,DateTime.UtcNow.Date,CancellationToken.None)).IsAllowed);
    }

    [Fact]
    public async Task GetReturnCodes_ShouldFilterByClearingHouse_AndKeepUnfilteredCompatibility()
    {
        await using var c = await Ctx();
        var cenit = await Ch(c, "CENIT");
        var achColombia = await Ch(c, "ACHCOL");
        c.AchReturnCodes.AddRange(
            new AchReturnCode { ClearingHouseId = cenit.Id, Code = "R01", Description = "CENIT", FlowType = "Any", IsActive = true },
            new AchReturnCode { ClearingHouseId = achColombia.Id, Code = "R01", Description = "ACH Colombia", FlowType = "Any", IsActive = true });
        await c.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(c);

        var filtered = await sut.GetReturnCodesAsync(cenit.Id, CancellationToken.None);
        var unfiltered = await sut.GetReturnCodesAsync(CancellationToken.None);

        var code = Assert.Single(filtered);
        Assert.Equal(cenit.Id, code.ClearingHouseId);
        Assert.Equal(2, unfiltered.Count(x => x.Code == "R01"));
    }

    [Fact]
    public async Task GetReturnCodesEndpoint_ShouldForwardOptionalClearingHouseFilter()
    {
        const int clearingHouseId = 77;
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        catalog
            .Setup(x => x.GetReturnCodesAsync(clearingHouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AchReturnCode>());
        var controller = new RegulatoryCatalogsController(catalog.Object);

        var action = await controller.GetReturnCodes(clearingHouseId, null, CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(action);
        catalog.Verify(x => x.GetReturnCodesAsync(clearingHouseId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReturnCodesEndpoint_ShouldPreferClearingHouseCode()
    {
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        catalog
            .Setup(x => x.GetReturnCodesByClearingHouseCodeAsync("CENIT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AchReturnCode>());
        var controller = new RegulatoryCatalogsController(catalog.Object);

        var action = await controller.GetReturnCodes(null, "CENIT", CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(action);
        catalog.Verify(x => x.GetReturnCodesByClearingHouseCodeAsync("CENIT", It.IsAny<CancellationToken>()), Times.Once);
    }

    static async Task<AchDbContext> Ctx(){var cn=new SqliteConnection("DataSource=:memory:");await cn.OpenAsync();var o=new DbContextOptionsBuilder<AchDbContext>().UseSqlite(cn).Options;var c=new AchDbContext(o);c.Database.EnsureCreated();return c;}
    static async Task<ClearingHouse> Ch(AchDbContext c,string code){var e=await c.ClearingHouses.FirstOrDefaultAsync(x=>x.Code==code);if(e!=null)return e;var cfg=new ClearingHouseConfig{ClearingHouseId=9000+Math.Abs(code.GetHashCode()%1000),HolidayStrategy="Colombian"};c.ClearingHouseConfigs.Add(cfg);await c.SaveChangesAsync();var ch=new ClearingHouse{Name=code,Code=code,OriginCode="000101006",ClearingHouseId=cfg.Id};c.ClearingHouses.Add(ch);await c.SaveChangesAsync();return ch;}
}
