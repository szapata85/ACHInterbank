using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Persistence.Reports.Models;

internal sealed class AchAuditReportDocumentModel
{
    public required AchAuditReportFilter Filter { get; init; }
    public required IReadOnlyList<AchAuditReportRowDto> Rows { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}
