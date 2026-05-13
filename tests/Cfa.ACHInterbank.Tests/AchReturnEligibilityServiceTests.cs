using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnEligibilityServiceTests
{
    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenTransactionDoesNotExist()
    {
        await using var context = BuildContext();
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(9999, "R01", DateTime.UtcNow, true), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "TRANSACTION_NOT_FOUND");
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenClearingHouseIsMissing()
    {
        await using var context = BuildContext();
        SeedBase(context, 0);
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "R01", DateTime.UtcNow, true), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "CLEARING_HOUSE_MISSING");
    }

[Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldUseClearingHouseId_FromAchCycle()
    {
        await using var context = BuildContext();
        SeedBase(context, 77);
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(77, "R01", It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(77, It.IsAny<TransactionTypeEnum>(), "R01", It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        await sut.EvaluateOutgoingReturnAsync(new(20, "r01", DateTime.UtcNow, true), CancellationToken.None);

        catalog.VerifyAll();
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenRegulatoryCatalogRejectsReason()
    {
        await using var context = BuildContext();
        SeedBase(context, 77);
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(77, "R01", It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "rechazo"));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(77, It.IsAny<TransactionTypeEnum>(), "R01", It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "R01", DateTime.UtcNow, true), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_CODE_REJECTED");
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldAllow_WhenRegulatoryCatalogAllowsReason()
    {
        await using var context = BuildContext();
        SeedBase(context, 77);
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(77, "R01", It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(77, It.IsAny<TransactionTypeEnum>(), "R01", It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, " r01 ", DateTime.UtcNow, true), CancellationToken.None);

        Assert.True(result.IsEligible);
        Assert.Equal("R01", result.NormalizedReasonCode);
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static void SeedBase(AchDbContext c, int clearingHouseId)
    {
        c.AchCycles.Add(new AchCycle { Id = "C2", CycleName = "c", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8,0,0), ClearingHouseId = clearingHouseId });
        c.AchTransactions.Add(new AchTransaction { Id = 20, AchCycleId = "C2", Type = TransactionTypeEnum.Credit, State = AchTransferStateEnum.Pending, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", TraceNumber = "123", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100m, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2" });
        c.SaveChanges();
    }
}
