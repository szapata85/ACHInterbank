using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnsFileByClearingHouseTests
{
    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldGenerateReturnFile_ForCenitClearingHouse()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 101, "CEN-C1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [101] = new(true, "R01", 7001, "Credit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("CEN-C1", [new ReturnSelectionItemDto(101, "R01")]), CancellationToken.None);

        Assert.NotNull(response);
        var generated = await context.Set<AchReturnGenerated>().SingleAsync(x => x.OriginalTransactionId == 101);
        Assert.Equal("CEN-C1", generated.ReturnCycleId);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldGenerateReturnFile_ForAchColombiaClearingHouse()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 201, "ACH-C1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [201] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-C1", [new ReturnSelectionItemDto(201, "DEV14")]), CancellationToken.None);

        Assert.NotNull(response);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 201));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldPassCenitClearingHouseContextToEligibility()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 301, "CEN-C2");
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.Is<AchReturnEligibilityRequest>(r => r.TransactionId == 301), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, "R01", 7001, "Credit", "Pending", []));

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("CEN-C2", [new ReturnSelectionItemDto(301, "R01")]), CancellationToken.None);
        eligibility.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldPassAchClearingHouseContextToEligibility()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 302, "ACH-C2");
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.Is<AchReturnEligibilityRequest>(r => r.TransactionId == 302), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, "DEV14", 7002, "Debit", "Pending", []));

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-C2", [new ReturnSelectionItemDto(302, "DEV14")]), CancellationToken.None);
        eligibility.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldReject_WhenEligibilityRejectsCrossClearingHouseReason()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 401, "CEN-C3");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [401] = new(false, "R99", 7001, "Credit", "Pending", [new AchReturnEligibilityFailure("RETURN_CODE_REJECTED", "La causal no pertenece a la cámara de la transacción.")])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("CEN-C3", [new ReturnSelectionItemDto(401, "R99")]), CancellationToken.None));
        Assert.Contains("no pertenece a la cámara", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 401));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldNotMixEligibilityBetweenCenitAndAchInSameTestFixture()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 501, "CEN-C4");
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 502, "ACH-C4");

        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [501] = new(true, "R01", 7001, "Credit", "Pending", []),
            [502] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("CEN-C4", [new ReturnSelectionItemDto(501, "R01")]), CancellationToken.None);
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-C4", [new ReturnSelectionItemDto(502, "DEV14")]), CancellationToken.None);

        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 501));
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 502));
    }

    static Mock<IAchReturnEligibilityService> BuildEligibilityMock(IDictionary<int, AchReturnEligibilityResult> byTransaction)
    {
        var mock = new Mock<IAchReturnEligibilityService>();
        mock.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchReturnEligibilityRequest req, CancellationToken _) => byTransaction[req.TransactionId]);
        return mock;
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static void SeedScenario(AchDbContext c, int clearingHouseId, string code, string name, int transactionId, string cycleId)
    {
        if (!c.ClearingHouses.Any(x => x.Id == clearingHouseId))
        {
            c.ClearingHouses.Add(new ClearingHouse { Id = clearingHouseId, Code = code, Name = name, OriginCode = "000101006" });
        }

        if (!c.AchCycles.Any(x => x.Id == cycleId))
        {
            c.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = cycleId, ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = clearingHouseId });
        }

        c.AchTransactions.Add(new AchTransaction
        {
            Id = transactionId,
            AchCycleId = cycleId,
            Type = transactionId % 2 == 0 ? TransactionTypeEnum.Debit : TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.Pending,
            EffectiveEntryDate = DateTime.UtcNow.Date,
            TransactionCode = transactionId % 2 == 0 ? "27" : "22",
            TraceNumber = $"12345678{transactionId:0000000}",
            ReceivingDFI = "12345678",
            OriginatingDFI = "12345678",
            Amount = 100m,
            Reference = $"REF-{transactionId}",
            SourceAccountNumber = "1",
            DestinationAccountNumber = "2"
        });

        c.SaveChanges();
    }
}
