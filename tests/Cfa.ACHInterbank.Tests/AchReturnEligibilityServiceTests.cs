using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnEligibilityServiceTests
{
    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenReasonCodeIsEmpty()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "   ", new DateTime(2026, 05, 11), true), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_REASON_REQUIRED");
        catalog.Verify(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        catalog.Verify(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldNormalizeReasonCode()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = BuildAllowCatalogMock();
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, " r01 ", new DateTime(2026, 05, 11), true), CancellationToken.None);

        Assert.True(result.IsEligible);
        Assert.Equal("R01", result.NormalizedReasonCode);
        catalog.Verify(x => x.ValidateReturnCodeAsync(77, "R01", It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldPreserveAlphanumericReasonCode()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = BuildAllowCatalogMock();
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, " dev14 ", new DateTime(2026, 05, 11), true), CancellationToken.None);

        Assert.Equal("DEV14", result.NormalizedReasonCode);
        catalog.Verify(x => x.ValidateReturnCodeAsync(77, "DEV14", It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenReturnDateBeforeOriginalEffectiveDate()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "R01", new DateTime(2026, 05, 09), true), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_DATE_BEFORE_ORIGINAL");
        catalog.Verify(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenReturnCodeCatalogRejects()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(77, "R01", It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "causal no permitida"));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "R01", new DateTime(2026, 05, 11), true), CancellationToken.None);

        Assert.False(result.IsEligible);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("RETURN_CODE_REJECTED", failure.Code);
        Assert.Equal("causal no permitida", failure.Message);
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenReturnPolicyRejectsByMaxDays()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "Supera plazo máximo de devolución."));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "R01", new DateTime(2026, 05, 11), true), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_POLICY_REJECTED" && x.Message.Contains("plazo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenReturnPolicyRejectsByState()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "Estado Pending no permitido."));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "R01", new DateTime(2026, 05, 11), true), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_POLICY_REJECTED");
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenReturnPolicyRejectsMissingAddenda()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "Addenda requerida."));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "R01", new DateTime(2026, 05, 11), false), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_POLICY_REJECTED");
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldAllow_WhenCodeAndPolicyAllow()
    {
        await using var context = BuildContext();
        SeedBase(context, 77, new DateTime(2026, 05, 10));
        var catalog = BuildAllowCatalogMock();
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new(20, "R01", new DateTime(2026, 05, 11), true), CancellationToken.None);

        Assert.True(result.IsEligible);
        Assert.Empty(result.Failures);
    }

    private static Mock<IAchRegulatoryCatalogService> BuildAllowCatalogMock()
    {
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        return catalog;
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static void SeedBase(AchDbContext c, int clearingHouseId, DateTime effectiveEntryDate)
    {
        c.AchCycles.Add(new AchCycle { Id = "C2", CycleName = "c", ProcessingDate = effectiveEntryDate, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = clearingHouseId });
        c.AchTransactions.Add(new AchTransaction { Id = 20, AchCycleId = "C2", Type = TransactionTypeEnum.Credit, State = AchTransferStateEnum.Pending, EffectiveEntryDate = effectiveEntryDate, TransactionCode = "22", TraceNumber = "123", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100m, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2" });
        c.SaveChanges();
    }
}
