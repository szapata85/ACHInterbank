using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnConcurrencyTests
{
    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldAcquireLock_ForSelectedTransaction()
    {
        await using var ctx = BuildContext(); Seed(ctx);
        var lockSvc = new Mock<IAchReturnGenerationLockService>();
        lockSvc.Setup(x => x.AcquireAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TestReturnGenerationLockService.NoOpForMock());
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchReturnEligibilityResult(true, "R01", 7, "Credit", "Pending", []));
        var sut = new AchReturnsService(ctx, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: lockSvc.Object);

        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(10, "R01")]), CancellationToken.None);
        lockSvc.Verify(x => x.AcquireAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1 && ids.Contains(10)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldRejectDuplicateRequest_BeforeAcquiringLock()
    {
        await using var ctx = BuildContext(); Seed(ctx);
        var lockSvc = new Mock<IAchReturnGenerationLockService>();
        var sut = new AchReturnsService(ctx, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: Mock.Of<IAchReturnEligibilityService>(), returnGenerationLockService: lockSvc.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(10, "R01"), new ReturnSelectionItemDto(10, "R01")]), CancellationToken.None));
        lockSvc.Verify(x => x.AcquireAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnGenerationLockService_ShouldSerializeSameTransaction()
    {
        var svc = new AchReturnGenerationLockService();
        var first = await svc.AcquireAsync([10], CancellationToken.None);
        var enteredSecond = false;
        var secondTask = Task.Run(async () => { await using var second = await svc.AcquireAsync([10], CancellationToken.None); enteredSecond = true; });
        await Task.Delay(100);
        Assert.False(enteredSecond);
        await first.DisposeAsync();
        await secondTask;
        Assert.True(enteredSecond);
    }

    [Fact]
    public async Task ReturnGenerationLockService_ShouldAllowDifferentTransactions()
    {
        var svc = new AchReturnGenerationLockService();
        await using var a = await svc.AcquireAsync([1], CancellationToken.None);
        await using var b = await svc.AcquireAsync([2], CancellationToken.None);
        Assert.NotNull(a); Assert.NotNull(b);
    }

    [Fact]
    public async Task ReturnGenerationLockService_ShouldAcquireMultipleTransactionLocksInStableOrder()
    {
        var svc = new AchReturnGenerationLockService();
        var acquiredCount = 0;

        var t1 = Task.Run(async () =>
        {
            await using var l = await svc.AcquireAsync([3, 1, 2], CancellationToken.None);
            Interlocked.Increment(ref acquiredCount);
            await Task.Delay(50);
        });
        var t2 = Task.Run(async () =>
        {
            await using var l = await svc.AcquireAsync([2, 1, 3], CancellationToken.None);
            Interlocked.Increment(ref acquiredCount);
            await Task.Delay(50);
        });

        await Task.WhenAll(t1, t2).WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(2, acquiredCount);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldNotChangeGeneratedFile_WhenLockIsUsed()
    {
        await using var ctx = BuildContext(); Seed(ctx);
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchReturnEligibilityResult(true, "R01", 7, "Credit", "Pending", []));
        var sut = new AchReturnsService(ctx, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new AchReturnGenerationLockService());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(10, "R01")]), CancellationToken.None);
        Assert.NotNull(response);
        Assert.Single(ctx.Set<AchReturnGenerated>());
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    static void Seed(AchDbContext c){ c.ClearingHouses.Add(new ClearingHouse{Id=7,Name="CH",Code="CENIT",OriginCode="000101006"}); c.AchCycles.Add(new AchCycle{Id="C1",CycleName="C1",ProcessingDate=DateTime.UtcNow.Date,CutoffTime=new TimeSpan(8,0,0),ClearingHouseId=7}); c.AchTransactions.Add(new AchTransaction{Id=10,AchCycleId="C1",Type=TransactionTypeEnum.Credit,State=AchTransferStateEnum.Pending,EffectiveEntryDate=DateTime.UtcNow.Date,TransactionCode="22",TraceNumber="123",ReceivingDFI="12345678",OriginatingDFI="12345678",Amount=100,Reference="R",SourceAccountNumber="1",DestinationAccountNumber="2"}); c.SaveChanges(); }
}
