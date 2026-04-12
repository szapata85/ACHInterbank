using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Persistence.Reports.Models;

internal sealed class AchNachaFileReportDocumentModel
{
    public required AchNachaFileReportFilter Filter { get; init; }
    public required IReadOnlyList<AchNachaFileReportRowDto> Rows { get; init; }
    public required AchNachaFileReportTotalsDto Totals { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}
