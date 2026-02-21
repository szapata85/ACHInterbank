using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

namespace Cfa.ACHInterbank.Persistence.Reports.Models;

internal sealed class TraceabilityReportDocumentModel
{
    public required TraceabilityReportFilter Filter { get; init; }
    public required IReadOnlyList<AchTraceabilityReportRowDto> Rows { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}
