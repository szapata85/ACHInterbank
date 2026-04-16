using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AchTransactionReportRowDto
{
    public int TransactionId { get; init; }
    public DateTime EffectiveEntryDate { get; init; }
    public string TransactionExternalId { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public TransactionTypeEnum TransactionType { get; init; }
    public AchTransferStateEnum State { get; init; }
    public string ClearingHouseName { get; init; } = string.Empty;
    public string AchCycleId { get; init; } = string.Empty;
    public string AchCycleName { get; init; } = string.Empty;
    public int BatchId { get; init; }
    public int BatchSequenceNumber { get; init; }
    public string SourceBankName { get; init; } = string.Empty;
    public string DestinationBankName { get; init; } = string.Empty;
    public string NachaFileName { get; init; } = string.Empty;
}

public sealed class AchTransactionReportTotalsDto
{
    public int TotalRecords { get; init; }
    public decimal TotalCreditAmount { get; init; }
    public decimal TotalDebitAmount { get; init; }
}

public sealed class AchTransactionReportResponseDto
{
    public IReadOnlyList<AchTransactionReportRowDto> Items { get; init; } = [];
    public AchTransactionReportTotalsDto Totals { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}
