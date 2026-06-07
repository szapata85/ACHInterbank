using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnIdempotencyTests
{
    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldReject_WhenSameTransactionIsSelectedTwice()
    {
        await using var ctx = BuildContext();
        SeedScenario(ctx);
        var eligibility = new Mock<IAchReturnEligibilityService>();
        var sut = new AchReturnsService(ctx, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(10, "R01"), new ReturnSelectionItemDto(10, "R01")]), CancellationToken.None));
        Assert.Contains("repetida", ex.Message, StringComparison.OrdinalIgnoreCase);
        eligibility.Verify(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenTransactionAlreadyReturned()
    {
        await using var ctx = BuildContext();
        SeedScenario(ctx, state: AchTransferStateEnum.ReturnedByOperator);
        var sut = new AchReturnEligibilityService(ctx, Mock.Of<IAchRegulatoryCatalogService>());

        var result = await sut.EvaluateOutgoingReturnAsync(new AchReturnEligibilityRequest(10, "R01", DateTime.UtcNow.Date, true), CancellationToken.None);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, f => f.Code == "RETURN_ALREADY_PROCESSED");
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldReject_WhenEligibilityReportsAlreadyProcessed()
    {
        await using var ctx = BuildContext();
        SeedScenario(ctx);
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(false, "R01", 7, "Credit", "Pending", [new AchReturnEligibilityFailure("RETURN_ALREADY_PROCESSED", "La transacción ya fue devuelta o ya tiene una devolución procesada.")]));

        var sut = new AchReturnsService(ctx, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(10, "R01")]), CancellationToken.None));
        Assert.Contains("ya fue devuelta", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldNotGeneratePartialFile_WhenDuplicateExistsInRequest()
    {
        await using var ctx = BuildContext();
        SeedScenario(ctx);
        var eligibility = new Mock<IAchReturnEligibilityService>();
        var sut = new AchReturnsService(ctx, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(10, "R01"), new ReturnSelectionItemDto(10, "R02")]), CancellationToken.None));
        Assert.Empty(ctx.Set<AchReturnGenerated>());
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldAllowSingleUniqueTransaction()
    {
        await using var ctx = BuildContext();
        SeedScenario(ctx);
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, "R01", 7, "Credit", "Pending", []));

        var sut = new AchReturnsService(ctx, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create());

        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(10, "R01")]), CancellationToken.None);
        Assert.NotNull(response);
        Assert.Single(ctx.Set<AchReturnGenerated>());
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldReject_WhenTransactionAlreadyIncludedInReturnFile()
    {
        await using var ctx = BuildContext();
        SeedScenario(ctx);
        ctx.Set<AchReturnGenerated>().Add(new AchReturnGenerated { OriginalTransactionId = 10, ReturnCycleId = "C1", ReturnReasonCode = "R01", Amount = 100, NewSequenceNumber = "123456780000001", OriginalSequenceNumber = "123456780000010", ReceiverEntityCode = "12345678", OriginatorEntityCode = "12345678", FileName = "RET_C1_20260513.RET", GeneratedAtUtc = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var catalog = new Mock<IAchRegulatoryCatalogService>();
        var sut = new AchReturnEligibilityService(ctx, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new AchReturnEligibilityRequest(10, "R01", DateTime.UtcNow.Date, true), CancellationToken.None);
        Assert.False(result.IsEligible);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_ALREADY_INCLUDED_IN_FILE");
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    static void SeedScenario(AchDbContext c, AchTransferStateEnum state = AchTransferStateEnum.Pending)
    {
        c.ClearingHouses.Add(new ClearingHouse { Id = 7, Name = "CH", Code = "CENIT", OriginCode = "000101006" });
        c.AchCycles.Add(new AchCycle { Id = "C1", CycleName = "C1", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = 7 });
        c.AchTransactions.Add(new AchTransaction { Id = 10, AchCycleId = "C1", Type = TransactionTypeEnum.Credit, State = state, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", TraceNumber = "123456780000010", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100m, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2" });
        c.SaveChanges();
    }
}
