using System.Reflection;
using Cfa.ACHInterbank.Application.Reports.Implementation;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public class AccountingReviewReportModelTests
{
    [Fact]
    public void AccountingReviewReport_ShouldAlwaysDeclareNonAccountingBoundary()
    {
        var flags = AccountingReviewBoundaryFlags.Default;
        flags.IsAccountingPosting.Should().BeFalse();
        flags.IsOfficialLedger.Should().BeFalse();
        flags.IsJournalEntry.Should().BeFalse();
        flags.CreatesAccountingEntry.Should().BeFalse();
        flags.RequiresAccountingApi.Should().BeFalse();
        flags.IsOperationalReport.Should().BeTrue();
        flags.IsThirdPartyReview.Should().BeTrue();
        flags.IsReconciliationSupport.Should().BeTrue();
    }

    [Fact]
    public void AccountingReviewReportModel_ShouldNotExposeLedgerJournalPostingTerms()
    {
        var publicTypes = typeof(AccountingReviewReportResult).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(AccountingReviewReportResult).Namespace)
            .ToArray();

        var forbidden = new[] { "LedgerId", "JournalId", "PostingId", "AccountingEntryId", "DebitAccount", "CreditAccount", "BookedAt", "PostedAt", "AccountingPosted" };
        var publicMembers = publicTypes.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Select(p => p.Name).ToArray();

        foreach (var key in forbidden)
            publicMembers.Should().NotContain(key);
    }

    [Fact]
    public void AccountingReviewReportBuilder_ShouldCalculateSummary()
    {
        var builder = new AccountingReviewReportBuilder();
        var request = new AccountingReviewReportRequest { RequestedBy = "qa" };
        var rows = new[]
        {
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.OutboundTransaction, Amount = 100m, IsAppliedOperationally = true },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.IncomingReturn, Amount = 20m, IsAppliedOperationally = true },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.Rejection, Amount = 10m, IsRejected = true },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.Orphan, Amount = 0m, IsOrphan = true },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.ManualAuditOnly, Amount = 0m, IsManualAuditOnly = true, ReconciliationStatus = AccountingReviewReconciliationStatus.ManualReview },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.ReturnOfReturn, Amount = 5m, IsReturnOfReturn = true },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.CudEvidence, Amount = 0m, IsCudEvidence = true, ReconciliationStatus = AccountingReviewReconciliationStatus.EvidencePending }
        };
        var differences = new[] { new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.Amount, Severity = AccountingReviewDifferenceSeverity.Warning, DifferenceAmount = 2m, Description = "diff" } };
        var result = builder.Build(request, rows, differences, []);

        result.Summary.TotalRows.Should().Be(7);
        result.Summary.TotalAmount.Should().Be(135m);
        result.Summary.TotalOutboundAmount.Should().Be(100m);
        result.Summary.TotalIncomingAmount.Should().Be(20m);
        result.Summary.TotalReturnAmount.Should().Be(5m);
        result.Summary.TotalRejectedAmount.Should().Be(10m);
        result.Summary.OrphanCount.Should().Be(1);
        result.Summary.ManualAuditOnlyCount.Should().Be(1);
        result.Summary.ReturnOfReturnCount.Should().Be(1);
        result.Summary.CudEvidenceCount.Should().Be(1);
        result.Summary.DifferenceCount.Should().Be(1);
        result.Summary.HasDifferences.Should().BeTrue();
    }

    [Fact]
    public void AccountingReviewReportBuilder_ShouldKeepManualAuditOnlyAsNotApplied()
    {
        var builder = new AccountingReviewReportBuilder();
        var row = new AccountingReviewReportRow { RowType = AccountingReviewRowType.ManualAuditOnly, IsManualAuditOnly = true, IsAppliedOperationally = false };
        var result = builder.Build(new AccountingReviewReportRequest { RequestedBy = "qa" }, [row], [], []);

        result.Rows.Single().IsManualAuditOnly.Should().BeTrue();
        result.Rows.Single().IsAppliedOperationally.Should().BeFalse();
        result.BoundaryFlags.IsAccountingPosting.Should().BeFalse();
    }

    [Fact]
    public void AccountingReviewReportBuilder_ShouldRepresentRejectedPartialAsRecordLevelNotAmountPartial()
    {
        var row = new AccountingReviewReportRow
        {
            RowType = AccountingReviewRowType.Rejection,
            IsRejected = true,
            Observation = "RejectedPartial record-level"
        };

        row.Observation.Should().Contain("record-level");
        typeof(AccountingReviewReportRow).GetProperties().Select(x => x.Name).Should().NotContain("PartialAmountReturn");
    }

    [Fact]
    public void AccountingReviewReportBuilder_ShouldRepresentCudAsOperationalEvidenceOnly()
    {
        var evidence = new AccountingReviewEvidenceReference
        {
            EvidenceType = AccountingReviewEvidenceType.CudOperationalEvidence,
            ReferenceId = "E1",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "ops",
            IsExternalEvidence = true
        };
        evidence.EvidenceType.Should().Be(AccountingReviewEvidenceType.CudOperationalEvidence);
        evidence.IsExternalEvidence.Should().BeTrue();
        typeof(AccountingReviewEvidenceReference).GetProperties().Select(x => x.Name).Should().NotContain("CudSettlementApi");
    }

    [Fact]
    public void AccountingReviewReportBuilder_ShouldRepresentDifferencesWithoutPosting()
    {
        var builder = new AccountingReviewReportBuilder();
        var differences = new[]
        {
            new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.Amount, Description = "amount", Severity = AccountingReviewDifferenceSeverity.Warning },
            new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.Status, Description = "status", Severity = AccountingReviewDifferenceSeverity.Info },
            new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.CauseCode, Description = "cause", Severity = AccountingReviewDifferenceSeverity.Critical }
        };
        var result = builder.Build(new AccountingReviewReportRequest { RequestedBy = "qa" }, [], differences, []);

        result.Differences.Should().HaveCount(3);
        result.BoundaryFlags.CreatesAccountingEntry.Should().BeFalse();
        result.BoundaryFlags.IsAccountingPosting.Should().BeFalse();
    }

    [Fact]
    public void AccountingReviewReportModel_ShouldBeNonPersistentReadModel()
    {
        var dbSetProps = typeof(AchDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
            .ToHashSet();

        dbSetProps.Should().NotContain(nameof(AccountingReviewReportResult));
        dbSetProps.Should().NotContain(nameof(AccountingReviewReportRow));

        var hasConfig = typeof(AccountingReviewReportResult).Assembly.GetTypes()
            .Any(t => t.Name.Contains("AccountingReview", StringComparison.Ordinal) &&
                      t.GetInterfaces().Any(i => i.Name.StartsWith("IEntityTypeConfiguration", StringComparison.Ordinal)));

        hasConfig.Should().BeFalse();
    }
}
