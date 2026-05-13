using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchIncomingReturnIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ShouldReject_WhenFileIsEmpty()
    {
        await using var c = Ctx();
        var sut = new AchIncomingReturnIngestionService(c);
        var r = await sut.IngestAsync(new("f.ach", "", DateTime.UtcNow), CancellationToken.None);
        Assert.False(r.IsAccepted);
        Assert.Contains(r.Failures, x => x.Code == "FILE_EMPTY");
    }

    [Fact]
    public async Task IngestAsync_ShouldParseIncomingReturnRecord()
    {
        await using var c = Ctx();
        var sut = new AchIncomingReturnIngestionService(c);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.True(r.ParsedReturnCount > 0);
        Assert.Equal("R01", r.Items.First().ReturnReasonCode);
    }

    [Fact]
    public async Task IngestAsync_ShouldLinkReturnToOriginalTransaction_ByTraceNumber()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.True(r.Items.First().IsLinked);
        Assert.Equal(10, r.Items.First().OriginalTransactionId);
    }

    [Fact]
    public async Task IngestAsync_ShouldResolveClearingHouseId_FromOriginalTransactionCycle()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7002);
        var sut = new AchIncomingReturnIngestionService(c);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("DEV14", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(7002, r.Items.First().ClearingHouseId);
    }

    [Fact]
    public async Task IngestAsync_ShouldReportFailure_WhenOriginalTransactionNotFound()
    {
        await using var c = Ctx();
        var sut = new AchIncomingReturnIngestionService(c);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "000000000000000"), DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "ORIGINAL_TRANSACTION_NOT_FOUND");
    }

    [Fact]
    public async Task IngestAsync_ShouldReportFailure_WhenReturnReasonMissing()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "RETURN_REASON_MISSING");
    }

    [Fact]
    public async Task IngestAsync_ShouldNotChangeTransactionState_InThisPhase()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var before = (await c.AchTransactions.SingleAsync()).State;
        var sut = new AchIncomingReturnIngestionService(c);
        await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        var after = (await c.AchTransactions.SingleAsync()).State;
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task IngestAsync_ShouldNotGenerateOutboundReturnFile()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c);
        await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Empty(c.Set<AchReturnGenerated>());
    }

    static string BuildType7(string reason, string originalTrace)
    {
        var chars = Enumerable.Repeat(' ', 106).ToArray();
        chars[0] = '7'; chars[1] = '9'; chars[2] = '9';
        var rr = reason.PadRight(5).Take(5).ToArray(); Array.Copy(rr,0,chars,3,5);
        var tr = originalTrace.PadLeft(15,'0').TakeLast(15).ToArray(); Array.Copy(tr,0,chars,8,15);
        return new string(chars);
    }

    static AchDbContext Ctx() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    static void SeedTx(AchDbContext c, string trace, int clearingHouseId)
    {
        c.ClearingHouses.Add(new ClearingHouse { Id = clearingHouseId, Code = clearingHouseId == 7001 ? "CENIT" : "ACH", Name = "CH", OriginCode = "000101006" });
        c.AchCycles.Add(new AchCycle { Id = "C1", CycleName = "C1", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = clearingHouseId });
        c.AchTransactions.Add(new AchTransaction { Id = 10, TraceNumber = trace, AchCycleId = "C1", Type = TransactionTypeEnum.Credit, State = AchTransferStateEnum.Pending, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2" });
        c.SaveChanges();
    }
}
