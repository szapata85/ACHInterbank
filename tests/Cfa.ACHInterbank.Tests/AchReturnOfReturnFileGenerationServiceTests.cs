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
    public async Task GenerateAsync_ShouldReturnFailure_WhenFlowNotFound()
    {
        await using var context = BuildContext();
        var sut = new AchReturnOfReturnFileGenerationService(context);
        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 999 }, DateTime.UtcNow), CancellationToken.None);
        Assert.False(result.IsGenerated);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_OF_RETURN_FLOW_NOT_FOUND");
    }





    [Fact]
    public async Task GenerateAsync_ShouldReturnFailure_WhenSourceAndRorDifferentClearingHouseInSameFlow()
    {
        await using var context = BuildContext();
        SeedFlowWithDifferentClearingHouses(context, 102, 7001, 7002);
        var sut = new AchReturnOfReturnFileGenerationService(context);
        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 102 }, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Contains(result.Failures, x => x.Code == "CLEARING_HOUSE_MISSING");
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateMultipleFlows_SameClearingHouse()
    {
        await using var context = BuildContext();
        SeedFlow(context, 110, 7001);
        SeedFlow(context, 111, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 110, 111 }, new DateTime(2026, 05, 14, 12, 34, 56, DateTimeKind.Utc)), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.Equal(2, result.GeneratedFlowCount);
        Assert.Contains("FLOW|110", result.ContentText);
        Assert.Contains("FLOW|111", result.ContentText);
    }

    [Fact]
    public async Task GenerateAsync_ShouldNotCreateAchReturnGenerated_AndNotChangeTransactionStates()
    {
        await using var context = BuildContext();
        SeedFlow(context, 120, 7001);
        var beforeStates = context.AchTransactions.AsNoTracking().ToDictionary(x => x.Id, x => x.State);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 120 }, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.False(context.Set<AchReturnGenerated>().Any());
        var afterStates = context.AchTransactions.AsNoTracking().ToDictionary(x => x.Id, x => x.State);
        Assert.Equal(beforeStates, afterStates);
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static void SeedFlow(AchDbContext context, int flowId, int clearingHouseId)
    {
        var cycleId = $"C-{flowId}";
        context.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = "C", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = TimeSpan.FromHours(8), ClearingHouseId = clearingHouseId });
        var src = BuildTx(flowId * 10 + 1, cycleId, $"SRC{flowId}");
        var ror = BuildTx(flowId * 10 + 2, cycleId, $"ROR{flowId}");
        context.AchTransactions.AddRange(src, ror);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { Id = flowId, SourceReturnTransactionId = src.Id, ReturnOfReturnTransactionId = ror.Id, ReasonCode = "R02" });
        context.SaveChanges();
    }

    static void SeedFlowWithDifferentClearingHouses(AchDbContext context, int flowId, int sourceClearingHouseId, int rorClearingHouseId)
    {
        var sourceCycleId = $"SC-{flowId}";
        var rorCycleId = $"RC-{flowId}";
        context.AchCycles.Add(new AchCycle { Id = sourceCycleId, CycleName = "SC", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = TimeSpan.FromHours(8), ClearingHouseId = sourceClearingHouseId });
        context.AchCycles.Add(new AchCycle { Id = rorCycleId, CycleName = "RC", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = TimeSpan.FromHours(8), ClearingHouseId = rorClearingHouseId });
        var src = BuildTx(flowId * 10 + 1, sourceCycleId, $"SRC{flowId}");
        var ror = BuildTx(flowId * 10 + 2, rorCycleId, $"ROR{flowId}");
        context.AchTransactions.AddRange(src, ror);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { Id = flowId, SourceReturnTransactionId = src.Id, ReturnOfReturnTransactionId = ror.Id, ReasonCode = "R02" });
        context.SaveChanges();
    }

    static AchTransaction BuildTx(int id, string cycleId, string trace)
        => new()
        {
            Id = id,
            AchCycleId = cycleId,
            Type = TransactionTypeEnum.Return,
            State = AchTransferStateEnum.ReturnedByOperator,
            EffectiveEntryDate = DateTime.UtcNow.Date,
            TransactionCode = "22",
            TraceNumber = trace,
            ReceivingDFI = "12345678",
            OriginatingDFI = "12345678",
            Amount = 100m,
            Reference = "R",
            SourceAccountNumber = "1",
            DestinationAccountNumber = "2",
            ReturnReasonCode = "R01"
        };
}
