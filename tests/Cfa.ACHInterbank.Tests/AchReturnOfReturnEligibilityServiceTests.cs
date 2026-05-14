using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnOfReturnEligibilityServiceTests
{
    [Fact]
    public async Task EvaluateAsync_ShouldReject_WhenSourceReturnDoesNotExist()
    {
        await using var context = BuildContext();
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        var sut = new AchReturnOfReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateAsync(new AchReturnOfReturnEligibilityRequest(999, "R02", DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "SOURCE_RETURN_NOT_FOUND");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReject_WhenOriginalReturnReasonIsMissing()
    {
        await using var context = BuildContext();
        SeedBase(context, returnReasonCode: string.Empty);
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        var sut = new AchReturnOfReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateAsync(new AchReturnOfReturnEligibilityRequest(20, "R02", DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "ORIGINAL_RETURN_REASON_MISSING");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReject_WhenNewReasonCodeIsMissing()
    {
        await using var context = BuildContext();
        SeedBase(context, returnReasonCode: "R01");
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        var sut = new AchReturnOfReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateAsync(new AchReturnOfReturnEligibilityRequest(20, "   ", DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "NEW_RETURN_REASON_REQUIRED");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldDelegateValidationToRegulatoryCatalog()
    {
        await using var context = BuildContext();
        SeedBase(context, returnReasonCode: "r01");
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnOfReturnAsync(77, "R01", "R02", "ReturnedByOperator", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, true));
        var sut = new AchReturnOfReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateAsync(new AchReturnOfReturnEligibilityRequest(20, " r02 ", DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsEligible);
        Assert.Equal("R01", result.OriginalReturnReasonCode);
        Assert.Equal("R02", result.NewReturnReasonCode);
        Assert.True(result.IsUniquePerTransaction);
    }


    [Fact]
    public async Task EvaluateAsync_ShouldReturnPolicyRejected_AndExposeUniquenessFlag()
    {
        await using var context = BuildContext();
        SeedBase(context, returnReasonCode: "R01");
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnOfReturnAsync(77, "R01", "R09", "ReturnedByOperator", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Rechazada", true));
        var sut = new AchReturnOfReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateAsync(new AchReturnOfReturnEligibilityRequest(20, "R09", DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.True(result.IsUniquePerTransaction);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_OF_RETURN_POLICY_REJECTED");
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static void SeedBase(AchDbContext c, string returnReasonCode)
    {
        c.AchCycles.Add(new AchCycle { Id = "C2", CycleName = "c", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = 77 });
        c.AchTransactions.Add(new AchTransaction { Id = 20, AchCycleId = "C2", Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.ReturnedByOperator, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", TraceNumber = "123", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100m, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2", ReturnReasonCode = returnReasonCode });
        c.SaveChanges();
    }
}
