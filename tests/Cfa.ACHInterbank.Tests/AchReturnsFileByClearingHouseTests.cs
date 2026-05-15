using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
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

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldUseExternalFileNamePolicy_ForReturnOut()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 601, "ACH-C5");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [601] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var policy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);
        policy.Setup(x => x.GenerateExternalNameAsync(
                It.Is<ExternalFileNameContext>(c =>
                    c.ExternalFileType == ExternalFileType.ReturnOut
                    && c.Direction == ExternalFileDirection.Outbound
                    && c.ClearingHouseId == 7002
                    && c.ClearingHouseCode == "ACH"
                    && c.InternalFileName != null
                    && c.InternalFileName.StartsWith("RET_ACH-C5_")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = "RET_PROVISIONAL_ACH-C5.RET",
                Validation = new ExternalFileNameValidationResult
                {
                    Disposition = ExternalFileValidationDisposition.Warning,
                    Issues = [new ExternalFileNameValidationIssue { RuleCode = "RETURN_NAMING_PROVISIONAL", Message = "warning", Disposition = ExternalFileValidationDisposition.Warning }]
                }
            });

        var sut = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService(),
            externalFileNamePolicy: policy.Object);

        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-C5", [new ReturnSelectionItemDto(601, "DEV14")]), CancellationToken.None);

        Assert.Equal("RET_PROVISIONAL_ACH-C5.RET", response.FileName);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 601 && x.FileName == "RET_PROVISIONAL_ACH-C5.RET"));
        policy.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_Golden_ReturnOut_UsesPolicyName_AndPersistsFileName()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 602, "ACH-GOLD-1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [602] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var policy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);
        policy.Setup(x => x.GenerateExternalNameAsync(
                It.Is<ExternalFileNameContext>(c =>
                    c.ExternalFileType == ExternalFileType.ReturnOut
                    && c.Direction == ExternalFileDirection.Outbound
                    && c.InternalFileName != null
                    && c.InternalFileName.StartsWith("RET_ACH-GOLD-1_")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = "RETURN_POLICY_GOLDEN.RET",
                Validation = new ExternalFileNameValidationResult
                {
                    Disposition = ExternalFileValidationDisposition.Warning,
                    Issues = [new ExternalFileNameValidationIssue { RuleCode = "RETURN_NAMING_PROVISIONAL", Message = "warning", Disposition = ExternalFileValidationDisposition.Warning }]
                }
            });

        var sut = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService(),
            externalFileNamePolicy: policy.Object);

        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-GOLD-1", [new ReturnSelectionItemDto(602, "DEV14")]), CancellationToken.None);

        Assert.Equal("RETURN_POLICY_GOLDEN.RET", response.FileName);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 602 && x.FileName == "RETURN_POLICY_GOLDEN.RET"));
        policy.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_Golden_ReturnOut_FallsBackToRetProvisional_WhenPolicyReturnsEmpty()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 603, "ACH-GOLD-2");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [603] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var policy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);
        policy.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = "",
                Validation = new ExternalFileNameValidationResult
                {
                    Disposition = ExternalFileValidationDisposition.Warning,
                    Issues = [new ExternalFileNameValidationIssue { RuleCode = "RETURN_NAMING_PROVISIONAL", Message = "warning", Disposition = ExternalFileValidationDisposition.Warning }]
                }
            });

        var sut = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService(),
            externalFileNamePolicy: policy.Object);

        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-GOLD-2", [new ReturnSelectionItemDto(603, "DEV14")]), CancellationToken.None);

        Assert.StartsWith("RET_ACH-GOLD-2_", response.FileName);
        Assert.EndsWith(".RET", response.FileName);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 603 && x.FileName == response.FileName));
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
