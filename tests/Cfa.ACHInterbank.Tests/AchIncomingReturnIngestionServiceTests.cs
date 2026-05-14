using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchIncomingReturnIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ShouldReject_WhenFileIsEmpty()
    {
        await using var c = Ctx();
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", "", DateTime.UtcNow), CancellationToken.None);
        Assert.False(r.IsAccepted);
        Assert.Contains(r.Failures, x => x.Code == "FILE_EMPTY");
    }

    [Fact]
    public async Task IngestAsync_ShouldParseIncomingReturnRecord()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.True(r.ParsedReturnCount > 0);
        Assert.Equal("R01", r.Items.First().ReturnReasonCode);
    }

    [Fact]
    public async Task IngestAsync_ShouldLinkReturnToOriginalTransaction_ByTraceNumber()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.True(r.Items.First().IsLinked);
        Assert.Equal(10, r.Items.First().OriginalTransactionId);
    }

    [Fact]
    public async Task IngestAsync_ShouldResolveClearingHouseId_FromOriginalTransactionCycle()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7002);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("DEV14", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(7002, r.Items.First().ClearingHouseId);
    }

    [Fact]
    public async Task IngestAsync_ShouldReportFailure_WhenOriginalTransactionNotFound()
    {
        await using var c = Ctx();
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "000000000000000"), DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "ORIGINAL_TRANSACTION_NOT_FOUND");
        catalog.Verify(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        catalog.Verify(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_ShouldReportFailure_WhenReturnReasonMissing()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "RETURN_REASON_MISSING");
    }

    [Fact]
    public async Task IngestAsync_ShouldValidateIncomingReturnCode_ByClearingHouse()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var catalog = CatalogAllowAllMock();
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        catalog.Verify(x => x.ValidateReturnCodeAsync(7001, "R01", TransactionTypeEnum.Credit, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ShouldNormalizeIncomingReturnReasonCode()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var catalog = CatalogAllowAllMock();
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        await sut.IngestAsync(new("f.ach", BuildType7("r01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        catalog.Verify(x => x.ValidateReturnCodeAsync(7001, "R01", TransactionTypeEnum.Credit, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ShouldPreserveIncomingAlphanumericReturnReasonCode()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var catalog = CatalogAllowAllMock();
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        await sut.IngestAsync(new("f.ach", BuildType7("dev14", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        catalog.Verify(x => x.ValidateReturnCodeAsync(7001, "DEV14", TransactionTypeEnum.Credit, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ShouldReject_WhenIncomingReturnCodeDoesNotBelongToClearingHouse()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(7001, "R01", TransactionTypeEnum.Credit, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "Causal no permitida"));
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.False(r.IsAccepted);
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_CODE_REJECTED");
    }

    [Fact]
    public async Task IngestAsync_ShouldValidateIncomingReturnPolicy_ByClearingHouse()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7002);
        var now = new DateTime(2026, 05, 14, 12, 0, 0, DateTimeKind.Utc);
        var catalog = CatalogAllowAllMock();
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), now), CancellationToken.None);
        catalog.Verify(x => x.ValidateReturnPolicyAsync(7002, TransactionTypeEnum.Credit, "R01", now.Date, now.Date, true, AchTransferStateEnum.Pending.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ShouldReject_WhenIncomingReturnPolicyRejectsByMaxDays()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "Supera plazo máximo"));
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_POLICY_REJECTED");
    }

    [Fact]
    public async Task IngestAsync_ShouldReject_WhenIncomingReturnPolicyRejectsByState()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "Estado no permitido"));
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_POLICY_REJECTED");
    }

    [Fact]
    public async Task IngestAsync_ShouldReject_WhenIncomingReturnPolicyRejectsMissingAddenda()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        catalog.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "Addenda requerida"));
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_POLICY_REJECTED");
    }

    [Fact]
    public async Task IngestAsync_ShouldNotChangeTransactionState_WhenRegulatoryValidationPasses()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var before = (await c.AchTransactions.SingleAsync()).State;
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        var after = (await c.AchTransactions.SingleAsync()).State;
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task IngestAsync_ShouldNotGenerateOutboundReturnFile_WhenRegulatoryValidationPasses()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Empty(c.Set<AchReturnGenerated>());
    }

    [Fact]
    public async Task IngestAsync_ShouldReportDuplicate_WhenSameOriginalTransactionAndReasonAppearsTwiceInFile()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "123456780000001");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.False(r.IsAccepted);
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_DUPLICATE_IN_FILE");
    }

    [Fact]
    public async Task IngestAsync_ShouldNotReportDuplicate_WhenSameOriginalTransactionHasDifferentReason()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("R01", "123456780000001") + BuildType7("DEV14", "123456780000001");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.DoesNotContain(r.Failures, x => x.Code == "INCOMING_RETURN_DUPLICATE_IN_FILE");
    }

    [Fact]
    public async Task IngestAsync_ShouldNotReportDuplicate_WhenDifferentOriginalTransactionsHaveSameReason()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        SeedTx(c, "123456780000002", 7001, txId: 11, cycleId: "C2");
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "123456780000002");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.DoesNotContain(r.Failures, x => x.Code == "INCOMING_RETURN_DUPLICATE_IN_FILE");
    }

    [Fact]
    public async Task IngestAsync_ShouldDetectDuplicateUsingOriginalTrace_WhenOriginalTransactionNotFound()
    {
        await using var c = Ctx();
        var catalog = new Mock<IAchRegulatoryCatalogService>(MockBehavior.Strict);
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        var content = BuildType7("R01", "000000000000000") + BuildType7(" R01 ", "000000000000000");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "ORIGINAL_TRANSACTION_NOT_FOUND");
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_DUPLICATE_IN_FILE");
        catalog.Verify(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_ShouldNormalizeReasonBeforeDuplicateDetection()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("r01", "123456780000001") + BuildType7(" R01 ", "123456780000001");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_DUPLICATE_IN_FILE");
    }

    [Fact]
    public async Task IngestAsync_ShouldPreserveDev14ForDuplicateDetection()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("dev14", "123456780000001") + BuildType7("DEV14", "123456780000001");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Failures, x => x.Code == "INCOMING_RETURN_DUPLICATE_IN_FILE");
        Assert.Contains(r.Items, x => x.ReturnReasonCode == "DEV14");
    }

    [Fact]
    public async Task IngestAsync_ShouldNotChangeTransactionState_WhenDuplicateDetected()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var before = (await c.AchTransactions.SingleAsync()).State;
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "123456780000001");
        await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        var after = (await c.AchTransactions.SingleAsync()).State;
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task IngestAsync_ShouldNotGenerateOutboundReturnFile_WhenDuplicateDetected()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "123456780000001");
        await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Empty(c.Set<AchReturnGenerated>());
    }

    

    [Fact]
    public async Task IngestAsync_ShouldPopulateAuditSummary()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var now = DateTime.UtcNow;
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), now), CancellationToken.None);
        Assert.NotNull(r.Audit);
        Assert.Equal("f.ach", r.Audit.FileName);
        Assert.Equal(now, r.Audit.ReceivedAtUtc);
        Assert.Equal(1, r.Audit.TotalRecords);
        Assert.Equal(r.ParsedReturnCount, r.Audit.ParsedReturnCount);
        Assert.Equal(r.LinkedReturnCount, r.Audit.LinkedReturnCount);
        Assert.Equal(r.Failures.Count, r.Audit.FailureCount);
    }

    [Fact]
    public async Task IngestAsync_ShouldCalculateContentSha256()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var content = BuildType7("R01", "123456780000001");
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r1 = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        var r2 = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(64, r1.Audit.ContentSha256.Length);
        Assert.Equal(r1.Audit.ContentSha256, r2.Audit.ContentSha256);
    }

    [Fact]
    public async Task IngestAsync_ShouldCalculateRawRecordHash()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(64, r.Audit.Records.First().RawRecordHash.Length);
    }

    [Fact]
    public async Task IngestAsync_ShouldNotExposeFullRawContentInAudit()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var raw = BuildType7("R01", "123456780000001");
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", raw, DateTime.UtcNow), CancellationToken.None);
        var preview = r.Audit.Records.First().RawRecordPreview;
        Assert.NotEqual(raw, preview);
        Assert.True(preview!.Length <= 24);
    }

    [Fact]
    public async Task IngestAsync_ShouldIncludeFailureCodesInAudit()
    {
        await using var c = Ctx();
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "000000000000000"), DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Audit.Failures, x => x.Code == "ORIGINAL_TRANSACTION_NOT_FOUND");
    }

    [Fact]
    public async Task IngestAsync_ShouldAuditDuplicateFailure()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "123456780000001");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Contains(r.Audit.Failures, x => x.Code == "INCOMING_RETURN_DUPLICATE_IN_FILE");
    }

    [Fact]
    public async Task IngestAsync_ShouldPreserveItemsAndExistingCounts()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Single(r.Items);
        Assert.Equal(r.ParsedReturnCount, r.Audit.ParsedReturnCount);
        Assert.Equal(r.LinkedReturnCount, r.Audit.LinkedReturnCount);
        Assert.Equal(r.UnlinkedReturnCount, r.Audit.UnlinkedReturnCount);
    }


    [Fact]
    public async Task IngestAsync_ShouldClassifyAccepted_WhenNoFailures()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(AchIncomingReturnIngestionDecision.Accepted, r.Decision);
        Assert.True(r.IsAccepted);
    }

    [Fact]
    public async Task IngestAsync_ShouldClassifyRejectedTotal_WhenFileIsEmpty()
    {
        await using var c = Ctx();
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", "", DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedTotal, r.Decision);
        Assert.True(r.IsRejectedTotal);
    }

    [Fact]
    public async Task IngestAsync_ShouldClassifyRejectedTotal_WhenAllReturnsAreUnlinked()
    {
        await using var c = Ctx();
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("R01", "000000000000001") + BuildType7("R02", "000000000000002");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedTotal, r.Decision);
    }

    [Fact]
    public async Task IngestAsync_ShouldClassifyRejectedPartial_WhenOneReturnValidAndAnotherUnlinked()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "000000000000002");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedPartial, r.Decision);
        Assert.True(r.IsRejectedPartial);
    }

    [Fact]
    public async Task IngestAsync_ShouldClassifyRejectedTotal_WhenAllLinkedReturnsHaveRegulatoryFailures()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, txId: 10, cycleId: "C1");
        SeedTx(c, "123456780000002", 7001, txId: 11, cycleId: "C2");
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((false, "rechazo"));
        var sut = new AchIncomingReturnIngestionService(c, catalog.Object);
        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "123456780000002");
        var r = await sut.IngestAsync(new("f.ach", content, DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedTotal, r.Decision);
    }

    [Fact]
    public async Task IngestAsync_ShouldIncludeDecisionInAudit()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());
        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(r.Decision, r.Audit.Decision);
    }
static string BuildType7(string reason, string originalTrace)
    {
        var chars = Enumerable.Repeat(' ', 106).ToArray();
        chars[0] = '7'; chars[1] = '9'; chars[2] = '9';
        var rr = reason.PadRight(5).Take(5).ToArray(); Array.Copy(rr,0,chars,3,5);
        var tr = originalTrace.PadLeft(15,'0').TakeLast(15).ToArray(); Array.Copy(tr,0,chars,8,15);
        return new string(chars);
    }

    static Mock<IAchRegulatoryCatalogService> CatalogAllowAllMock()
    {
        var m = new Mock<IAchRegulatoryCatalogService>();
        m.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        m.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        return m;
    }

    static IAchRegulatoryCatalogService CatalogAllowAll() => CatalogAllowAllMock().Object;

    static AchDbContext Ctx() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    static void SeedTx(AchDbContext c, string trace, int clearingHouseId, int txId = 10, string cycleId = "C1")
    {
        if (!c.ClearingHouses.Any(x => x.Id == clearingHouseId))
        {
            c.ClearingHouses.Add(new ClearingHouse { Id = clearingHouseId, Code = clearingHouseId == 7001 ? "CENIT" : "ACH", Name = "CH", OriginCode = "000101006" });
        }
        c.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = cycleId, ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = clearingHouseId });
        c.AchTransactions.Add(new AchTransaction { Id = txId, TraceNumber = trace, AchCycleId = cycleId, Type = TransactionTypeEnum.Credit, State = AchTransferStateEnum.Pending, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2", OriginalTraceRef = $"ALT{txId:000000000000}" });
        c.SaveChanges();
    }
}
