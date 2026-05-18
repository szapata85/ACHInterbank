using System.Reflection;
using Cfa.ACHInterbank.Application.Reconciliation.Implementation;
using Cfa.ACHInterbank.Application.Reconciliation.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public class ReconciliationEvidenceModelTests
{
    [Fact]
    public void ReconciliationEvidence_ShouldAlwaysDeclareNonAccountingBoundary()
    {
        var flags = ReconciliationEvidenceBoundaryFlags.Default;
        flags.IsAccountingPosting.Should().BeFalse();
        flags.IsOfficialLedger.Should().BeFalse();
        flags.IsJournalEntry.Should().BeFalse();
        flags.CreatesAccountingEntry.Should().BeFalse();
        flags.RequiresAccountingApi.Should().BeFalse();
        flags.IsOperationalEvidence.Should().BeTrue();
        flags.IsThirdPartyReviewEvidence.Should().BeTrue();
        flags.IsReconciliationSupport.Should().BeTrue();
    }

    [Fact]
    public void ReconciliationEvidenceModel_ShouldNotExposeLedgerJournalPostingTerms()
    {
        var types = typeof(ReconciliationEvidenceResult).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(ReconciliationEvidenceResult).Namespace)
            .ToArray();
        var props = types.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Select(p => p.Name).ToArray();
        var forbidden = new[] { "LedgerId", "JournalId", "PostingId", "AccountingEntryId", "DebitAccount", "CreditAccount", "BookedAt", "PostedAt", "AccountingPosted" };
        foreach (var f in forbidden)
            props.Should().NotContain(f);
    }

    [Fact]
    public void ReconciliationEvidenceBuilder_ShouldBuildEvidenceSetWithScopeAndItems()
    {
        var builder = new ReconciliationEvidenceBuilder();
        var request = new ReconciliationEvidenceRequest { RequestedBy = "qa", CycleId = "C1", FileName = "A.NACHA" };
        var items = new[]
        {
            MakeItem("1", ReconciliationEvidenceType.OperationalReport),
            MakeItem("2", ReconciliationEvidenceType.Traceability),
            MakeItem("3", ReconciliationEvidenceType.ThirdPartyReport, isExternal:true),
            MakeItem("4", ReconciliationEvidenceType.CudOperationalEvidence, isCud:true, isExternal:true),
            MakeItem("5", ReconciliationEvidenceType.Orphan, isOrphan:true),
            MakeItem("6", ReconciliationEvidenceType.ManualReview, isManual:true),
            MakeItem("7", ReconciliationEvidenceType.ReturnOfReturn, isRor:true),
            MakeItem("8", ReconciliationEvidenceType.Rejection, isRejected:true, description:"RejectedPartial record-level"),
            MakeItem("9", ReconciliationEvidenceType.Netting, isNetting:true),
            MakeItem("10", ReconciliationEvidenceType.Liquidity, isLiquidity:true)
        };
        var attachments = items.Select(i => new ReconciliationEvidenceAttachment { AttachmentId = "A" + i.EvidenceItemId, EvidenceItemId = i.EvidenceItemId, AttachmentType = ReconciliationEvidenceAttachmentType.Pdf, FileName = i.EvidenceItemId + ".pdf", CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "qa" }).ToArray();

        var result = builder.Build(request, items, attachments, [], []);
        result.EvidenceSetId.Should().NotBe(Guid.Empty);
        result.GeneratedBy.Should().Be("qa");
        result.Scope.CycleId.Should().Be("C1");
        result.Items.Should().HaveCount(10);
        result.BoundaryFlags.IsAccountingPosting.Should().BeFalse();
    }

    [Fact]
    public void ReconciliationEvidenceBuilder_ShouldGenerateDeterministicIdempotencyKey()
    {
        var builder = new ReconciliationEvidenceBuilder();
        var request = new ReconciliationEvidenceRequest { RequestedBy = "qa", CycleId = "C1", FileHash = "H1", TransactionId = 10 };
        var items = new[] { MakeItem("1", ReconciliationEvidenceType.OperationalReport, tx:10, fileHash:"H1") };

        var r1 = builder.Build(request, items, [], [], []);
        var r2 = builder.Build(request, items, [], [], []);
        r1.IdempotencyKey.Key.Should().Be(r2.IdempotencyKey.Key);

        var request2 = new ReconciliationEvidenceRequest { RequestedBy = request.RequestedBy, CycleId = "C2", FileHash = request.FileHash, TransactionId = request.TransactionId };
        var r3 = builder.Build(request2, items, [], [], []);
        r3.IdempotencyKey.Key.Should().NotBe(r1.IdempotencyKey.Key);
    }

    [Fact]
    public void ReconciliationEvidenceBuilder_ShouldRepresentCudAsOperationalEvidenceOnly()
    {
        var item = MakeItem("C1", ReconciliationEvidenceType.CudOperationalEvidence, isCud:true, isExternal:true);
        item.EvidenceType.Should().Be(ReconciliationEvidenceType.CudOperationalEvidence);
        item.IsCudEvidence.Should().BeTrue();
        item.IsExternalEvidence.Should().BeTrue();
        typeof(ReconciliationEvidenceItem).GetProperties().Select(p => p.Name).Should().NotContain("CudSettlementApi");
    }

    [Fact]
    public void ReconciliationEvidenceBuilder_ShouldKeepManualAuditOnlyAsEvidenceNotApplied()
    {
        var review = new ReconciliationEvidenceReview { ReviewId = "R1", EvidenceItemId = "M1", ReviewStatus = ReconciliationEvidenceReviewStatus.RequiresMoreEvidence };
        var item = MakeItem("M1", ReconciliationEvidenceType.ManualReview, isManual:true);
        var result = new ReconciliationEvidenceBuilder().Build(new ReconciliationEvidenceRequest { RequestedBy = "qa" }, [item], [], [], [review]);
        result.Items.Single().IsManualAuditOnly.Should().BeTrue();
        result.Reviews.Single().ReviewStatus.Should().Be(ReconciliationEvidenceReviewStatus.RequiresMoreEvidence);
        result.BoundaryFlags.IsAccountingPosting.Should().BeFalse();
    }

    [Fact]
    public void ReconciliationEvidenceBuilder_ShouldRepresentOrphanAsPendingEvidence()
    {
        var item = MakeItem("O1", ReconciliationEvidenceType.Orphan, isOrphan:true);
        var diff = new ReconciliationEvidenceDifferenceLink { DifferenceLinkId = "D1", EvidenceItemId = "O1", DifferenceType = ReconciliationEvidenceDifferenceType.OrphanPending, Severity = ReconciliationEvidenceSeverity.Warning, Description = "pending" };
        var result = new ReconciliationEvidenceBuilder().Build(new ReconciliationEvidenceRequest { RequestedBy = "qa" }, [item], [], [diff], []);
        result.Items.Single().IsOrphan.Should().BeTrue();
        result.DifferenceLinks.Single().DifferenceType.Should().Be(ReconciliationEvidenceDifferenceType.OrphanPending);
        result.Warnings.Should().Contain(x => x.Contains("Orphan", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReconciliationEvidenceBuilder_ShouldRepresentReturnOfReturnAsTraceableEvidence()
    {
        var item = MakeItem("R1", ReconciliationEvidenceType.ReturnOfReturn, tx:99, isRor:true, extRef:"ROR-99");
        var result = new ReconciliationEvidenceBuilder().Build(new ReconciliationEvidenceRequest { RequestedBy = "qa" }, [item], [], [], []);
        result.Items.Single().IsReturnOfReturn.Should().BeTrue();
        result.Items.Single().EvidenceType.Should().Be(ReconciliationEvidenceType.ReturnOfReturn);
        result.Items.Single().ExternalReference.Should().Be("ROR-99");
        result.BoundaryFlags.CreatesAccountingEntry.Should().BeFalse();
    }

    [Fact]
    public void ReconciliationEvidenceBuilder_ShouldRepresentRejectedPartialAsRecordLevelEvidence()
    {
        var item = MakeItem("RJ1", ReconciliationEvidenceType.Rejection, isRejected:true, description:"RejectedPartial record-level");
        item.IsRejected.Should().BeTrue();
        item.Description.Should().Contain("record-level");
        typeof(ReconciliationEvidenceItem).GetProperties().Select(p => p.Name).Should().NotContain(new[] { "PartialAmountReturn", "AmountPartialReturn" });
    }

    [Fact]
    public void ReconciliationEvidenceBuilder_ShouldCreateWarningsForCriticalDifferencesAndPendingEvidence()
    {
        var cud = MakeItem("CUD1", ReconciliationEvidenceType.CudOperationalEvidence, isCud:true);
        var manual = MakeItem("M1", ReconciliationEvidenceType.ManualReview, isManual:true);
        var orphan = MakeItem("O1", ReconciliationEvidenceType.Orphan, isOrphan:true);
        var critical = new ReconciliationEvidenceDifferenceLink { DifferenceLinkId = "D1", EvidenceItemId = "CUD1", DifferenceType = ReconciliationEvidenceDifferenceType.CudEvidenceMissing, Severity = ReconciliationEvidenceSeverity.Critical, Description = "critical" };

        var result = new ReconciliationEvidenceBuilder().Build(new ReconciliationEvidenceRequest { RequestedBy = "qa" }, [cud, manual, orphan], [], [critical], []);
        result.Warnings.Should().Contain(x => x.Contains("Critical", StringComparison.OrdinalIgnoreCase));
        result.Warnings.Should().Contain(x => x.Contains("CUD", StringComparison.OrdinalIgnoreCase));
        result.Warnings.Should().Contain(x => x.Contains("Manual", StringComparison.OrdinalIgnoreCase));
        result.Warnings.Should().Contain(x => x.Contains("Orphan", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReconciliationEvidenceAttachment_ShouldNotStoreBinaryContent()
    {
        var props = typeof(ReconciliationEvidenceAttachment).GetProperties().Select(p => p.Name).ToArray();
        props.Should().NotContain(new[] { "Bytes", "Content", "Base64", "FileContent" });
    }

    [Fact]
    public void ReconciliationEvidenceModel_ShouldBeNonPersistentReadModel()
    {
        var dbSetTypes = typeof(AchDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
            .ToHashSet();

        dbSetTypes.Should().NotContain(nameof(ReconciliationEvidenceResult));
        dbSetTypes.Should().NotContain(nameof(ReconciliationEvidenceItem));

        var hasConfig = typeof(ReconciliationEvidenceResult).Assembly.GetTypes()
            .Any(t => t.Namespace == typeof(ReconciliationEvidenceResult).Namespace &&
                      t.GetInterfaces().Any(i => i.Name.StartsWith("IEntityTypeConfiguration", StringComparison.Ordinal)));
        hasConfig.Should().BeFalse();
    }

    private static ReconciliationEvidenceItem MakeItem(string id, ReconciliationEvidenceType type, int? tx = null, string? fileHash = null, string? extRef = null, bool isExternal = false, bool isManual = false, bool isOrphan = false, bool isRor = false, bool isRejected = false, bool isCud = false, bool isNetting = false, bool isLiquidity = false, string? description = null)
        => new()
        {
            EvidenceItemId = id,
            EvidenceType = type,
            Source = isExternal ? ReconciliationEvidenceSource.ExternalThirdParty : ReconciliationEvidenceSource.Internal,
            TransactionId = tx,
            FileHash = fileHash,
            ExternalReference = extRef,
            IsExternalEvidence = isExternal,
            IsManualAuditOnly = isManual,
            IsOrphan = isOrphan,
            IsReturnOfReturn = isRor,
            IsRejected = isRejected,
            IsCudEvidence = isCud,
            IsNettingEvidence = isNetting,
            IsLiquidityEvidence = isLiquidity,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "qa"
        };
}
