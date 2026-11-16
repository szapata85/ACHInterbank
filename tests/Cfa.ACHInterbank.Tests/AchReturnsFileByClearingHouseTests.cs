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
using System.Text;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnsFileByClearingHouseTests
{
    private const int RecordLength = 106;

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldFail_WhenReturnOutPolicyIsMissing()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 100, "ACH-NOPOL");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [100] = new(true, "R01", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-NOPOL", [new ReturnSelectionItemDto(100, "R01")]), CancellationToken.None));

        Assert.Contains("RETURN_FILENAME_POLICY_REQUIRED", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 100));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldRequireCenitPolicy_ForCenitClearingHouse()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 101, "CEN-C1");
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        var lockService = new Mock<IAchReturnGenerationLockService>(MockBehavior.Strict);
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: lockService.Object, externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("CEN-C1", [new ReturnSelectionItemDto(101, "R01")]), CancellationToken.None));

        Assert.Contains("CENIT_RETURN_POLICY_REQUIRED", ex.Message, StringComparison.Ordinal);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 101));
        Assert.False(await context.AchTransactionStateEvents.AnyAsync(x => x.AchTransactionId == 101));
        Assert.Equal(AchTransferStateEnum.Pending, await context.AchTransactions.Where(x => x.Id == 101).Select(x => x.State).SingleAsync());
        eligibility.Verify(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        lockService.Verify(x => x.AcquireAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldGenerateReturnFile_ForAchColombiaClearingHouse()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 201, "ACH-C1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [201] = new(true, "R01", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-C1", [new ReturnSelectionItemDto(201, "R01")]), CancellationToken.None);

        Assert.NotNull(response);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 201));
    }

    [Fact]
    public async Task GeneratedReturnArtifact_CanBeRebuiltByteForByte_ForDispatchRetry()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 202, "ACH-C1-REBUILD");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [202] = new(true, "R01", 7002, "Debit", "Pending", [])
        });
        var builder = ReturnOutNachaFileBuilderFactory.Create();
        var sut = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService(),
            externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(),
            nachaFileBuilder: builder);
        var generated = await sut.GenerateReturnsFileAsync(
            new GenerateReturnsFileRequest("ACH-C1-REBUILD", [new ReturnSelectionItemDto(202, "R01")]),
            CancellationToken.None);
        var identifierMap = new Mock<INachaFileIdentifierMapService>();
        identifierMap.Setup(x => x.ResolveIdentifierAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync('A');
        var rebuilder = new AchOutboundReturnArtifactService(context, builder, identifierMap.Object);

        var rebuilt = await rebuilder.BuildAsync(generated.FileName);

        Assert.Equal(generated.Content, rebuilt.Content);
        Assert.Equal(generated.TotalRecords, rebuilt.RecordCount);
        Assert.Equal(generated.TotalReturns, rebuilt.ReturnCount);
        Assert.Equal([202], rebuilt.TransactionIds);
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldUseCenitClearingHouseContext()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 301, "CEN-C2");
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        catalog.Setup(x => x.ValidateReturnCodeAsync(7001, "R01", TransactionTypeEnum.Credit, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(7001, TransactionTypeEnum.Credit, "R01", It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, "Pending", It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new AchReturnEligibilityRequest(301, "R01", DateTime.UtcNow.Date, true), CancellationToken.None);

        Assert.True(result.IsEligible);
        Assert.Equal(7001, result.ClearingHouseId);
        Assert.Equal("Credit", result.TransactionType);
        catalog.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldPassAchClearingHouseContextToEligibility()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 302, "ACH-C2");
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.Is<AchReturnEligibilityRequest>(r => r.TransactionId == 302), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, "R01", 7002, "Debit", "Pending", []));

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-C2", [new ReturnSelectionItemDto(302, "R01")]), CancellationToken.None);
        eligibility.VerifyAll();
    }

    [Fact]
    public async Task EvaluateOutgoingReturnAsync_ShouldRejectCrossClearingHouseReason_ForCenitContext()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 401, "CEN-C3");
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        catalog.Setup(x => x.ValidateReturnCodeAsync(7001, "R99", TransactionTypeEnum.Credit, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "La causal no pertenece a la cámara de la transacción."));
        var sut = new AchReturnEligibilityService(context, catalog.Object);

        var result = await sut.EvaluateOutgoingReturnAsync(new AchReturnEligibilityRequest(401, "R99", DateTime.UtcNow.Date, true), CancellationToken.None);

        Assert.False(result.IsEligible);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("RETURN_CODE_REJECTED", failure.Code);
        Assert.Contains("no pertenece a la cámara", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(7001, result.ClearingHouseId);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 401));
        catalog.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldNotMixEligibilityBetweenCenitAndAchInSameTestFixture()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 501, "CEN-C4");
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 502, "ACH-C4");

        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [502] = new(true, "R01", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
        var cenitEx = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("CEN-C4", [new ReturnSelectionItemDto(501, "R01")]), CancellationToken.None));
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-C4", [new ReturnSelectionItemDto(502, "R01")]), CancellationToken.None);

        Assert.Contains("CENIT_RETURN_POLICY_REQUIRED", cenitEx.Message, StringComparison.Ordinal);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 501));
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 502));
        Assert.Equal(AchTransferStateEnum.Pending, await context.AchTransactions.Where(x => x.Id == 501).Select(x => x.State).SingleAsync());
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, await context.AchTransactions.Where(x => x.Id == 502).Select(x => x.State).SingleAsync());
        Assert.False(await context.AchTransactionStateEvents.AnyAsync(x => x.AchTransactionId == 501));
        Assert.True(await context.AchTransactionStateEvents.AnyAsync(x => x.AchTransactionId == 502));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldUseExternalFileNamePolicy_ForReturnOut()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 601, "ACH-C5");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [601] = new(true, "R01", 7002, "Debit", "Pending", [])
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
                ExternalFileName = "0101006.001.1",
                Components = new ExternalFileNameComponents { FullName = "0101006.001.1", FileIdModifier = 'A' },
                Validation = new ExternalFileNameValidationResult
                {
                    Disposition = ExternalFileValidationDisposition.Passed
                }
            });

        var builder = ReturnOutNachaFileBuilderFactory.Create();
        var sut = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService(),
            externalFileNamePolicy: policy.Object,
            nachaFileBuilder: builder);

        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-C5", [new ReturnSelectionItemDto(601, "R01")]), CancellationToken.None);

        Assert.Equal("0101006.001.1", response.FileName);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 601 && x.FileName == "0101006.001.1"));
        policy.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_Golden_ReturnOut_UsesPolicyName_AndPersistsFileName()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 602, "ACH-GOLD-1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [602] = new(true, "R01", 7002, "Debit", "Pending", [])
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
                ExternalFileName = "0101006.002.1",
                Components = new ExternalFileNameComponents { FullName = "0101006.002.1", FileIdModifier = 'B' },
                Validation = new ExternalFileNameValidationResult
                {
                    Disposition = ExternalFileValidationDisposition.Passed
                }
            });

        var builder = ReturnOutNachaFileBuilderFactory.Create();
        var sut = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService(),
            externalFileNamePolicy: policy.Object,
            nachaFileBuilder: builder);

        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-GOLD-1", [new ReturnSelectionItemDto(602, "R01")]), CancellationToken.None);

        Assert.Equal("0101006.002.1", response.FileName);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 602 && x.FileName == "0101006.002.1"));
        var finalRequest = Mock.Get(builder).Invocations
            .Where(x => x.Method.Name == nameof(INachaFileBuilder.BuildReturnOutAsync))
            .Select(x => (NachaReturnOutBuildRequest)x.Arguments[0])
            .Last();
        Assert.Equal("B", finalRequest.FileIdModifier);
        policy.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_Golden_ReturnOut_ShouldFail_WhenPolicyReturnsEmpty()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 603, "ACH-GOLD-2");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [603] = new(true, "R01", 7002, "Debit", "Pending", [])
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
            externalFileNamePolicy: policy.Object,
            nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-GOLD-2", [new ReturnSelectionItemDto(603, "R01")]), CancellationToken.None));

        Assert.Contains("RETURN_FILENAME_POLICY_REQUIRED", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 603));
    }


    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldDelegateReturnOutRenderingToOptionCBuilder()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 606, "ACH-CFG-1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [606] = new(true, "R01", 7002, "Debit", "Pending", [])
        });

        var builder = ReturnOutNachaFileBuilderFactory.Create();
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: builder);
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CFG-1", [new ReturnSelectionItemDto(606, "R01")]), CancellationToken.None);

        Assert.NotNull(response);
        Mock.Get(builder).Verify(x => x.BuildReturnOutAsync(It.IsAny<NachaReturnOutBuildRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_OptionC_ShouldIgnoreLegacyNachaRecordConfig()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 607, "ACH-CFG-2");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [607] = new(true, "R01", 7002, "Debit", "Pending", [])
        });

        var builder = ReturnOutNachaFileBuilderFactory.Create();
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: builder);
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CFG-2", [new ReturnSelectionItemDto(607, "R01")]), CancellationToken.None);

        var content = Encoding.UTF8.GetString(response.Content);
        var records = SplitRecords(content);
        AssertRecordTypes(records);
        Mock.Get(builder).Verify(x => x.BuildReturnOutAsync(It.IsAny<NachaReturnOutBuildRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_OptionC_Type1_ShouldKeep106Characters()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 608, "ACH-CFG-3");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [608] = new(true, "R01", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CFG-3", [new ReturnSelectionItemDto(608, "R01")]), CancellationToken.None);

        var content = Encoding.UTF8.GetString(response.Content);
        var records = SplitRecords(content);
        var r1 = records.First(r => r[0] == '1');
        Assert.Equal(106, r1.Length);
    }


    [Fact]
    public async Task GenerateReturnsFileAsync_OptionC_Type1_ShouldKeepRecordTypeOne()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 609, "ACH-CFG-4");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [609] = new(true, "R01", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CFG-4", [new ReturnSelectionItemDto(609, "R01")]), CancellationToken.None);

        var records = SplitRecords(Encoding.UTF8.GetString(response.Content));
        var r1 = records.First(r => r[0] == '1');
        Assert.Equal('1', r1[0]);
        Assert.Equal(106, r1.Length);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_OptionC_ShouldPreserveRecordStructureAndPadding()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 604, "ACH-RLAY-1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [604] = new(true, "R01", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-RLAY-1", [new ReturnSelectionItemDto(604, "R01")]), CancellationToken.None);

        var content = Encoding.UTF8.GetString(response.Content);
        var records = SplitRecords(content);
        AssertRecordTypes(records);
        AssertBlockPadding(records);

        var r6 = records.Where(r => r[0] == '6').ToList();
        var r7 = records.Where(r => r[0] == '7').ToList();
        Assert.Single(r6);
        Assert.Single(r7);
        Assert.All(records, record => Assert.Equal(106, record.Length));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_CenitRail_ShouldRequireNormativePolicy()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 605, "CEN-RLAY-1");
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("CEN-RLAY-1", [new ReturnSelectionItemDto(605, "R01")]), CancellationToken.None));

        Assert.Contains("CENIT_RETURN_POLICY_REQUIRED", ex.Message, StringComparison.Ordinal);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 605));
        Assert.False(await context.AchTransactionStateEvents.AnyAsync(x => x.AchTransactionId == 605));
        Assert.Equal(AchTransferStateEnum.Pending, await context.AchTransactions.Where(x => x.Id == 605).Select(x => x.State).SingleAsync());
        eligibility.Verify(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldRejectDev14BeforeReturnOutPersistenceOrGeneration()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 701, "ACH-CP-1");
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        var causePolicy = new Mock<IAchCauseCodePolicy>(MockBehavior.Strict);
        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), causeCodePolicy: causePolicy.Object, nachaFileBuilder: builder.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CP-1", [new ReturnSelectionItemDto(701, "DEV14")]), CancellationToken.None));

        Assert.Contains("ACHCOL_DEV14_CLAIMS_ONLY", ex.Message, StringComparison.Ordinal);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 701));
        Assert.False(await context.AchTransactionStateEvents.AnyAsync(x => x.AchTransactionId == 701));
        eligibility.Verify(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        causePolicy.Verify(x => x.EvaluateAsync(It.IsAny<AchCauseCodePolicyRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        builder.Verify(x => x.BuildReturnOutAsync(It.IsAny<NachaReturnOutBuildRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldAcceptR10WhenNoPrenotificationExistsWithinConfiguredWindow()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 710, "ACH-R10-1");
        SetR10OriginalCycle(context, "ACH-R10-1", "Ciclo 1", new DateTime(2026, 11, 18));
        SeedScheduledAchCycle(context, 7002, "ACH-R10-1-TARGET", new DateTime(2026, 11, 18), 9);
        (await context.AchCycles.SingleAsync(row => row.Id == "ACH-R10-1-TARGET")).CycleName = "Ciclo 2";
        await context.SaveChangesAsync();
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [710] = new(true, "R10", 7002, "Debit", "Pending", [])
        });
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-R10-1", [new ReturnSelectionItemDto(710, "R10")], ReturnCycleId: "ACH-R10-1-TARGET"), CancellationToken.None);

        Assert.NotEmpty(response.Content);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(row => row.OriginalTransactionId == 710));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldAcceptR10WithExplicitNoAuthorizationBasisAndAuditIt()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 712, "ACH-R10-2");
        SetR10OriginalCycle(context, "ACH-R10-2", "Ciclo 1", new DateTime(2026, 11, 18));
        SeedScheduledAchCycle(context, 7002, "ACH-R10-2-TARGET", new DateTime(2026, 11, 18), 9);
        (await context.AchCycles.SingleAsync(row => row.Id == "ACH-R10-2-TARGET")).CycleName = "Ciclo 2";
        await context.SaveChangesAsync();
        var original = await context.AchTransactions.SingleAsync(transaction => transaction.Id == 712);
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 713,
            AchCycleId = original.AchCycleId,
            Type = TransactionTypeEnum.Prenotification,
            IsPrenotification = true,
            TransactionCode = "28",
            DestinationInstitutionId = original.DestinationInstitutionId,
            DestinationAccountNumber = original.DestinationAccountNumber,
            EffectiveEntryDate = original.EffectiveEntryDate,
            Reference = "PRENOTE-713",
            SourceAccountNumber = "1"
        });
        await context.SaveChangesAsync();
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [712] = new(true, "R10", 7002, "Debit", "Pending", [])
        });
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-R10-2", [new ReturnSelectionItemDto(712, "R10", AchColombiaR10Basis: AchColombiaR10ReturnBasis.NoReceiverAuthorizationOrAgreement)], ReturnCycleId: "ACH-R10-2-TARGET"), CancellationToken.None);

        Assert.Contains("NoReceiverAuthorizationOrAgreement", await context.AchTransactionStateEvents.Where(eventRow => eventRow.AchTransactionId == 712).Select(eventRow => eventRow.PayloadJson).SingleAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldFailClosedForR10WhenPrenotificationExistsAndNoBasisIsProvided()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 714, "ACH-R10-3");
        SetR10OriginalCycle(context, "ACH-R10-3", "Ciclo 1", new DateTime(2026, 11, 18));
        SeedScheduledAchCycle(context, 7002, "ACH-R10-3-TARGET", new DateTime(2026, 11, 18), 9);
        (await context.AchCycles.SingleAsync(row => row.Id == "ACH-R10-3-TARGET")).CycleName = "Ciclo 2";
        await context.SaveChangesAsync();
        var original = await context.AchTransactions.SingleAsync(transaction => transaction.Id == 714);
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 715,
            AchCycleId = original.AchCycleId,
            Type = TransactionTypeEnum.Prenotification,
            IsPrenotification = true,
            TransactionCode = "28",
            DestinationInstitutionId = original.DestinationInstitutionId,
            DestinationAccountNumber = original.DestinationAccountNumber,
            EffectiveEntryDate = original.EffectiveEntryDate,
            Reference = "PRENOTE-715",
            SourceAccountNumber = "1"
        });
        await context.SaveChangesAsync();
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [714] = new(true, "R10", 7002, "Debit", "Pending", [])
        });
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-R10-3", [new ReturnSelectionItemDto(714, "R10")], ReturnCycleId: "ACH-R10-3-TARGET"), CancellationToken.None));

        Assert.Contains("ACHCOL_R10_BASIS_UNPROVEN", ex.Message, StringComparison.Ordinal);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(row => row.OriginalTransactionId == 714));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldAllowR10AtConfiguredFourCycleBoundary()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 716, "ACH-R10-W0");
        var day = new DateTime(2026, 11, 18);
        SetR10OriginalCycle(context, "ACH-R10-W0", "Ciclo 1", day);
        for (var n = 1; n <= 3; n++)
        {
            SeedScheduledAchCycle(context, 7002, $"ACH-R10-W{n}", day, n + 1);
            (await context.AchCycles.SingleAsync(cycle => cycle.Id == $"ACH-R10-W{n}")).CycleName = $"Ciclo {n + 1}";
        }
        await context.SaveChangesAsync();
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [716] = new(true, "R10", 7002, "Debit", "Pending", [])
        });
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-R10-W0", [new ReturnSelectionItemDto(716, "R10", AchColombiaR10Basis: AchColombiaR10ReturnBasis.NoReceiverAuthorizationOrAgreement)], ReturnCycleId: "ACH-R10-W3"), CancellationToken.None);

        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(row => row.OriginalTransactionId == 716));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldRejectR10AfterConfiguredFourCycleBoundaryEvenWhenSevenCyclesExist()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 718, "ACH-R10-X0");
        var day = new DateTime(2026, 11, 18);
        SetR10OriginalCycle(context, "ACH-R10-X0", "Ciclo 1", day);
        for (var hour = 1; hour <= 6; hour++)
        {
            SeedScheduledAchCycle(context, 7002, $"ACH-R10-X{hour}", day, hour);
            (await context.AchCycles.SingleAsync(cycle => cycle.Id == $"ACH-R10-X{hour}")).CycleName = $"Ciclo {hour + 1}";
        }
        await context.SaveChangesAsync();
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [718] = new(true, "R10", 7002, "Debit", "Pending", [])
        });
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-R10-X0", [new ReturnSelectionItemDto(718, "R10")], ReturnCycleId: "ACH-R10-X4"), CancellationToken.None));

        Assert.Contains("ACHCOL_R10_RETURN_WINDOW_EXPIRED", ex.Message, StringComparison.Ordinal);
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(row => row.OriginalTransactionId == 718));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldPermitR10ForPreviousOperationalDayAcrossWeekendAndHoliday()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 720, "ACH-R10-D0");
        var original = await context.AchCycles.SingleAsync(cycle => cycle.Id == "ACH-R10-D0");
        original.CycleName = "Ciclo 7";
        original.ProcessingDate = new DateTime(2026, 11, 13); // Friday
        var transaction = await context.AchTransactions.SingleAsync(x => x.Id == 720);
        transaction.EffectiveEntryDate = original.ProcessingDate;
        SeedScheduledAchCycle(context, 7002, "ACH-R10-D1", new DateTime(2026, 11, 17), 7);
        (await context.AchCycles.SingleAsync(cycle => cycle.Id == "ACH-R10-D1")).CycleName = "Ciclo 1";
        await context.SaveChangesAsync();
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [720] = new(true, "R10", 7002, "Debit", "Pending", [])
        });
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-R10-D0", [new ReturnSelectionItemDto(720, "R10", AchColombiaR10Basis: AchColombiaR10ReturnBasis.NoReceiverAuthorizationOrAgreement)], ReturnCycleId: "ACH-R10-D1"), CancellationToken.None);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(row => row.OriginalTransactionId == 720));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldUseThePolicyEffectiveOnTheOriginalOperationalDay()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 722, "ACH-R10-H0");
        var historicalDay = new DateTime(2026, 9, 29); // five-cycle historical policy
        var originalCycle = await context.AchCycles.SingleAsync(cycle => cycle.Id == "ACH-R10-H0");
        originalCycle.ProcessingDate = historicalDay;
        var originalTransaction = await context.AchTransactions.SingleAsync(transaction => transaction.Id == 722);
        originalTransaction.EffectiveEntryDate = historicalDay;
        var currentPolicy = await context.AchReturnPolicies.SingleAsync(policy => policy.ClearingHouseId == 7002 && policy.TransactionType == "Debit");
        context.AchReturnPolicies.Add(new AchReturnPolicy
        {
            ClearingHouseId = 7002,
            TransactionType = "Debit",
            Direction = AchReturnDirection.Any,
            FlowType = AchReturnFlowType.Return,
            MaxCycles = 2,
            EffectiveFrom = historicalDay,
            EffectiveTo = historicalDay,
            IsActive = true
        });
        currentPolicy.EffectiveFrom = historicalDay.AddDays(1);
        await context.SaveChangesAsync();
        SeedScheduledAchCycle(context, 7002, "ACH-R10-H1", historicalDay, 1);
        SeedScheduledAchCycle(context, 7002, "ACH-R10-H2", historicalDay, 2);
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [722] = new(true, "R10", 7002, "Debit", "Pending", [])
        });
        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-R10-H0", [new ReturnSelectionItemDto(722, "R10")], ReturnCycleId: "ACH-R10-H2"), CancellationToken.None));

        Assert.Contains("ACHCOL_R10_RETURN_WINDOW_EXPIRED", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldBlock_WhenCausePolicyRejectsRailFlow()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 702, "ACH-CP-2");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult> { [702] = new(true, "R01", 7002, "Debit", "Pending", []) });
        var causePolicy = new Mock<IAchCauseCodePolicy>();
        causePolicy.Setup(x => x.EvaluateAsync(It.IsAny<AchCauseCodePolicyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCauseCodePolicyResult(false, AchCauseCodeRail.AchColombia, AchCauseCodeKind.ReturnReason, true, [new("RAIL_MISMATCH_OR_NOT_CONFIGURED", "mismatch", AchCauseCodePolicySeverity.Error)]));

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), causeCodePolicy: causePolicy.Object, nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CP-2", [new ReturnSelectionItemDto(702, "R01")]), CancellationToken.None));
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 702));
    }

    [Theory]
    [InlineData("D04")]
    [InlineData("I500")]
    [InlineData("DXX-LIQ")]
    public async Task GenerateReturnsFileAsync_ShouldBlock_WhenNonReturnCodeIsUsedAsReturnReason(string reason)
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 703, "ACH-CP-3");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult> { [703] = new(true, reason, 7002, "Debit", "Pending", []) });
        var causePolicy = new Mock<IAchCauseCodePolicy>();
        causePolicy.Setup(x => x.EvaluateAsync(It.IsAny<AchCauseCodePolicyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCauseCodePolicyResult(false, AchCauseCodeRail.AchColombia, AchCauseCodeKind.Unknown, true, [new("FLOW_MISMATCH", "invalid", AchCauseCodePolicySeverity.Error)]));

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), causeCodePolicy: causePolicy.Object, nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CP-3", [new ReturnSelectionItemDto(703, reason)]), CancellationToken.None));
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 703));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    public async Task R10_Hotfix02_SevenCyclesRespectOperationalChronology(int targetNumber, bool expected)
    {
        await using var context = BuildContext();
        const int transactionId = 880;
        SeedScenario(context, 7002, "ACH", "ACH Colombia", transactionId, "ACH-HF02-C1");
        var c1 = await context.AchCycles.SingleAsync(cycle => cycle.Id == "ACH-HF02-C1");
        c1.CycleName = "Ciclo 1";
        c1.ProcessingDate = new DateTime(2026, 11, 18);
        (await context.AchTransactions.SingleAsync(row => row.Id == transactionId)).EffectiveEntryDate = c1.ProcessingDate;
        c1.StartTime = new TimeSpan(19, 1, 0);
        c1.CutoffTime = TimeSpan.FromHours(7);
        c1.ClearingHouseCycleConfig!.StartTime = c1.StartTime;
        c1.ClearingHouseCycleConfig.CutoffTime = c1.CutoffTime;
        await context.SaveChangesAsync();
        for (var n = 2; n <= 7; n++)
        {
            SeedScheduledAchCycle(context, 7002, $"ACH-HF02-C{n}", c1.ProcessingDate, 2 * n + 4);
            var cycle = await context.AchCycles.SingleAsync(row => row.Id == $"ACH-HF02-C{n}");
            cycle.CycleName = $"Ciclo {n}";
        }
        await context.SaveChangesAsync();
        var original = await context.AchTransactions.Include(row => row.AchCycle)
            .ThenInclude(cycle => cycle!.ClearingHouseCycleConfig)
            .SingleAsync(row => row.Id == transactionId);
        var returnCycle = await context.AchCycles.Include(row => row.ClearingHouseCycleConfig)
            .SingleAsync(row => row.Id == $"ACH-HF02-C{targetNumber}");
        var policy = new AchReturnEligibilityPolicyComponent(context,
            Mock.Of<IAchReturnEligibilityService>(), null, new OperationalCalendarService(context));
        var scope = new AchReturnGenerationScope(c1, returnCycle, null, true, false);
        var item = new ReturnSelectionItemDto(transactionId, "R10",
            AchColombiaR10Basis: AchColombiaR10ReturnBasis.NoReceiverAuthorizationOrAgreement);
        var action = () => policy.ValidatePreEligibilityAsync(original, scope, item, CancellationToken.None);
        if (expected)
        {
            await action();
        }
        else
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(action);
            Assert.Contains("ACHCOL_R10_RETURN_WINDOW_EXPIRED", error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void R10_Hotfix02_CycleOneStartsPreviousCalendarDateEvenAcrossMidnight()
    {
        var operationalDate = new DateTime(2026, 11, 18); // miércoles hábil
        var c1 = new AchCycle {
            Id = "C1", CycleName = "Ciclo 1", ProcessingDate = operationalDate,
            StartTime = new TimeSpan(19, 1, 0), CutoffTime = TimeSpan.FromHours(7)
        };
        var c2 = new AchCycle {
            Id = "C2", CycleName = "Ciclo 2", ProcessingDate = operationalDate,
            StartTime = new TimeSpan(7, 1, 0), CutoffTime = TimeSpan.FromHours(9)
        };
        Assert.Equal(new DateTime(2026, 11, 17, 19, 1, 0),
            AchReturnEligibilityPolicyComponent.ResolveOperationalCycleStart(c1));
        Assert.Equal(new DateTime(2026, 11, 18, 7, 1, 0),
            AchReturnEligibilityPolicyComponent.ResolveOperationalCycleStart(c2));
        Assert.True(AchReturnEligibilityPolicyComponent.ResolveOperationalCycleStart(c1)
            < AchReturnEligibilityPolicyComponent.ResolveOperationalCycleStart(c2));
        c1.CycleName = "C1";
        Assert.Equal(new DateTime(2026, 11, 17, 19, 1, 0),
            AchReturnEligibilityPolicyComponent.ResolveOperationalCycleStart(c1));
    }

    // Independent copy of the published matrix, not derived from production
    // helpers. Covers all 49 same-day and all 49 previous-day cycle pairs.
    public static IEnumerable<object[]> SevenCycleNormativeMatrix()
    {
        var previous = new[] { "4567", "567", "67", "7", "", "", "" };
        var current = new[] { "", "1", "12", "123", "1234", "2345", "3456" };
        for (var target = 1; target <= 7; target++)
        for (var original = 1; original <= 7; original++)
        {
            yield return new object[] { original, target, RegulatoryCycleSourceDay.PreviousOperationalDay,
                previous[target - 1].Contains(original.ToString()) };
            yield return new object[] { original, target, RegulatoryCycleSourceDay.CurrentOperationalDay,
                current[target - 1].Contains(original.ToString()) };
        }
    }

    [Theory]
    [MemberData(nameof(SevenCycleNormativeMatrix))]
    public void R10_Final_CatalogExactlyMatchesAllPublishedPairs(
        int origin, int destination, RegulatoryCycleSourceDay sourceDay, bool expected)
    {
        Assert.Equal(expected, RegulatoryCycleScheduleCatalog.AllowsAchColombiaSevenCycleReturn(
            origin, destination, sourceDay));
    }

    [Theory]
    [InlineData("2026-11-13", 7, "2026-11-17", 1, true)] // holiday Monday 16 Nov + weekend
    [InlineData("2026-11-13", 3, "2026-11-17", 1, false)]
    [InlineData("2026-11-18", 1, "2026-11-18", 2, true)]
    [InlineData("2026-11-18", 1, "2026-11-18", 1, false)]
    [InlineData("2026-11-17", 7, "2026-11-19", 1, false)] // intervening working day
    [InlineData("2026-11-14", 7, "2026-11-17", 1, false)] // Saturday source
    [InlineData("2026-11-16", 7, "2026-11-17", 1, false)] // national holiday source
    [InlineData("2026-11-13", 7, "2026-11-15", 1, false)] // Sunday destination
    public async Task R10_Final_PolicyEvaluatesActualOperationalDaysAndPairs(
        string originDay, int originalNumber, string destinationDay, int returnNumber, bool expected)
    {
        await using var context = BuildContext();
        const int transactionId = 992;
        SeedScenario(context, 7002, "ACH", "ACH Colombia", transactionId, "R10-FINAL-ORIGIN");
        var originalDate = DateTime.ParseExact(originDay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var targetDate = DateTime.ParseExact(destinationDay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var originalCycle = await context.AchCycles.SingleAsync(row => row.Id == "R10-FINAL-ORIGIN");
        originalCycle.CycleName = $"Ciclo {originalNumber}";
        originalCycle.ProcessingDate = originalDate;
        originalCycle.StartTime = originalNumber == 1 ? new TimeSpan(19, 1, 0) : TimeSpan.FromHours(16);
        originalCycle.CutoffTime = originalNumber == 1 ? TimeSpan.FromHours(7) : TimeSpan.FromHours(18);
        var transaction = await context.AchTransactions.SingleAsync(row => row.Id == transactionId);
        transaction.EffectiveEntryDate = originalDate;
        SeedScheduledAchCycle(context, 7002, "R10-FINAL-RETURN", targetDate, 8);
        var returnCycle = await context.AchCycles.SingleAsync(row => row.Id == "R10-FINAL-RETURN");
        returnCycle.CycleName = $"Ciclo {returnNumber}";
        if (returnNumber == 1)
        {
            returnCycle.StartTime = new TimeSpan(19, 1, 0);
            returnCycle.CutoffTime = TimeSpan.FromHours(7);
        }
        await context.SaveChangesAsync();
        var attached = await context.AchTransactions.Include(row => row.AchCycle)
            .ThenInclude(row => row!.ClearingHouseCycleConfig).SingleAsync(row => row.Id == transactionId);
        var scope = new AchReturnGenerationScope(originalCycle, returnCycle, null, true, false);
        var policy = new AchReturnEligibilityPolicyComponent(context,
            Mock.Of<IAchReturnEligibilityService>(), null, new OperationalCalendarService(context));
        var item = new ReturnSelectionItemDto(transactionId, "R10",
            AchColombiaR10Basis: AchColombiaR10ReturnBasis.NoReceiverAuthorizationOrAgreement);

        var action = () => policy.ValidatePreEligibilityAsync(attached, scope, item, CancellationToken.None);
        if (expected) await action();
        else await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    static List<string> SplitRecords(string content)
    {
        var normalized = (content ?? string.Empty).Replace("\r", "");
        var lines = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (lines.Count > 1) return lines;
        return Enumerable.Range(0, normalized.Length / RecordLength)
            .Select(i => normalized.Substring(i * RecordLength, RecordLength))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
    static void AssertRecordTypes(IReadOnlyCollection<string> records)
    {
        Assert.Contains(records, r => r.StartsWith('1'));
        Assert.Contains(records, r => r.StartsWith('5'));
        Assert.Contains(records, r => r.StartsWith('6'));
        Assert.Contains(records, r => r.StartsWith('7'));
        Assert.Contains(records, r => r.StartsWith('8'));
        Assert.Contains(records, r => r.StartsWith('9'));
        Assert.All(records, r => Assert.Equal(RecordLength, r.Length));
    }
    static void AssertBlockPadding(IReadOnlyList<string> records)
    {
        Assert.Equal(0, records.Count % 10);
        var firstControl = -1;
        for (var i = 0; i < records.Count; i++) { if (records[i].StartsWith('9')) { firstControl = i; break; } }
        if (firstControl < 0) return;
        for (var i = firstControl + 1; i < records.Count; i++) Assert.True(records[i].All(c => c == '9'));
    }
    static int ParseInt(string record, int start, int len) => int.Parse(record.Substring(start, len));
    static long ParseLong(string record, int start, int len) => long.Parse(record.Substring(start, len));
    static long ComputeEntryHashFromType6(IEnumerable<string> type6) => type6.Sum(r => long.Parse(r.Substring(3, 8))) % 10_000_000_000L;

    static Mock<IAchReturnEligibilityService> BuildEligibilityMock(IDictionary<int, AchReturnEligibilityResult> byTransaction)
    {
        var mock = new Mock<IAchReturnEligibilityService>();
        mock.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchReturnEligibilityRequest req, CancellationToken _) => byTransaction[req.TransactionId]);
        return mock;
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static void SetR10OriginalCycle(AchDbContext context, string cycleId, string cycleName, DateTime operationalDate)
    {
        var cycle = context.AchCycles.Single(row => row.Id == cycleId);
        cycle.CycleName = cycleName;
        cycle.ProcessingDate = operationalDate;
        if (cycleName == "Ciclo 1")
        {
            cycle.StartTime = new TimeSpan(19, 1, 0);
            cycle.CutoffTime = TimeSpan.FromHours(7);
        }
        foreach (var transaction in context.AchTransactions.Where(row => row.AchCycleId == cycleId))
        {
            transaction.EffectiveEntryDate = operationalDate;
        }
        context.SaveChanges();
    }

    static void SeedScheduledAchCycle(AchDbContext context, int clearingHouseId, string cycleId, DateTime processingDate, int hour, bool allowsReturn = true)
    {
        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            CycleName = cycleId,
            ClearingHouseId = clearingHouseId,
            ProcessingDate = processingDate.Date,
            StartTime = new TimeSpan(hour, 0, 0),
            EndTime = new TimeSpan(hour, 30, 0),
            CutoffTime = new TimeSpan(hour, 15, 0),
            OutputReleaseTime = new TimeSpan(hour, 30, 0),
            ClearingHouseCycleConfig = new ClearingHouseCycleConfig
            {
                ClearingHouseId = clearingHouseId,
                PolicyVersion = "TEST",
                CycleName = cycleId,
                StartTime = new TimeSpan(hour, 0, 0),
                EndTime = new TimeSpan(hour, 30, 0),
                CutoffTime = new TimeSpan(hour, 15, 0),
                OutputReleaseTime = new TimeSpan(hour, 30, 0),
                AllowsReturn = allowsReturn,
                EffectiveFrom = processingDate.Date
            }
        });
        context.SaveChanges();
    }

    static void SeedScenario(AchDbContext c, int clearingHouseId, string code, string name, int transactionId, string cycleId)
    {
        if (!c.ClearingHouses.Any(x => x.Id == clearingHouseId))
        {
            c.ClearingHouses.Add(new ClearingHouse { Id = clearingHouseId, Code = code, Name = name, OriginCode = "000101006" });
        }

        if (string.Equals(code, "ACH", StringComparison.OrdinalIgnoreCase)
            && !c.AchReturnPolicies.Any(policy => policy.ClearingHouseId == clearingHouseId
                && policy.TransactionType == "Debit"
                && policy.FlowType == AchReturnFlowType.Return))
        {
            c.AchReturnPolicies.Add(new AchReturnPolicy
            {
                ClearingHouseId = clearingHouseId,
                TransactionType = "Debit",
                Direction = AchReturnDirection.Any,
                FlowType = AchReturnFlowType.Return,
                MaxCycles = 4,
                EffectiveFrom = DateTime.UtcNow.Date,
                IsActive = true
            });
        }

        if (!c.AchCycles.Any(x => x.Id == cycleId))
        {
            var cyclePolicy = new ClearingHouseCycleConfig
            {
                ClearingHouseId = clearingHouseId,
                PolicyVersion = "TEST",
                CycleName = cycleId,
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(8, 0, 0),
                OutputReleaseTime = new TimeSpan(8, 30, 0),
                AllowsReturn = true,
                EffectiveFrom = DateTime.UtcNow.Date
            };
            c.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = cycleId, ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = clearingHouseId, ClearingHouseCycleConfig = cyclePolicy });
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
            DestinationAccountNumber = "2",
            DestinationInstitutionId = 1
        });

        c.SaveChanges();
    }
}
