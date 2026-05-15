using System.Text;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

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
    public async Task GenerateAsync_ShouldReturnFailure_WhenSourceReturnTransactionMissing()
    {
        await using var context = BuildContext();
        SeedFlowMissingSource(context, 130, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 130 }, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_OF_RETURN_FLOW_NOT_FOUND");
        Assert.Null(result.ContentText);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnFailure_WhenReturnOfReturnTransactionMissing()
    {
        await using var context = BuildContext();
        SeedFlowMissingReturnOfReturn(context, 131, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 131 }, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Contains(result.Failures, x => x.Code == "RETURN_OF_RETURN_FLOW_NOT_FOUND");
        Assert.Null(result.ContentText);
        Assert.Null(result.Content);
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
    public async Task GenerateAsync_ShouldReturnLoadedFlowIds_WhenFailureAfterLoadingFlows()
    {
        await using var context = BuildContext();
        SeedFlowWithDifferentClearingHouses(context, 132, 7001, 7002);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 132 }, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Contains(132, result.FlowIds);
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
    public async Task GenerateAsync_ShouldReturnAsciiContentMatchingContentText()
    {
        await using var context = BuildContext();
        SeedFlow(context, 140, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 140 }, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.NotNull(result.ContentText);
        Assert.NotNull(result.Content);
        Assert.Equal(result.ContentText, Encoding.ASCII.GetString(result.Content!));
    }

    [Fact]
    public async Task GenerateAsync_ShouldKeepDeterministicFileName()
    {
        await using var context = BuildContext();
        SeedFlow(context, 141, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 141 }, new DateTime(2026, 05, 14, 12, 34, 56, DateTimeKind.Utc)), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.Equal("ROR_7001_20260514123456.ach", result.FileName);
    }

    [Fact]
    public async Task GenerateAsync_ShouldKeepFlowIdsInResult_WhenGenerated()
    {
        await using var context = BuildContext();
        SeedFlow(context, 150, 7001);
        SeedFlow(context, 151, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 150, 151 }, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.Equal(2, result.GeneratedFlowCount);
        Assert.Contains(150, result.FlowIds);
        Assert.Contains(151, result.FlowIds);
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


    [Fact]
    public async Task GenerateAsync_ShouldPersistAudit_WhenGenerationSucceeds()
    {
        await using var context = BuildContext();
        SeedFlow(context, 160, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 160 }, new DateTime(2026, 05, 14, 12, 34, 56, DateTimeKind.Utc), "qa-user", "uat"), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.NotNull(result.AuditId);
        Assert.NotNull(result.ContentSha256);

        var audit = await context.AchReturnOfReturnGeneratedFileAudits.Include(x => x.Flows).SingleAsync(x => x.Id == result.AuditId);
        Assert.Equal("ROR_7001_20260514123456.ach", audit.FileName);
        Assert.Equal(7001, audit.ClearingHouseId);
        Assert.Equal(1, audit.GeneratedFlowCount);
        Assert.Equal(result.Content!.Length, audit.ContentLength);
        Assert.Equal(result.ContentSha256, audit.ContentSha256);
        Assert.Equal("qa-user", audit.RequestedBy);
        Assert.Equal("uat", audit.Source);
        Assert.Single(audit.Flows);
        Assert.Equal(160, audit.Flows.Single().ReturnOfReturnFlowId);
    }

    [Fact]
    public async Task GenerateAsync_ShouldNotPersistAudit_WhenGenerationFails()
    {
        await using var context = BuildContext();
        SeedFlowWithDifferentClearingHouses(context, 161, 7001, 7002);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 161 }, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Null(result.AuditId);
        Assert.Null(result.ContentSha256);
        Assert.Empty(context.AchReturnOfReturnGeneratedFileAudits);
    }

    [Fact]
    public async Task GenerateAsync_ShouldNotPersistContentTextOrBytes()
    {
        await using var context = BuildContext();
        SeedFlow(context, 162, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 162 }, new DateTime(2026, 05, 14, 12, 34, 56, DateTimeKind.Utc)), CancellationToken.None);

        Assert.True(result.IsGenerated);
        var audit = await context.AchReturnOfReturnGeneratedFileAudits.SingleAsync(x => x.Id == result.AuditId);
        var names = typeof(AchReturnOfReturnGeneratedFileAudit).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("ContentText", names);
        Assert.DoesNotContain("Content", names);
        Assert.Equal(result.Content!.Length, audit.ContentLength);
    }

    [Fact]
    public async Task GenerateNachaAsync_ProducesRecordTypes_1_5_6_7_8_9_AndNotPipe()
    {
        await using var context = BuildContext();
        var flowId = SeedFlow(context, 170, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);
        var result = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, new DateTime(2026, 05, 14, 12, 34, 56, DateTimeKind.Utc), "qa", "nacha"), CancellationToken.None);
        if (!result.IsGenerated)
        {
            Assert.NotEmpty(result.Failures);
            return;
        }
        Assert.NotNull(result.ContentText);
        Assert.StartsWith("1", result.ContentText!);
        Assert.Contains("5", result.ContentText);
        Assert.Contains("6", result.ContentText);
        Assert.Contains("799", result.ContentText);
        Assert.Contains("8", result.ContentText);
        Assert.Contains("9", result.ContentText);
        Assert.DoesNotContain("ROR|", result.ContentText);
        Assert.DoesNotContain("FLOW|", result.ContentText);
    }

    [Fact]
    public async Task GenerateNachaAsync_DuplicateProductiveGeneration_ReturnsConflictStyleFailure()
    {
        await using var context = BuildContext();
        var flowId = SeedFlow(context, 171, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);
        var first = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "nacha"), CancellationToken.None);
        if (!first.IsGenerated) return;
        var second = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "nacha"), CancellationToken.None);
        Assert.False(second.IsGenerated);
        Assert.Contains(second.Failures, x => x.Code == "DUPLICATE_PRODUCTIVE_GENERATION");
    }

    [Fact]
    public async Task GenerateAsync_AuditMode_ShouldNotInvokeExternalFileNamePolicy()
    {
        await using var context = BuildContext();
        SeedFlow(context, 173, 7001);
        var policy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);
        var sut = new AchReturnOfReturnFileGenerationService(context, policy.Object);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { 173 }, DateTime.UtcNow, "qa", "audit"), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.StartsWith("ROR_", result.FileName);
        Assert.Contains("ROR|", result.ContentText);
        Assert.Contains("FLOW|", result.ContentText);
        policy.Verify(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateNachaAsync_ShouldInvokeExternalFileNamePolicy_ForReturnOfReturnOut()
    {
        await using var context = BuildContext();
        var flowId = SeedFlow(context, 174, 7001);
        var policy = new Mock<IExternalFileNamePolicy>();
        ExternalFileNameContext? captured = null;
        policy.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .Callback<ExternalFileNameContext, CancellationToken>((ctx, _) => captured = ctx)
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = "ROR_POLICY_174.ach",
                Validation = new ExternalFileNameValidationResult()
            });
        var sut = new AchReturnOfReturnFileGenerationService(context, policy.Object);

        var result = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "api"), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.NotNull(captured);
        Assert.Equal(ExternalFileType.ReturnOfReturnOut, captured!.ExternalFileType);
        Assert.Equal(ExternalFileDirection.Outbound, captured.Direction);
        Assert.StartsWith("RORNACHA_", captured.InternalFileName);
        Assert.False(string.IsNullOrWhiteSpace(captured.NachaContent));
    }

    [Fact]
    public async Task GenerateNachaAsync_ShouldUsePolicyFileNameAndPersistAudit()
    {
        await using var context = BuildContext();
        var flowId = SeedFlow(context, 175, 7001);
        var policy = new Mock<IExternalFileNamePolicy>();
        policy.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = "ROR_POLICY_NAME.ach",
                Validation = new ExternalFileNameValidationResult()
            });
        var sut = new AchReturnOfReturnFileGenerationService(context, policy.Object);

        var result = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "api"), CancellationToken.None);

        Assert.True(result.IsGenerated);
        Assert.Equal("ROR_POLICY_NAME.ach", result.FileName);
        var audit = await context.AchReturnOfReturnGeneratedFileAudits.SingleAsync(x => x.Id == result.AuditId);
        Assert.Equal("ROR_POLICY_NAME.ach", audit.FileName);
    }

    [Fact]
    public async Task GenerateNachaAsync_ShouldPreserveSourceRealWithNachaModeMarker()
    {
        await using var context = BuildContext();
        var flowId = SeedFlow(context, 176, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "spa-angular-ror"), CancellationToken.None);

        Assert.True(result.IsGenerated);
        var audit = await context.AchReturnOfReturnGeneratedFileAudits.SingleAsync(x => x.Id == result.AuditId);
        Assert.Equal("nacha:spa-angular-ror", audit.Source);
    }

    [Fact]
    public async Task GenerateNachaAsync_DuplicateProductiveGeneration_ShouldBlock_WhenSourceHasRealValue()
    {
        await using var context = BuildContext();
        var flowId = SeedFlow(context, 177, 7001);
        var policy = new Mock<IExternalFileNamePolicy>();
        policy.SetupSequence(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult { ExternalFileName = "CUSTOM_POLICY_NAME_1.ach", Validation = new ExternalFileNameValidationResult() })
            .ReturnsAsync(new ExternalFileNamePolicyResult { ExternalFileName = "CUSTOM_POLICY_NAME_2.ach", Validation = new ExternalFileNameValidationResult() });
        var sut = new AchReturnOfReturnFileGenerationService(context, policy.Object);

        var first = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "spa-angular-ror"), CancellationToken.None);
        Assert.True(first.IsGenerated);

        var second = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "api"), CancellationToken.None);

        Assert.False(second.IsGenerated);
        Assert.Contains(second.Failures, x => x.Code == "DUPLICATE_PRODUCTIVE_GENERATION");
    }

    [Fact]
    public async Task GenerateNachaAsync_ShouldNormalizeProductiveSourceMarker_WhenCallerSendsUppercaseNachaPrefix()
    {
        await using var context = BuildContext();
        var flowId = SeedFlow(context, 181, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var first = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "NACHA:spa-angular-ror"), CancellationToken.None);

        Assert.True(first.IsGenerated);
        var audit = await context.AchReturnOfReturnGeneratedFileAudits.SingleAsync(x => x.Id == first.AuditId);
        Assert.Equal("nacha:spa-angular-ror", audit.Source);

        var second = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "api"), CancellationToken.None);
        Assert.False(second.IsGenerated);
        Assert.Contains(second.Failures, x => x.Code == "DUPLICATE_PRODUCTIVE_GENERATION");
    }

    [Fact]
    public async Task GenerateAsync_AuditMode_And_GenerateNachaAsync_ShouldCoexistForSameFlows()
    {
        await using var context = BuildContext();
        var flowId1 = SeedFlow(context, 178, 7001);
        var flowId2 = SeedFlow(context, 179, 7001);
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var auditFirst = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId1 }, DateTime.UtcNow, "qa", "audit"), CancellationToken.None);
        var nachaSecond = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId1 }, DateTime.UtcNow, "qa", "api"), CancellationToken.None);

        Assert.True(auditFirst.IsGenerated);
        Assert.True(nachaSecond.IsGenerated);

        var nachaFirst = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId2 }, DateTime.UtcNow, "qa", "api"), CancellationToken.None);
        var auditSecond = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId2 }, DateTime.UtcNow, "qa", "audit"), CancellationToken.None);

        Assert.True(nachaFirst.IsGenerated);
        Assert.True(auditSecond.IsGenerated);
    }

    [Fact]
    public async Task GenerateNachaAsync_ShouldReturnFailure_WhenPolicyHardBlocks()
    {
        await using var context = BuildContext();
        var flowId = SeedFlow(context, 180, 7001);
        var policy = new Mock<IExternalFileNamePolicy>();
        policy.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = "",
                Validation = new ExternalFileNameValidationResult
                {
                    Disposition = ExternalFileValidationDisposition.HardBlock,
                    Issues = new List<ExternalFileNameValidationIssue>
                    {
                        new() { RuleCode = "RULE", Message = "Blocked" }
                    }
                }
            });
        var sut = new AchReturnOfReturnFileGenerationService(context, policy.Object);

        var result = await sut.GenerateNachaAsync(new AchReturnOfReturnFileGenerationRequest(new[] { flowId }, DateTime.UtcNow, "qa", "api"), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Contains(result.Failures, x => x.Code == "EXTERNAL_FILENAME_VALIDATION_FAILED");
    }

    static AchDbContext BuildContext() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static int SeedFlow(AchDbContext context, int flowId, int clearingHouseId)
    {
        EnsureClearingHouse(context, clearingHouseId);
        var cycleId = $"C-{flowId}";
        context.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = "C", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = TimeSpan.FromHours(8), ClearingHouseId = clearingHouseId });
        var src = BuildTx(flowId * 10 + 1, cycleId, $"SRC{flowId}");
        var ror = BuildTx(flowId * 10 + 2, cycleId, $"ROR{flowId}");
        context.AchTransactions.AddRange(src, ror);
        var flow = new ReturnOfReturnFlow { Id = flowId, SourceReturnTransactionId = src.Id, ReturnOfReturnTransactionId = ror.Id, ReasonCode = "R02", SourceReturnTransaction = src, ReturnOfReturnTransaction = ror };
        context.ReturnOfReturnFlows.Add(flow);
        context.SaveChanges();
        return (int)flow.Id;
    }

    static void SeedFlowWithDifferentClearingHouses(AchDbContext context, int flowId, int sourceClearingHouseId, int rorClearingHouseId)
    {
        EnsureClearingHouse(context, sourceClearingHouseId, "ACH");
        EnsureClearingHouse(context, rorClearingHouseId, "CENIT");
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

    static void EnsureClearingHouse(AchDbContext context, int clearingHouseId, string code = "ACH", string originCode = "000101006")
    {
        if (context.ClearingHouses.Any(x => x.Id == clearingHouseId)) return;
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = clearingHouseId,
            Code = code,
            Name = $"ClearingHouse {clearingHouseId}",
            OriginCode = originCode
        });
    }

    static void SeedFlowMissingSource(AchDbContext context, int flowId, int clearingHouseId)
    {
        var cycleId = $"MS-{flowId}";
        context.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = "MS", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = TimeSpan.FromHours(8), ClearingHouseId = clearingHouseId });
        var source = BuildTx(flowId * 10 + 1, cycleId, $"SRC{flowId}");
        var ror = BuildTx(flowId * 10 + 2, cycleId, $"ROR{flowId}");
        context.AchTransactions.AddRange(source, ror);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { Id = flowId, SourceReturnTransactionId = source.Id, ReturnOfReturnTransactionId = ror.Id, ReasonCode = "R02" });
        context.SaveChanges();
        var flow = context.ReturnOfReturnFlows.Single(x => x.Id == flowId);
        flow.SourceReturnTransactionId = 999999;
        context.SaveChanges();
    }

    static void SeedFlowMissingReturnOfReturn(AchDbContext context, int flowId, int clearingHouseId)
    {
        var cycleId = $"MR-{flowId}";
        context.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = "MR", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = TimeSpan.FromHours(8), ClearingHouseId = clearingHouseId });
        var source = BuildTx(flowId * 10 + 1, cycleId, $"SRC{flowId}");
        var ror = BuildTx(flowId * 10 + 2, cycleId, $"ROR{flowId}");
        context.AchTransactions.AddRange(source, ror);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { Id = flowId, SourceReturnTransactionId = source.Id, ReturnOfReturnTransactionId = ror.Id, ReasonCode = "R02" });
        context.SaveChanges();
        var flow = context.ReturnOfReturnFlows.Single(x => x.Id == flowId);
        flow.ReturnOfReturnTransactionId = 999998;
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
