using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Implementation;

public sealed class AccountingReviewReportBuilder : IAccountingReviewReportBuilder
{
    public AccountingReviewReportResult Build(
        AccountingReviewReportRequest request,
        IEnumerable<AccountingReviewReportRow> rows,
        IEnumerable<AccountingReviewDifference> differences,
        IEnumerable<AccountingReviewEvidenceReference> evidence)
    {
        var rowsList = rows.ToList();
        var diffList = differences.ToList();
        var evidenceList = evidence.ToList();

        var summary = new AccountingReviewReportSummary
        {
            TotalRows = rowsList.Count,
            TotalAmount = rowsList.Sum(x => x.Amount),
            TotalOutboundAmount = rowsList.Where(x => x.RowType == AccountingReviewRowType.OutboundTransaction).Sum(x => x.Amount),
            TotalIncomingAmount = rowsList.Where(x => x.RowType == AccountingReviewRowType.IncomingReturn).Sum(x => x.Amount),
            TotalReturnAmount = rowsList.Where(x => x.RowType is AccountingReviewRowType.OutboundReturn or AccountingReviewRowType.ReturnOfReturn).Sum(x => x.Amount),
            TotalRejectedAmount = rowsList.Where(x => x.IsRejected).Sum(x => x.Amount),
            TotalDifferenceAmount = diffList.Sum(x => x.DifferenceAmount),
            OrphanCount = rowsList.Count(x => x.IsOrphan),
            ManualAuditOnlyCount = rowsList.Count(x => x.IsManualAuditOnly),
            ReturnOfReturnCount = rowsList.Count(x => x.IsReturnOfReturn),
            CudEvidenceCount = rowsList.Count(x => x.IsCudEvidence),
            DifferenceCount = diffList.Count,
            HasDifferences = diffList.Count != 0,
            HasPendingEvidence = rowsList.Any(x => x.ReconciliationStatus == AccountingReviewReconciliationStatus.EvidencePending),
            HasManualReviewItems = rowsList.Any(x => x.ReconciliationStatus == AccountingReviewReconciliationStatus.ManualReview || x.IsManualAuditOnly || x.IsOrphan)
        };

        var scope = new AccountingReviewScope
        {
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            ClearingHouseId = request.ClearingHouseId,
            ClearingHouseCode = request.ClearingHouseCode,
            CycleId = request.CycleId,
            CycleName = request.CycleName,
            FileId = request.FileId,
            FileName = request.FileName,
            TransactionId = request.TransactionId,
            Status = request.Status,
            CauseCode = request.CauseCode,
            RequestedBy = request.RequestedBy,
            CorrelationId = request.CorrelationId
        };

        return new AccountingReviewReportResult
        {
            ReportId = Guid.NewGuid(),
            GeneratedAt = DateTimeOffset.UtcNow,
            GeneratedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy,
            Scope = scope,
            Summary = summary,
            Rows = rowsList,
            Differences = diffList,
            Evidence = evidenceList,
            ExportMetadata = new AccountingReviewExportMetadata(),
            BoundaryFlags = AccountingReviewBoundaryFlags.Default,
            Warnings = BuildWarnings(request, rowsList, diffList)
        };
    }

    private static IReadOnlyList<string> BuildWarnings(AccountingReviewReportRequest request, IReadOnlyList<AccountingReviewReportRow> rows, IReadOnlyList<AccountingReviewDifference> differences)
    {
        var warnings = new List<string>();
        if (!request.IncludeCudEvidence)
            warnings.Add("CUD evidence excluded by request scope.");
        if (!request.IncludeManualAuditOnly)
            warnings.Add("Manual audit-only rows excluded by request scope.");
        if (rows.Count == 0)
            warnings.Add("No rows found for requested scope.");
        if (differences.Any(x => x.Severity == AccountingReviewDifferenceSeverity.Critical))
            warnings.Add("Critical differences detected.");
        return warnings;
    }
}
