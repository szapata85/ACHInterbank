using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed record TraceabilityReportFilter
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public AchTransferStateEnum? State { get; init; }
    public string? AchCycleId { get; init; }
}

