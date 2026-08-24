using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class RejectionTotalVsPartialCharacterizationTests
{
    private const string RejectedTestCause = "X99";

    [Fact]
    public async Task IngestAsync_ShouldCharacterizeRejectedTotal_AsFileLevelRejectionWithoutStateChanges()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, 10);
        var sut = new AchIncomingReturnIngestionService(c, RejectingCatalog());

        var r = await sut.IngestAsync(new("f.ach", LegacyType99ReturnRecordBuilder.Build("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);

        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedTotal, r.Decision);
        Assert.Equal(0, r.UpdatedTransactionCount);
        Assert.Equal(AchTransferStateEnum.Pending, (await c.AchTransactions.SingleAsync(x => x.Id == 10)).State);
        Assert.Empty(await c.AchTransactionStateEvents.ToListAsync());
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_CODE_REJECTED");
        Assert.False(r.IsRejectedPartial);
    }

    [Fact]
    public async Task IngestAsync_ShouldCharacterizeRejectedPartial_AsMixedAppliedAndRejectedRecords()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, 10);
        SeedTx(c, "123456780000002", 7001, 11);
        var sut = new AchIncomingReturnIngestionService(c, CatalogRejectConfiguredCause());

        var content = LegacyType99ReturnRecordBuilder.Build("R01", "123456780000001", "123456789012345") + LegacyType99ReturnRecordBuilder.Build(RejectedTestCause, "123456780000002", "123456789012346");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);

        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedPartial, r.Decision);
        Assert.Equal(1, r.UpdatedTransactionCount);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, (await c.AchTransactions.SingleAsync(x => x.Id == 10)).State);
        Assert.Equal(AchTransferStateEnum.Pending, (await c.AchTransactions.SingleAsync(x => x.Id == 11)).State);
        Assert.Single(await c.AchTransactionStateEvents.ToListAsync());
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_CODE_REJECTED");
    }

    [Fact]
    public async Task IngestAsync_ShouldKeepRejectedPartialDistinctFromAmountPartialReturn_CurrentBehavior()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, 10, amount: 125.75m);
        SeedTx(c, "123456780000002", 7001, 11, amount: 300m);
        var sut = new AchIncomingReturnIngestionService(c, CatalogRejectConfiguredCause());

        var content = LegacyType99ReturnRecordBuilder.Build("R01", "123456780000001", "123456789012345") + LegacyType99ReturnRecordBuilder.Build(RejectedTestCause, "123456780000002", "123456789012346");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);

        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedPartial, r.Decision);
        Assert.Equal(125.75m, (await c.AchTransactions.SingleAsync(x => x.Id == 10)).Amount);
        Assert.Equal(300m, (await c.AchTransactions.SingleAsync(x => x.Id == 11)).Amount);
        Assert.DoesNotContain(r.Failures, x => x.Code.Contains("AMOUNT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task IngestAsync_ShouldCharacterizeAccepted_AsAllValidRecordsApplied()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, 10);
        SeedTx(c, "123456780000002", 7001, 11);
        var sut = new AchIncomingReturnIngestionService(c, AllowAllCatalog());

        var content = LegacyType99ReturnRecordBuilder.Build("R01", "123456780000001", "123456789012345") + LegacyType99ReturnRecordBuilder.Build(RejectedTestCause, "123456780000002", "123456789012346");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);

        Assert.Equal(AchIncomingReturnIngestionDecision.Accepted, r.Decision);
        Assert.Equal(2, r.UpdatedTransactionCount);
        Assert.Equal(2, await c.AchTransactionStateEvents.CountAsync());
        Assert.DoesNotContain(r.Failures, _ => true);
    }

    [Fact]
    public async Task RejectionSemantics_ShouldDocumentCurrentMeaning_InAssertions()
    {
        Assert.Equal("RejectedTotal", AchIncomingReturnIngestionDecision.RejectedTotal);
        Assert.Equal("RejectedPartial", AchIncomingReturnIngestionDecision.RejectedPartial);
        Assert.Equal("Accepted", AchIncomingReturnIngestionDecision.Accepted);
    }

    static AchDbContext Ctx() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static void SeedTx(AchDbContext c, string trace, int chId, int txId, decimal amount = 100m)
    {
        var cfg = c.ClearingHouseConfigs.FirstOrDefault(x => x.ClearingHouseId == chId) ?? c.ClearingHouseConfigs.Add(new ClearingHouseConfig { ClearingHouseId = chId, HolidayStrategy = "Colombian" }).Entity;
        c.SaveChanges();
        var ch = c.ClearingHouses.FirstOrDefault(x => x.Code == (chId == 7001 ? "CENIT" : "ACH")) ?? c.ClearingHouses.Add(new ClearingHouse { Name = chId == 7001 ? "CENIT" : "ACH", Code = chId == 7001 ? "CENIT" : "ACH", OriginCode = "000101006", ClearingHouseId = cfg.Id }).Entity;
        c.SaveChanges();
        var cycle = new AchCycle { Id = $"C{txId}", CycleName = $"C{txId}", ProcessingDate = DateTime.UtcNow.Date, ClearingHouseId = ch.Id, CutoffTime = new TimeSpan(8,0,0), StartTime = new TimeSpan(7,0,0), EndTime = new TimeSpan(9,0,0) };
        c.AchCycles.Add(cycle);
        c.AchTransactions.Add(new AchTransaction { Id = txId, TraceNumber = trace, Amount = amount, TransactionCode = "22", Type = TransactionTypeEnum.Credit, State = AchTransferStateEnum.Pending, AchCycleId = cycle.Id, EffectiveEntryDate = DateTime.UtcNow.Date, ReceivingDFI = "1", OriginatingDFI = "1", Reference = $"r{txId}", SourceAccountNumber = "1", DestinationAccountNumber = "2" });
        c.SaveChanges();
    }

    static IAchRegulatoryCatalogService AllowAllCatalog()
    {
        var m = new Mock<IAchRegulatoryCatalogService>();
        m.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        m.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        return m.Object;
    }

    static IAchRegulatoryCatalogService RejectingCatalog()
    {
        var m = new Mock<IAchRegulatoryCatalogService>();
        m.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "blocked"));
        m.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "blocked"));
        return m.Object;
    }

    static IAchRegulatoryCatalogService CatalogRejectConfiguredCause()
    {
        var m = new Mock<IAchRegulatoryCatalogService>();
        m.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.Is<string>(s => s == RejectedTestCause), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "configured test cause blocked"));
        m.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.Is<string>(s => s != RejectedTestCause), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        m.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        return m.Object;
    }
}
