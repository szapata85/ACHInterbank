using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Persistence.Reports.Models;

internal sealed class AchReturnRejectionReportDocumentModel
{
    public required string Title { get; init; }
    public required AchReturnRejectionReportFilter Filter { get; init; }
    public required IReadOnlyList<AchReturnRejectionReportRowDto> Rows { get; init; }
    public required AchReturnRejectionReportTotalsDto Totals { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}
