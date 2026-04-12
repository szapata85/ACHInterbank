using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AchAuditReportFilter
{
    public string? User { get; init; }
    public string? Action { get; init; }
    public string? Entity { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class AchAuditReportRowDto
{
    public string User { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Entity { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public DateTime DateUtc { get; init; }
}

public sealed class AchAuditReportResponseDto
{
    public IReadOnlyList<AchAuditReportRowDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}

public sealed class AchHistoryReportFilter
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int? TransactionId { get; init; }
    public AchTransferStateEnum? ToState { get; init; }
    public AchStateEventSourceEnum? Source { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class AchHistoryReportRowDto
{
    public int TransactionId { get; init; }
    public AchTransferStateEnum FromState { get; init; }
    public AchTransferStateEnum ToState { get; init; }
    public AchStateEventSourceEnum Source { get; init; }
    public string? ReasonCode { get; init; }
    public DateTime DateUtc { get; init; }
    public string? ChangedBy { get; init; }
}

public sealed class AchHistoryReportResponseDto
{
    public IReadOnlyList<AchHistoryReportRowDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}
