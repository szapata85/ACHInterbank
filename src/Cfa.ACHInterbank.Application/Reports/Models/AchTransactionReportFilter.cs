using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AchTransactionReportFilter
{
    public DateTime? Date { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? AchCycleId { get; init; }
    public AchTransferStateEnum? State { get; init; }
    public string? Reference { get; init; }
    public int? BankId { get; init; }
    public TransactionTypeEnum? TransactionType { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

