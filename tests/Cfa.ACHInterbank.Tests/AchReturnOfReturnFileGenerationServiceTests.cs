using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnOfReturnFileGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnFailure_WhenFlowIdsEmpty()
    {
        await using var context = BuildContext();
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(Array.Empty<int>(), DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_OF_RETURN_FLOW_EMPTY");
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateDeterministicFileInMemory()
    {
        await using var context = BuildContext();
        SeedFlow(context, flowId: 100, clearingHouseId: 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);
        var ts = new DateTime(2026, 05, 14, 12, 34, 56, DateTimeKind.Utc);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 100 }, ts), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.Equal("ROR_7001_20260514123456.ach", result.FileName);
        Assert.NotNull(result.ContentText);
        Assert.NotNull(result.Content);
        Assert.Equal(1, result.GeneratedFlowCount);
        Assert.Contains(100, result.FlowIds);
        Assert.Contains("FLOW|100", result.ContentText);
    }

    [Fact]
    public async Task GenerateAsync_ShouldFail_WhenMixedClearingHouse()
    {
        await using var context = BuildContext();
        SeedFlow(context, flowId: 100, clearingHouseId: 7001);
        SeedFlow(context, flowId: 101, clearingHouseId: 7002);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 100, 101 }, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Contains(result.Failures, x => x.Code == "CLEARING_HOUSE_MISSING");
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static void SeedFlow(AchDbContext context, int flowId, int clearingHouseId)
    {
        var cycleId = $"C-{flowId}";
        context.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = "C", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = TimeSpan.FromHours(8), ClearingHouseId = clearingHouseId });
        var src = new AchTransaction { Id = flowId * 10 + 1, AchCycleId = cycleId, Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.ReturnedByOperator, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", TraceNumber = $"SRC{flowId}", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100m, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2", ReturnReasonCode = "R01" };
        var ror = new AchTransaction { Id = flowId * 10 + 2, AchCycleId = cycleId, Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.ReturnedByOperator, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", TraceNumber = $"ROR{flowId}", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100m, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2", ReturnReasonCode = "R02" };
        context.AchTransactions.AddRange(src, ror);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { Id = flowId, SourceReturnTransactionId = src.Id, ReturnOfReturnTransactionId = ror.Id, ReasonCode = "R02" });
        context.SaveChanges();
    }
}
