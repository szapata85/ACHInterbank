using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Persistence.Reports.Models;

internal sealed class AchTransactionMovementReportDocumentModel
{
    public required string Title { get; init; }
    public required AchTransactionReportFilter Filter { get; init; }
    public required IReadOnlyList<AchTransactionReportRowDto> Rows { get; init; }
    public required AchTransactionReportTotalsDto Totals { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}

