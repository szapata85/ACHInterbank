using Cfa.ACHInterbank.Application.Reports.Export.Models;
using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Export.Interfaces;

public interface IAccountingReviewReportExporter
{
    AccountingReviewExportResult Export(AccountingReviewReportResult report, AccountingReviewExportRequest request);
}
