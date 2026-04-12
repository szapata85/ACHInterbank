using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Persistence.Reports.Models;

internal sealed class AchHistoryReportDocumentModel
{
    public required AchHistoryReportFilter Filter { get; init; }
    public required IReadOnlyList<AchHistoryReportRowDto> Rows { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}
