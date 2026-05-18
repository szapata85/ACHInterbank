using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Interfaces;

public interface IAccountingReviewReportBuilder
{
    AccountingReviewReportResult Build(
        AccountingReviewReportRequest request,
        IEnumerable<AccountingReviewReportRow> rows,
        IEnumerable<AccountingReviewDifference> differences,
        IEnumerable<AccountingReviewEvidenceReference> evidence);
}
