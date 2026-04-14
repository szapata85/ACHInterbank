using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AchReturnRejectionReportFilter
{
    public DateTime? Date { get; init; }
    public string? Causal { get; init; }
    public int? ClearingHouseId { get; init; }
    public AchTransferStateEnum? State { get; init; }
    public string? Reference { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class AchReturnRejectionReportRowDto
{
    public int TransactionId { get; init; }
    public DateTime EffectiveEntryDate { get; init; }
    public string TransactionExternalId { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public AchTransferStateEnum State { get; init; }
    public string CausalCode { get; init; } = string.Empty;
    public string CausalDescription { get; init; } = string.Empty;
    public string ClearingHouseName { get; init; } = string.Empty;
    public string AchCycleId { get; init; } = string.Empty;
    public string AchCycleName { get; init; } = string.Empty;
    public string OriginalTraceRef { get; init; } = string.Empty;
    public int? OriginalTransactionId { get; init; }
    public string? OriginalTransactionReference { get; init; }
}

public sealed class AchReturnRejectionReportTotalsDto
{
    public int TotalRecords { get; init; }
    public decimal TotalAmount { get; init; }
}

public sealed class AchReturnRejectionReportResponseDto
{
    public IReadOnlyList<AchReturnRejectionReportRowDto> Items { get; init; } = [];
    public AchReturnRejectionReportTotalsDto Totals { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}
