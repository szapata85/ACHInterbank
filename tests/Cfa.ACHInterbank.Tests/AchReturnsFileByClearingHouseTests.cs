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


    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldResolveNachaRecordConfig_ForReturnOut()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 606, "ACH-CFG-1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [606] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var provider = new Mock<INachaRecordConfigProvider>(MockBehavior.Strict);
        provider.Setup(x => x.Resolve(7002, "ACH", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound))
            .Returns(new NachaRecordConfigProvider().Resolve(7002, "ACH", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound));

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), nachaRecordConfigProvider: provider.Object);
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CFG-1", [new ReturnSelectionItemDto(606, "DEV14")]), CancellationToken.None);

        Assert.NotNull(response);
        provider.VerifyAll();
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_CurrentLayout_ShouldRemainUnchanged_WhenUsingNachaRecordConfig()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 607, "ACH-CFG-2");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [607] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var provider = new Mock<INachaRecordConfigProvider>();
        provider.Setup(x => x.Resolve(It.IsAny<int>(), It.IsAny<string?>(), NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound))
            .Returns(new NachaRecordConfigProvider().Resolve(7002, "ACH", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound));

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), nachaRecordConfigProvider: provider.Object);
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CFG-2", [new ReturnSelectionItemDto(607, "DEV14")]), CancellationToken.None);

        var content = Encoding.UTF8.GetString(response.Content);
        var records = SplitRecords(content);
        AssertRecordTypes(records);
        var r1 = records.First(r => r[0] == '1');
        var r5 = records.First(r => r[0] == '5');
        Assert.Contains("A094101", r1);
        Assert.Contains("DEVOLUCIONES", r5);
        Assert.Contains("RETORNO", r5);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_CurrentLayout_Type1_ShouldKeepRecordSizeBlockingFactorFormatCode()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 608, "ACH-CFG-3");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [608] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CFG-3", [new ReturnSelectionItemDto(608, "DEV14")]), CancellationToken.None);

        var content = Encoding.UTF8.GetString(response.Content);
        var records = SplitRecords(content);
        var r1 = records.First(r => r[0] == '1');
        Assert.Equal(106, r1.Length);
        Assert.Contains("A094101", r1);
        Assert.DoesNotContain("A094106", r1);
    }


    [Fact]
    public async Task GenerateReturnsFileAsync_CurrentLayout_Type1_ShouldKeepRecordTypeOne()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 609, "ACH-CFG-4");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [609] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CFG-4", [new ReturnSelectionItemDto(609, "DEV14")]), CancellationToken.None);

        var records = SplitRecords(Encoding.UTF8.GetString(response.Content));
        var r1 = records.First(r => r[0] == '1');
        Assert.Equal('1', r1[0]);
        Assert.Contains("A094101", r1);
        Assert.Equal(106, r1.Length);
        Assert.DoesNotContain("A094106", r1);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_Golden_NachaRecords_AchRail_CurrentLayout()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 604, "ACH-RLAY-1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [604] = new(true, "DEV14", 7002, "Debit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-RLAY-1", [new ReturnSelectionItemDto(604, "DEV14")]), CancellationToken.None);

        var content = Encoding.UTF8.GetString(response.Content);
        var records = SplitRecords(content);
        AssertRecordTypes(records);
        AssertBlockPadding(records);

        var r1 = records.First(r => r[0] == '1');
        var r5 = records.First(r => r[0] == '5');
        var r6 = records.Where(r => r[0] == '6').ToList();
        var r7 = records.Where(r => r[0] == '7').ToList();
        var r8 = records.First(r => r[0] == '8');
        var r9 = records.First(r => r[0] == '9');

        Assert.Contains("ACH-RET", r1);
        Assert.Contains("ACH Colombia", r1);
        Assert.Contains("DEVOLUCIONES", r5);
        Assert.Contains("RETORNO", r5);
        Assert.Contains("DEV14", string.Concat(r7));

        Assert.Equal(r6.Count + r7.Count, ParseInt(r8, 4, 6));
        Assert.Equal(r6.Count + r7.Count, ParseInt(r9, 13, 8));
        Assert.Equal(1, ParseInt(r9, 1, 6));
        Assert.Equal(records.Count / 10, ParseInt(r9, 7, 6));
        Assert.Equal(ComputeEntryHashFromType6(r6), ParseLong(r8, 10, 10));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_Golden_NachaRecords_CenitRail_CurrentLayout()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7001, "CENIT", "CENIT", 605, "CEN-RLAY-1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult>
        {
            [605] = new(true, "R01", 7001, "Credit", "Pending", [])
        });

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService());
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("CEN-RLAY-1", [new ReturnSelectionItemDto(605, "R01")]), CancellationToken.None);

        var content = Encoding.UTF8.GetString(response.Content);
        var records = SplitRecords(content);
        AssertRecordTypes(records);
        AssertBlockPadding(records);

        var r5 = records.First(r => r[0] == '5');
        Assert.Contains("DEVOLUCIONES", r5);
        Assert.Contains("RETORNO", r5);
        Assert.Contains("R01", string.Concat(records.Where(r => r[0] == '7')));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldEvaluateCausePolicy_ForOutboundReturn()
    {
        await using var context = BuildContext();
        SeedScenario(context, 7002, "ACH", "ACH Colombia", 701, "ACH-CP-1");
        var eligibility = BuildEligibilityMock(new Dictionary<int, AchReturnEligibilityResult> { [701] = new(true, "DEV14", 7002, "Debit", "Pending", []) });
        var causePolicy = new Mock<IAchCauseCodePolicy>(MockBehavior.Strict);
        causePolicy.Setup(x => x.EvaluateAsync(It.Is<AchCauseCodePolicyRequest>(r =>
                r.Code == "DEV14" &&
                r.Flow == AchCauseCodeFlow.OutboundReturn &&
                r.ClearingHouseId == 7002 &&
                r.ClearingHouseCode == "ACH" &&
                r.Source == "GenerateReturnsFileAsync"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCauseCodePolicyResult(true, AchCauseCodeRail.AchColombia, AchCauseCodeKind.ReturnReason, true, [new("NORMATIVE_PENDING", "pending", AchCauseCodePolicySeverity.Warning)]));

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), causeCodePolicy: causePolicy.Object);
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CP-1", [new ReturnSelectionItemDto(701, "DEV14")]), CancellationToken.None);

        Assert.NotNull(response.Content);
        Assert.True(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 701));
        causePolicy.VerifyAll();
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

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), causeCodePolicy: causePolicy.Object);
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

        var sut = new AchReturnsService(context, regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), causeCodePolicy: causePolicy.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CP-3", [new ReturnSelectionItemDto(703, reason)]), CancellationToken.None));
        Assert.False(await context.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == 703));
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
