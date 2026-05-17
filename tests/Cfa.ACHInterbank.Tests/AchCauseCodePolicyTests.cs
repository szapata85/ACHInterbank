using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchCauseCodePolicyTests
{
    [Fact]
    public async Task AchCauseCodePolicy_ShouldAllowAchReturnCodes_ForOutboundReturn_CurrentCatalog()
    {
        await using var c = await BuildContextAsync(); var (_, ach) = await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync();
        var sut = new AchCauseCodePolicy(c);
        foreach (var code in new[] { "R07", "R10", "R29", "R31", "DEV14" })
        {
            var result = await sut.EvaluateAsync(new(code, AchCauseCodeFlow.OutboundReturn, ach.Id, "ACH"));
            Assert.True(result.IsAllowed, $"Expected code {code} to be allowed. Issues: {string.Join(" | ", result.Issues.Select(i => $"{i.Code}:{i.Message}"))}");
        }
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldAllowCenitReturnCodes_ForOutboundReturn_CurrentCatalog()
    {
        await using var c = await BuildContextAsync(); var (cenit, _) = await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync();
        var sut = new AchCauseCodePolicy(c);
        foreach (var code in new[] { "R01", "R02", "R03", "R04", "R06", "R08", "R09", "R12", "R13", "R14", "R15", "R16", "R17", "R20", "R23" })
            Assert.True((await sut.EvaluateAsync(new(code, AchCauseCodeFlow.OutboundReturn, cenit.Id, "CENIT"))).IsAllowed);
    }


    [Fact]
    public async Task AchCauseCodePolicy_ShouldAllowDxx_ForFileRejectTotalAndPartial()
    {
        await using var c = await BuildContextAsync(); await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync();
        var sut = new AchCauseCodePolicy(c);
        foreach (var code in new[] { "D01", "D02", "D03", "D04", "D05", "D06" })
        {
            var total = await sut.EvaluateAsync(new(code, AchCauseCodeFlow.FileRejectTotal));
            Assert.True(total.IsAllowed, $"Expected {code} to be allowed in FileRejectTotal");
            Assert.Equal(AchCauseCodeKind.FileRejection, total.Kind);

            var partial = await sut.EvaluateAsync(new(code, AchCauseCodeFlow.FileRejectPartial));
            Assert.True(partial.IsAllowed, $"Expected {code} to be allowed in FileRejectPartial");
            Assert.Equal(AchCauseCodeKind.FileRejection, partial.Kind);
        }
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldRejectDxx_ForReturnFlows()
    {
        await using var c = await BuildContextAsync(); await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync();
        var sut = new AchCauseCodePolicy(c);
        foreach (var flow in new[] { AchCauseCodeFlow.OutboundReturn, AchCauseCodeFlow.IncomingReturn, AchCauseCodeFlow.ReturnOfReturn })
        {
            var result = await sut.EvaluateAsync(new("D04", flow));
            Assert.False(result.IsAllowed);
            Assert.Equal(AchCauseCodeKind.FileRejection, result.Kind);
        }
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldAllowIxxx_ForOperatorResponseAndCommandCenter()
    {
        await using var c = await BuildContextAsync(); await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync();
        var sut = new AchCauseCodePolicy(c);
        foreach (var code in new[] { "I500", "I503", "ITIMEOUT", "ISOAP", "IFUNC" })
        {
            var operatorResponse = await sut.EvaluateAsync(new(code, AchCauseCodeFlow.OperatorResponse));
            Assert.True(operatorResponse.IsAllowed, $"Expected {code} to be allowed in OperatorResponse");
            Assert.Equal(AchCauseCodeKind.TechnicalIntegration, operatorResponse.Kind);

            var commandCenter = await sut.EvaluateAsync(new(code, AchCauseCodeFlow.CommandCenter));
            Assert.True(commandCenter.IsAllowed, $"Expected {code} to be allowed in CommandCenter");
            Assert.Equal(AchCauseCodeKind.TechnicalIntegration, commandCenter.Kind);
        }
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldRejectIxxx_ForReturnFlows()
    {
        await using var c = await BuildContextAsync(); await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync();
        var sut = new AchCauseCodePolicy(c);
        foreach (var flow in new[] { AchCauseCodeFlow.OutboundReturn, AchCauseCodeFlow.IncomingReturn, AchCauseCodeFlow.ReturnOfReturn })
        {
            var result = await sut.EvaluateAsync(new("I500", flow));
            Assert.False(result.IsAllowed);
            Assert.Equal(AchCauseCodeKind.TechnicalIntegration, result.Kind);
        }
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldRejectRxxDevxx_ForFileRejectFlows()
    {
        await using var c = await BuildContextAsync(); var (cenit, ach) = await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync();
        var sut = new AchCauseCodePolicy(c);

        foreach (var flow in new[] { AchCauseCodeFlow.FileRejectTotal, AchCauseCodeFlow.FileRejectPartial })
        {
            Assert.False((await sut.EvaluateAsync(new("R01", flow, cenit.Id, "CENIT"))).IsAllowed);
            Assert.False((await sut.EvaluateAsync(new("DEV14", flow, ach.Id, "ACH"))).IsAllowed);
        }
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldRejectInternalCode_ForFileRejectFlows()
    {
        await using var c = await BuildContextAsync(); await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync();
        var sut = new AchCauseCodePolicy(c);

        Assert.False((await sut.EvaluateAsync(new("DXX-LIQ", AchCauseCodeFlow.FileRejectTotal))).IsAllowed);
        Assert.False((await sut.EvaluateAsync(new("DXX-LIQ", AchCauseCodeFlow.FileRejectPartial))).IsAllowed);
        Assert.True((await sut.EvaluateAsync(new("DXX-LIQ", AchCauseCodeFlow.InternalOnly))).IsAllowed);
    }
    [Fact] public async Task AchCauseCodePolicy_ShouldRejectCenitOnlyCode_ForAchRail() { await using var c = await BuildContextAsync(); var (_, ach)=await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync(); var r=await new AchCauseCodePolicy(c).EvaluateAsync(new("R01", AchCauseCodeFlow.OutboundReturn, ach.Id, "ACH")); Assert.False(r.IsAllowed); }
    [Fact] public async Task AchCauseCodePolicy_ShouldRejectAchOnlyCode_ForCenitRail() { await using var c = await BuildContextAsync(); var (cenit,_)=await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync(); var r=await new AchCauseCodePolicy(c).EvaluateAsync(new("DEV14", AchCauseCodeFlow.OutboundReturn, cenit.Id, "CENIT")); Assert.False(r.IsAllowed); }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldClassifyDxxAsFileRejection_NotReturnReason()
    {
        await using var c = await BuildContextAsync(); await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync(); var sut = new AchCauseCodePolicy(c);
        var outbound = await sut.EvaluateAsync(new("D04", AchCauseCodeFlow.OutboundReturn)); Assert.False(outbound.IsAllowed); Assert.Equal(AchCauseCodeKind.FileRejection, outbound.Kind);
        var reject = await sut.EvaluateAsync(new("D04", AchCauseCodeFlow.FileRejectTotal)); Assert.True(reject.IsAllowed);
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldClassifyIxxxAsTechnical_NotReturnReason()
    {
        await using var c = await BuildContextAsync(); await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync(); var sut = new AchCauseCodePolicy(c);
        var outbound = await sut.EvaluateAsync(new("I500", AchCauseCodeFlow.OutboundReturn)); Assert.False(outbound.IsAllowed); Assert.Equal(AchCauseCodeKind.TechnicalIntegration, outbound.Kind);
        var op = await sut.EvaluateAsync(new("I500", AchCauseCodeFlow.OperatorResponse)); Assert.True(op.IsAllowed);
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldRejectInternalCode_ForExternalFlow()
    {
        await using var c = await BuildContextAsync(); await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync(); var sut = new AchCauseCodePolicy(c);
        Assert.False((await sut.EvaluateAsync(new("DXX-LIQ", AchCauseCodeFlow.OutboundReturn))).IsAllowed);
        Assert.True((await sut.EvaluateAsync(new("DXX-LIQ", AchCauseCodeFlow.InternalOnly))).IsAllowed);
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldAllowReturnOfReturn_WhenCurrentCatalogPolicyAllows()
    {
        await using var c = await BuildContextAsync(); var (cenit, _) = await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync(); var sut = new AchCauseCodePolicy(c);
        var r = await sut.EvaluateAsync(new("R02", AchCauseCodeFlow.ReturnOfReturn, cenit.Id, "CENIT", OriginalReasonCode: "R01"));
        Assert.True(r.IsAllowed);
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldRejectReturnOfReturn_WhenRailDoesNotAllowReason()
    {
        await using var c = await BuildContextAsync(); var (_, ach) = await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync(); var sut = new AchCauseCodePolicy(c);
        var r = await sut.EvaluateAsync(new("R02", AchCauseCodeFlow.ReturnOfReturn, ach.Id, "ACH", OriginalReasonCode: "R07"));
        Assert.False(r.IsAllowed);
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldReturnWarning_WhenNormativeStatusPending()
    {
        await using var c = await BuildContextAsync(); var (_, ach)= await SeedRails(c); await new RegulatoryCatalogSeeder(c).SeedAsync(); var sut = new AchCauseCodePolicy(c);
        var r = await sut.EvaluateAsync(new("R07", AchCauseCodeFlow.OutboundReturn, ach.Id, "ACH"));
        Assert.Contains(r.Issues, x => x.Code == "NORMATIVE_PENDING" && x.Severity == AchCauseCodePolicySeverity.Warning);
    }

    [Fact]
    public async Task AchCauseCodePolicy_ShouldNotModifyCurrentTransactionValidatorBehavior()
    {
        await using var c = await BuildContextAsync(); var (cenit,_)= await SeedRails(c); c.AchReturnCodes.Add(new AchReturnCode { ClearingHouseId = cenit.Id, Code = "R01", Description = "x", AppliesToDebit = true, IsActive = true }); await c.SaveChangesAsync();
        var validator = new TransactionValidator(c);
        var normalized = validator.NormalizeAndValidateAddenda(new Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos.AddendaDto { AddendaType = "99", BusinessType = AchAddendaBusinessType.Return, ReturnReasonCode = "R01", OriginalTraceNumber = "123456789012345", NewTraceNumber = "543210987654321" }, Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.TransactionTypeEnum.Return, false, "RETORNO");
        Assert.Equal("R01", normalized.ReturnReasonCode);
    }

    static async Task<AchDbContext> BuildContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:"); await conn.OpenAsync();
        var ctx = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(conn).Options);
        ctx.Database.EnsureCreated(); return ctx;
    }
    static async Task<(ClearingHouse cenit, ClearingHouse ach)> SeedRails(AchDbContext c)
    {
        var cc = new ClearingHouseConfig { ClearingHouseId = 7001, HolidayStrategy = "Colombian" };
        var ac = new ClearingHouseConfig { ClearingHouseId = 7002, HolidayStrategy = "Colombian" };
        c.ClearingHouseConfigs.AddRange(cc, ac); await c.SaveChangesAsync();
        var cenit = new ClearingHouse { Name = "CENIT", Code = "CENIT", OriginCode = "000101006", ClearingHouseId = cc.Id };
        var ach = new ClearingHouse { Name = "ACH", Code = "ACH", OriginCode = "000101006", ClearingHouseId = ac.Id };
        c.ClearingHouses.AddRange(cenit, ach); await c.SaveChangesAsync(); return (cenit, ach);
    }
}
